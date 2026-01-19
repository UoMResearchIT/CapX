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

    // Private: Get the editor element
    function getEditable(host) {
        // Radzen HtmlEditor wraps a contenteditable element under .rz-html-editor-content
        return host.querySelector('.rz-html-editor-content [contenteditable="true"]')
            || host.querySelector('.rz-html-editor-content');
    }

    // Public: Returns token info near the caret and the caret rect (viewport coords)
    function getTokenInfo(hostSelector, triggerList) {
        const host = document.querySelector(hostSelector);
        const editable = host && getEditable(host);
        if (!editable) return null;

        const sel = window.getSelection();
        if (!sel || sel.rangeCount === 0) return { hasTrigger: false };

        const range = sel.getRangeAt(0);
        if (!editable.contains(range.endContainer)) return { hasTrigger: false };

        // Build the text from the start of the line/paragraph node to caret (simple heuristic)
        const probe = range.cloneRange();
        const block = getClosestBlock(range.endContainer, editable);
        probe.setStart(block, 0);
        const pre = probe.toString();

        const triggers = (triggerList || "@,#").split(",").map(s => s.trim()).filter(Boolean);
        const last = lastTriggerIndex(pre, triggers);
        if (!last) {
            const rect = getCaretRect(range);
            return { hasTrigger: false, clientTop: rect.top, clientLeft: rect.left, clientHeight: rect.height };
        }

        const { index, trigger } = last;
        const text = pre.substring(index + 1); // after the trigger up to caret
        const rect = getCaretRect(range);

        return {
            hasTrigger: true,
            trigger: trigger,
            text: text,
            clientTop: rect.top,
            clientLeft: rect.left,
            clientHeight: rect.height
        };
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

    // Public: Select from the last trigger to the caret so we can replace it from C#
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
        const pre = probe.toString();

        const lastAt = pre.lastIndexOf(triggerChar || '@');
        if (lastAt < 0) return false;

        // Walk to the text node and offset that match lastAt
        const walker = document.createTreeWalker(block, NodeFilter.SHOW_TEXT, null);
        let remaining = lastAt, startNode = null, startOffset = 0, n;
        while (n = walker.nextNode()) {
            const len = n.nodeValue.length;
            if (remaining <= len) { startNode = n; startOffset = remaining; break; }
            remaining -= len;
        }
        if (!startNode) return false;

        const selRange = document.createRange();
        selRange.setStart(startNode, startOffset); // at the trigger char
        selRange.setEnd(range.endContainer, range.endOffset); // caret
        sel.removeAllRanges();
        sel.addRange(selRange);
        return true;
    }

    // Return just the two functions we want to be accessible
    return {
        getTokenInfo,
        selectFromTriggerToCaret
    };
})();