// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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