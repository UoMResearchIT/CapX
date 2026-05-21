// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

function apexChartsUpdateAxis(id, minvalue, maxvalue) {
    console.log('Updating chart ' + id + ' with min: ' + minvalue + ' and max: ' + maxvalue);
    ApexCharts.exec(id, 'updateOptions', {
        xaxis: {
            min: minvalue,
            max: maxvalue
        }
    }, false, true);
}