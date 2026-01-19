// Setup the reference to the dotnet object
function setDotNetReference(dotNetHelper) {
    window.dotNetHelper = dotNetHelper;
}

// Scroll to the given element
function scrollToElement(id) {
    const e = document.getElementById(id);
    if (e != null) {
        e.scrollIntoView();
        e.focus();
    }
}

// Mark in yellow the keyword in the note content
function highlightInNotes(keyword) {
    var context = document.querySelectorAll('.note-content');
    var instance = new Mark(context);
    instance.mark(keyword);
}

// Clear the higlighting in the note content
function clearHighlightInNotes() {
    var context = document.querySelectorAll('.note-content');
    var instance = new Mark(context);
    instance.unmark();
}

// Copy text to clipboard
function copyText(text) {
    navigator.clipboard.writeText(text).then(function () {
        alert("Text copied to clipboard!");
    })
    .catch(function (error) {
        alert(error);
    });
};


// JS functions scoped to "mentions" namespace
window.mentions = (function () {

    // ===== STATE =====
    let active = false;

    // ===== UTIL =====
    function getEditable(host) {
        // Radzen HtmlEditor wraps a contenteditable element under .rz-html-editor-content
        return host.querySelector('.rz-html-editor-content [contenteditable="true"]')
            || host.querySelector('.rz-html-editor-content');
    }

    function isInsideMention(node) {
        let el = node && node.parentNode;
        while (el && el.nodeType === Node.ELEMENT_NODE) {
            if (el.classList && el.classList.contains('mention')) return true;
            el = el.parentNode;
        }
        return false;
    }

    // Build the text from block-start to caret, but mask '@' characters inside .mention
    function buildPreMasked(block, rangeToCaret) {
        const targetLen = rangeToCaret.toString().length; // exact chars up to caret
        const walker = document.createTreeWalker(block, NodeFilter.SHOW_TEXT, null);
        let out = '', remaining = targetLen, n;
        while ((n = walker.nextNode()) && remaining > 0) {
            let t = n.nodeValue || '';
            if (t.length > remaining) t = t.slice(0, remaining);
            if (isInsideMention(n)) {
                // Mask @ so lastIndexOf('@') won't find triggers inside mentions
                t = t.replace(/@/g, '\uFFFF');
            }
            out += t;
            remaining -= t.length;
        }
        return out;
    }

    // Private: Get the index of where the popup was last triggered
    function lastTriggerIndex(text, triggers) {
        let best = -1, trig = null;
        for (const t of triggers) {
            const i = text.lastIndexOf(t);
            if (i > best) { best = i; trig = t; }
        }
        if (best < 0) return null;

        // Ensure it's a token start: either at string start or preceded by whitespace/punctuation
        const prev = best === 0 ? " " : text[best - 1];
        if (/\s|[()[\]{}.,;:!?/"'\\-]/.test(prev)) {
            return { index: best, trigger: trig[0] };
        }
        return null;
    }

    // Private: Get the block element closest to the node
    function getClosestBlock(node, root) {
        while (node && node !== root) {
            if (node.nodeType === Node.ELEMENT_NODE) {
                const name = node.nodeName.toLowerCase();
                if (["p", "div", "li", "blockquote", "pre"].includes(name)) return node;
            }
            node = node.parentNode;
        }
        return root;
    }

    // Private: Get rectangle coords of the caret
    function getCaretRect(range) {
        // Prefer a client rect; fallback to a zero-width marker trick
        let rects = range.getClientRects();
        if (rects && rects.length) return rects[rects.length - 1];
        const span = document.createElement('span');
        span.appendChild(document.createTextNode('\u200b'));
        range.insertNode(span);
        const rect = span.getBoundingClientRect();
        span.parentNode.removeChild(span);
        range.collapse(false);
        return rect;
    }

    // ===== PUBLIC =====

    // 1) Hook keydown to cancel Enter/Tab/Arrows only when popup is active
    function bindKeydown(hostSelector) {
        const host = document.querySelector(hostSelector);
        const editable = host && getEditable(host);
        if (!editable) return;

        // Use capture to beat the contenteditable default behavior
        editable.addEventListener('keydown', (e) => {
            if (!active) return;
            const k = e.key;
            if (k === 'Enter' || k === 'Tab' || k === 'ArrowDown' || k === 'ArrowUp' || k === 'Escape') {
                // Prevent newline, caret movement, tab focus, etc.
                e.preventDefault();
                // NOTE: do NOT stopPropagation so Blazor still receives @onkeydown
            }
        }, true);
    }

    function setActive(value) {
        active = !!value;
    }

    // 2) Token info detection (ignores @ inside .mention)
    function getTokenInfo(hostSelector, triggerList) {
        const host = document.querySelector(hostSelector);
        const editable = host && getEditable(host);
        if (!editable) return null;

        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return { hasTrigger: false };

        const range = sel.getRangeAt(0);
        if (!editable.contains(range.endContainer)) return { hasTrigger: false };

        const probe = range.cloneRange();
        const block = getClosestBlock(range.endContainer, editable);
        probe.setStart(block, 0);

        const preMasked = buildPreMasked(block, probe);

        const triggers = (triggerList || "@,#").split(",").map(s => s.trim()).filter(Boolean);
        const last = lastTriggerIndex(preMasked, triggers);
        const rect = getCaretRect(range);

        if (!last) {
            return { hasTrigger: false, clientTop: rect.top, clientLeft: rect.left, clientHeight: rect.height };
        }

        const { index, trigger } = last;
        // Compute text after trigger from the unmasked substring to caret:
        // Get the real pre (unmasked) so user query isn't affected by masks
        const preReal = probe.toString();
        const text = preReal.substring(index + 1);

        return {
            hasTrigger: true,
            trigger: trigger,
            text: text,
            clientTop: rect.top,
            clientLeft: rect.left,
            clientHeight: rect.height
        };
    }

    // 3) Select from last trigger to caret (also ignores @ inside .mention)
    function selectFromTriggerToCaret(hostSelector, triggerChar) {
        const host = document.querySelector(hostSelector);
        const editable = host && getEditable(host);
        if (!editable) return false;

        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return false;
        const range = sel.getRangeAt(0);
        if (!editable.contains(range.endContainer)) return false;

        const probe = range.cloneRange();
        const block = getClosestBlock(range.endContainer, editable);
        probe.setStart(block, 0);

        const preMasked = buildPreMasked(block, probe);
        const lastAt = preMasked.lastIndexOf((triggerChar || '@'));
        if (lastAt < 0) return false;

        // Walk to the text node and offset matching lastAt over the REAL text flow
        // (masked text is same length as real pre, so indices line up)
        const walker = document.createTreeWalker(block, NodeFilter.SHOW_TEXT, null);
        let remaining = lastAt, startNode = null, startOffset = 0, n;
        while (n = walker.nextNode()) {
            const len = (n.nodeValue || '').length;
            if (remaining <= len) { startNode = n; startOffset = remaining; break; }
            remaining -= len;
        }
        if (!startNode) return false;

        const selRange = document.createRange();
        selRange.setStart(startNode, startOffset);
        selRange.setEnd(range.endContainer, range.endOffset);
        sel.removeAllRanges();
        sel.addRange(selRange);
        return true;
    }

    return {
        // lifecycle / control
        bindKeydown,
        setActive,

        // mention logic
        getTokenInfo,
        selectFromTriggerToCaret
    };
})();