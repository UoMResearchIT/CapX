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

    // Find the last '@' character
    var content = container.textContent;
    var lastAtIndex = content.lastIndexOf('@');

    // If the last '@' character is not found, return
    if (lastAtIndex == -1) return;

    // Create a new range starting after the last '@' character
    range.setStart(container, lastAtIndex + 1);
    range.setEnd(container, lastAtIndex + 1);

    var textNode = document.createTextNode(text);
    range.insertNode(textNode);

    // Move the cursor to the end of the inserted text
    range.setStartAfter(textNode);
    range.setEndAfter(textNode);
    sel.removeAllRanges();
    sel.addRange(range);
}


function copyText (text) {
    navigator.clipboard.writeText(text).then(function () {
        alert("Link to note copied to clipboard!");
    })
    .catch(function (error) {
        alert(error);
    });
};