// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

let showFinishedAsSeparate = false

setFinishedFlag = function (finished) {
    showFinishedAsSeparate = finished
};

formatTooltip = function({ series, seriesIndex, dataPointIndex, w }) {
    var html = '<div class="p-2">';
    for (let i = 0; i < series.length; i++) {
        var value = series[i][dataPointIndex];
        var limit = 4;
        if (showFinishedAsSeparate == true) {
            limit = 6;
        }
        if (i < limit) {
            value -= series[i + 1][dataPointIndex];
        }
        html += '<span class="dot" style="background-color:' + w.globals.colors[i] + '"></span>' + w.globals.seriesNames[i] + ' : <b>' + value.toFixed(2) + '</b><br />'
    }
    return html;
}