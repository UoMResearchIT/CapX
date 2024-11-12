function highlightInCompetencies(keyword) {
    var context = document.querySelectorAll('.competency-highlightable');
    var instance = new Mark(context);
    instance.mark(keyword);
}

function clearHighlightInCompetencies() {
    var context = document.querySelectorAll('.competency-highlightable');
    var instance = new Mark(context);
    instance.unmark();
}