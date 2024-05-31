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