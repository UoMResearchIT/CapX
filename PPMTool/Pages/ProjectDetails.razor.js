function scrollToElement(id) {
    const e = document.getElementById(id);
    e.scrollIntoView();
    e.focus();
}

function highlightInNotes(keyword) {
    var context = document.querySelectorAll('.note-content');
    var instance = new Mark(context);
    instance.mark(keyword);
}

function clearHighlightInNotes() {
    var context = document.querySelectorAll('.note-content');
    var instance = new Mark(context);
    instance.unmark();
}

function insertTextAtCaret(text) {
    var sel = window.getSelection();
    var range = sel.getRangeAt(0);
    var container = range.commonAncestorContainer;

    // Get the text content from the start of the container to the cursor position
    var preCursorRange = range.cloneRange();
    preCursorRange.selectNodeContents(container);
    preCursorRange.setEnd(range.endContainer, range.endOffset);
    var preCursorContent = preCursorRange.toString();

    // Find the last '@' character before the cursor
    var lastAtIndex = preCursorContent.lastIndexOf('@');

    // If the last '@' character is not found, return
    if (lastAtIndex == -1) return;

    // Create a new range starting after the last '@' character
    var newRange = document.createRange();
    newRange.setStart(container, lastAtIndex + 1);
    newRange.setEnd(container, lastAtIndex + 1);

    var textNode = document.createTextNode(text);
    newRange.insertNode(textNode);

    // Move the cursor to the end of the inserted text
    newRange.setStartAfter(textNode);
    newRange.setEndAfter(textNode);
    sel.removeAllRanges();
    sel.addRange(newRange);
}

function copyText(text) {
    setTimeout(() => {
        navigator.clipboard.writeText(text).then(function () {
            alert("Link to note copied to clipboard!");
        })
            .catch(function (error) {
                alert(error);
            });
    }, 0);
};