// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

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

    /**
     * Resolve the editable host (iframe + document + editable root) for a Radzen HTML editor.
     *
     * Supports two layouts:
     *  1) **Iframe-based**: Radzen wraps the editor content inside an <iframe>.
     *     - Returns the iframe element, its contentDocument, and the first [contenteditable="true"] element
     *       (falling back to <body> if none is found).
     *  2) **Inline (no iframe)**: The editor is rendered directly in the main document.
     *     - Attempts to find '.rz-html-editor-content [contenteditable="true"]', or the
     *       '.rz-html-editor-content' container, or finally the outer host itself.
     *
     * Returns `null` only if `hostSelector` does not match any element.
     *
     * @param {string} hostSelector - A CSS selector pointing to the Radzen editor container element.
     * @returns {{
     *   iframe: HTMLIFrameElement|null,
     *   doc: Document,
     *   editable: HTMLElement
     * } | null} An object describing the editing context, or null if the host is not found.
     */
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

    /**
     * Get the current Selection object from an editor host (e.g., iframe) if available.
     *
     * - Prefers the host's document selection (h.doc.getSelection) if present.
     * - Falls back to window.getSelection().
     * - Returns null if there's no valid selection or no ranges.
     *
     * @param {{ doc?: Document }} h - Host object that may contain an iframe or embedded document.
     * @returns {Selection|null} The Selection with at least one range, or null if unavailable.
     */
    function getSelection(h) {
        // Use the iframe's selection if available
        if (!h || !h.doc) return null;
        const sel = h.doc.getSelection ? h.doc.getSelection() : window.getSelection();
        return sel && sel.rangeCount > 0 ? sel : null;
    }

    /**
     * Return the closest element node for a given DOM Node.
     *
     * - If the node is an element, returns it directly.
     * - If it's a text or other node, returns its parentElement if present.
     * - Returns null if no element can be resolved.
     *
     * @param {Node|null} node - Any DOM node (Text, Element, etc.).
     * @returns {Element|null} The nearest element ancestor (or self), or null if not found.
     */
    function getClosestElement(node) {
        if (!node) return null;
        if (node.nodeType === Node.ELEMENT_NODE) return node;
        return node.parentElement || null;
    }

    /**
     * Find the nearest block-level ancestor from a given node up to a root element.
     *
     * - Walks up the DOM from the node to (but not beyond) `rootElement`.
     * - Recognizes common block containers: p, div, li, blockquote, pre.
     * - Returns `rootElement` as a safe fallback if none found.
     *
     * @param {Node|null} node - Starting node (often the selection's startContainer or a text node).
     * @param {Element} rootElement - The editor/content root element to limit search.
     * @returns {Element} The nearest block element, or `rootElement` if none matched.
     */
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

    /**
     * Compute a DOMRect for the caret (range end), using reliable fallbacks.
     *
     * Strategy:
     *  1) Try range.getClientRects(): use the last rect (most caret-proximal).
     *  2) Try range.getBoundingClientRect() if non-zero.
     *  3) Fallback: insert a zero-width marker (U+200B) span at the range,
     *     measure it, then remove it and restore the range collapsed at end.
     *
     * @param {Range} range - A live DOM Range (typically from selection).
     * @returns {DOMRect} A client rect approximating the caret position.
     */
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

    /**
     * Check whether the given node is inside an element with class "mention".
     *
     * - Walks up from the node's element (or its parentElement if it's a text node).
     * - Returns true if any ancestor has class "mention".
     *
     * @param {Node|null} node - Node to test (text or element).
     * @returns {boolean} True if the node is inside a ".mention" element.
     */
    function isInsideMention(node) {
        let el = node && (node.nodeType === Node.ELEMENT_NODE ? node : node.parentElement);
        while (el) {
            if (el.classList && el.classList.contains('mention')) return true;
            el = el.parentElement;
        }
        return false;
    }

    /**
     * Build text content from the start of a block element up to the caret,
     * masking '@' characters inside ".mention" elements so they do not trigger matching.
     *
     * - Uses a TreeWalker over text nodes within `block`.
     * - Collects exactly the number of characters represented by `rangeToCaret`.
     * - Replaces '@' with U+FFFF (non-character) inside ".mention" to suppress triggers.
     *
     * @param {Element} block - The block element root to start reading from.
     * @param {Range} rangeToCaret - A range whose toString() length matches characters up to the caret.
     * @returns {string} The accumulated (possibly masked) text up to the caret.
     */
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

    /**
     * Find the last trigger occurrence in text from a set of trigger strings,
     * ensuring it's at a token boundary (preceded by whitespace or punctuation).
     *
     * - Scans using String.lastIndexOf for each trigger and keeps the rightmost match.
     * - If found, validates that the preceding character is whitespace or punctuation
     *   to avoid matching in the middle of a word.
     *
     * @param {string} text - The source text to search.
     * @param {string[]} triggers - List of trigger tokens (e.g., ["@", "#"] or multi-char).
     * @returns {{ index: number, trigger: string } | null}
     *          Object with index of the last occurrence and the first char of the trigger,
     *          or null if none valid.
     */
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


    /**
     * Bind a keydown handler inside the editor (iframe or inline) that blocks Enter,
     * Tab, ArrowUp/ArrowDown, and Escape **when the autocomplete UI is active**.
     *
     * - Uses a retry loop because the iframe content may not be ready immediately.
     * - Attaches the handler in the *capture* phase so it fires before
     *   contenteditable default behaviour.
     * - Prevents default behaviour for navigation keys when `active` is true.
     * - Does NOT stop propagation: Blazor still receives @onkeydown events.
     *
     * @param {string} hostSelector - CSS selector for the Radzen editor wrapper.
     */
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
                if (
                    e.key === 'Enter' ||
                    e.key === 'Tab' ||
                    e.key === 'ArrowUp' ||
                    e.key === 'ArrowDown' ||
                    e.key === 'Escape'
                ) {
                    e.preventDefault(); // stop newline, tabbing, and caret movement
                    // Intentionally not stopping propagation
                }
            }, true);
        };
        attemptBind();
    }

    /**
     * Set whether the autocomplete UI is currently active.
     *
     * When `active` is true:
     *   - bindKeydown() will suppress Enter/Tab/Arrows/Escape.
     *
     * @param {boolean} value - True if autocomplete is active.
     */
    function setActive(value) {
        active = !!value;
    }

    /**
     * Compute token/trigger information near the caret inside the editor.
     *
     * Returns:
     *   - Whether a trigger (e.g. '@' or '#') is present immediately before caret.
     *   - The extracted text after the trigger (the query).
     *   - Caret coordinates relative to the editor container.
     *
     * Steps:
     *  1. Resolve the editing host (iframe or inline).
     *  2. Get the current caret range.
     *  3. Determine the block element containing the caret.
     *  4. Clone the range so it spans from the block start to caret
     *     → this gives the "pre" text.
     *  5. Build a *masked* version of that text so '@' inside .mention elements
     *     does NOT trigger autocomplete.
     *  6. Identify the last valid trigger in the text.
     *  7. Compute caret screen rect, convert to container-relative coords.
     *
     * @param {string} hostSelector - Selector for the editor wrapper element.
     * @param {string} triggerList - Comma-separated triggers (e.g. "@,#").
     * @returns {{
     *   hasTrigger: boolean,
     *   trigger?: string,
     *   text?: string,
     *   clientTop: number,
     *   clientLeft: number,
     *   clientHeight: number
     * }}
     */
    function getTokenInfo(hostSelector, triggerList) {
        const h = getEditableHost(hostSelector);
        if (!h || !h.editable) return { hasTrigger: false };

        const sel = getSelection(h);
        if (!sel) return { hasTrigger: false };

        const range = sel.getRangeAt(0);
        if (!h.editable.contains(range.endContainer)) return { hasTrigger: false };

        const block = getClosestBlock(range.endContainer, h.editable);

        // Clone a range covering the entire block up to the caret
        const probe = range.cloneRange();
        probe.setStart(block, 0);

        // Masked pre so @ inside mentions isn't detected
        const preMasked = buildPreMasked(block, probe);
        const triggers = (triggerList || "@,#").split(",").map(s => s.trim()).filter(Boolean);
        const last = lastTriggerIndex(preMasked, triggers);

        // --- compute caret rect in viewport coords ---
        const rect = getCaretRect(range);

        // Find the element relative to which UI should be positioned
        const hostEl = document.querySelector(hostSelector);
        const container = (hostEl && hostEl.closest('.editor-wrapper')) || hostEl || document.body;

        // Convert viewport rect to container-relative rect
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
        const text = preReal.substring(index + 1);

        return {
            hasTrigger: true,
            trigger: trigger,
            text: text,
            clientTop: relTop,
            clientLeft: relLeft,
            clientHeight: relHeight
        };
    }

    /**
     * Select the text from the last trigger character up to the caret.
     *
     * This allows external code (e.g., Blazor) to replace that section
     * with the chosen mention/autocomplete value.
     *
     * Process:
     *  1. Locate caret and containing block.
     *  2. Build masked PRE text to find the correct trigger index.
     *     (Masked so '@' inside .mention elements is ignored.)
     *  3. Walk text nodes to convert trigger index → actual DOM position.
     *  4. Construct a new range from trigger → caret and apply it.
     *
     * @param {string} hostSelector - Selector for the editor wrapper.
     * @param {string} triggerChar - The trigger character (default '@').
     * @returns {boolean} True if the selection was successfully set.
     */
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

        // Masked text to find trigger correctly
        const preMasked = buildPreMasked(block, probe);
        const trig = (triggerChar || '@');
        const lastAt = preMasked.lastIndexOf(trig);
        if (lastAt < 0) return false;

        // Walk text nodes to locate actual node + offset
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
        newRange.setStart(startNode, startOffset); // at trigger char
        newRange.setEnd(range.endContainer, range.endOffset); // caret

        sel.removeAllRanges();
        sel.addRange(newRange);
        return true;
    }

    // Exported methods
    return {
        bindKeydown,
        setActive,
        getTokenInfo,
        selectFromTriggerToCaret
    };
})();