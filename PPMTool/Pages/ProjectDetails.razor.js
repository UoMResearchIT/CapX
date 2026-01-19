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
    let active = false;

    // ---------- Helpers ----------
    function getRelativePosition(rect, container) {
        const cRect = container.getBoundingClientRect();
        return {
            top: rect.top - cRect.top,
            left: rect.left - cRect.left,
            height: rect.height
        };
    }

    function getEditableHost(hostSelector) {
        const outer = document.querySelector(hostSelector);
        if (!outer) return null;

        const iframe = outer.querySelector("iframe");
        if (iframe && iframe.contentDocument) {
            const doc = iframe.contentDocument;
            const editable =
                (doc.body && doc.body.querySelector("[contenteditable='true']")) || doc.body;
            return { iframe, doc, editable };
        }

        // Fallback when not using an iframe
        const editable =
            outer.querySelector('.rz-html-editor-content [contenteditable="true"]') ||
            outer.querySelector('.rz-html-editor-content') ||
            outer;
        return { iframe: null, doc: document, editable };
    }

    function getSelection(h) {
        // Use the iframe's selection if available
        if (!h || !h.doc) return null;
        const sel = h.doc.getSelection ? h.doc.getSelection() : window.getSelection();
        return sel && sel.rangeCount > 0 ? sel : null;
    }

    function getClosestElement(node) {
        if (!node) return null;
        if (node.nodeType === Node.ELEMENT_NODE) return node;
        return node.parentElement || null;
    }

    function getClosestBlock(node, rootElement) {
        let el = getClosestElement(node);
        while (el && el !== rootElement) {
            const name = el.nodeName && el.nodeName.toLowerCase();
            if (name && (name === 'p' || name === 'div' || name === 'li' || name === 'blockquote' || name === 'pre')) {
                return el;
            }
            el = el.parentElement;
        }
        return rootElement;
    }

    function getCaretRect(range) {
        // Try the native rect first
        const rects = range.getClientRects && range.getClientRects();
        if (rects && rects.length) return rects[rects.length - 1];
        const rect = range.getBoundingClientRect && range.getBoundingClientRect();
        if (rect && rect.height !== 0 && rect.width !== 0) return rect;

        // Fallback marker
        const span = range.commonAncestorContainer.ownerDocument.createElement('span');
        span.appendChild(range.commonAncestorContainer.ownerDocument.createTextNode('\u200b'));
        range.insertNode(span);
        const r = span.getBoundingClientRect();
        span.parentNode.removeChild(span);
        range.collapse(false);
        return r;
    }

    // Mask @ signs inside nodes that are within .mention spans
    function isInsideMention(node) {
        let el = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        while (el) {
            if (el.classList && el.classList.contains('mention')) return true;
            el = el.parentElement;
        }
        return false;
    }

    // Build text from block start to caret, masking @ inside .mention so it won't trigger
    function buildPreMasked(block, rangeToCaret) {
        const targetLen = rangeToCaret.toString().length; // exact chars up to caret
        const doc = block.ownerDocument;
        const walker = doc.createTreeWalker(block, NodeFilter.SHOW_TEXT, null);
        let out = '', remaining = targetLen, n;
        while ((n = walker.nextNode()) && remaining > 0) {
            let t = n.nodeValue || '';
            if (t.length > remaining) t = t.slice(0, remaining);
            if (isInsideMention(n)) {
                t = t.replace(/@/g, '\uFFFF'); // mask @ so it isn't detected as a trigger
            }
            out += t;
            remaining -= t.length;
        }
        return out;
    }

    function lastTriggerIndex(text, triggers) {
        let best = -1, trig = null;
        for (const t of triggers) {
            const i = text.lastIndexOf(t);
            if (i > best) { best = i; trig = t; }
        }
        if (best < 0) return null;
        // Token start if preceded by whitespace or punctuation
        const prev = best === 0 ? " " : text[best - 1];
        if (/\s|[()[\]{}.,;:!?/"'\\-]/.test(prev)) {
            return { index: best, trigger: trig[0] };
        }
        return null;
    }

    // ---------- Public API ----------

    // Bind a keydown handler inside the editor (including iframe) to block Enter/Tab/Arrows when active
    function bindKeydown(hostSelector) {
        const attemptBind = () => {
            const h = getEditableHost(hostSelector);
            if (!h || !h.doc || !h.editable) {
                setTimeout(attemptBind, 50);
                return;
            }
            // Capture phase to preempt contenteditable defaults
            h.editable.addEventListener('keydown', (e) => {
                if (!active) return;
                if (e.key === 'Enter' || e.key === 'Tab' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'Escape') {
                    e.preventDefault(); // stop newline, tabbing, caret move
                    // Intentionally not stopping propagation: Blazor still receives @onkeydown
                }
            }, true);
        };
        attemptBind();
    }

    function setActive(value) {
        active = !!value;
    }

    // Returns token info near the caret, with caret rectangle (viewport coords)

    function getTokenInfo(hostSelector, triggerList) {
        const h = getEditableHost(hostSelector);
        if (!h || !h.editable) return { hasTrigger: false };

        const sel = getSelection(h);
        if (!sel) return { hasTrigger: false };

        const range = sel.getRangeAt(0);
        if (!h.editable.contains(range.endContainer)) return { hasTrigger: false };

        const block = getClosestBlock(range.endContainer, h.editable);

        const probe = range.cloneRange();
        probe.setStart(block, 0);

        // Masked pre so @ inside mentions isn't detected
        const preMasked = buildPreMasked(block, probe);
        const triggers = (triggerList || "@,#").split(",").map(s => s.trim()).filter(Boolean);
        const last = lastTriggerIndex(preMasked, triggers);

        // --- compute caret rect in viewport coords ---
        const rect = getCaretRect(range);

        // --- find the container we want to position relative to ---
        // hostSelector should be the Radzen editor wrapper (e.g., "#editor-entry")
        // We then look for the nearest .editor-wrapper; if not found, fall back to the host element.
        const hostEl = document.querySelector(hostSelector);
        const container = (hostEl && hostEl.closest('.editor-wrapper')) || hostEl || document.body;

        // Convert viewport rect to container-relative coordinates
        const containerRect = container.getBoundingClientRect();
        const relTop = rect.top - containerRect.top;
        const relLeft = rect.left - containerRect.left;
        const relHeight = rect.height;

        if (!last) {
            return {
                hasTrigger: false,
                clientTop: relTop,
                clientLeft: relLeft,
                clientHeight: relHeight
            };
        }

        // Get real (unmasked) text for the query
        const preReal = probe.toString();
        const { index, trigger } = last;
        const text = preReal.substring(index + 1); // chars after trigger up to caret

        return {
            hasTrigger: true,
            trigger: trigger,
            text: text,
            clientTop: relTop,
            clientLeft: relLeft,
            clientHeight: relHeight
        };
    }

    // Selects the range from the last trigger char to the caret (so Blazor can replace it)
    function selectFromTriggerToCaret(hostSelector, triggerChar) {
        const h = getEditableHost(hostSelector);
        if (!h || !h.editable) return false;

        const sel = getSelection(h);
        if (!sel) return false;

        const range = sel.getRangeAt(0);
        if (!h.editable.contains(range.endContainer)) return false;

        const block = getClosestBlock(range.endContainer, h.editable);

        const probe = range.cloneRange();
        probe.setStart(block, 0);

        // Use masked pre to find the correct trigger index (ignoring @ in mentions)
        const preMasked = buildPreMasked(block, probe);
        const trig = (triggerChar || '@');
        const lastAt = preMasked.lastIndexOf(trig);
        if (lastAt < 0) return false;

        // Walk text nodes to lastAt over REAL text (lengths match masked)
        const walker = h.doc.createTreeWalker(block, NodeFilter.SHOW_TEXT, null);
        let remaining = lastAt, startNode = null, startOffset = 0, n;
        while ((n = walker.nextNode())) {
            const len = (n.nodeValue || '').length;
            if (remaining <= len) {
                startNode = n;
                startOffset = remaining;
                break;
            }
            remaining -= len;
        }
        if (!startNode) return false;

        const newRange = h.doc.createRange();
        newRange.setStart(startNode, startOffset); // at the trigger char
        newRange.setEnd(range.endContainer, range.endOffset); // caret

        sel.removeAllRanges();
        sel.addRange(newRange);
        return true;
    }

    return {
        bindKeydown,
        setActive,
        getTokenInfo,
        selectFromTriggerToCaret
    };
})();