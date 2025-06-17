function captureTimesheetAsImage(fileName) {
    // Hide some elements
    document.getElementById("timesheetButtons").style.display = "none";
    document.getElementById("downloadScreenshotButton").style.display = "none";
    document.getElementById("timesheetNoteAndComments").style.display = "none";
    document.getElementById("sidebar").style.display = "none";

    html2canvas(document.body).then(canvas => {
        // Restore after capture
        document.getElementById("timesheetButtons").style.display = "block";
        document.getElementById("downloadScreenshotButton").style.display = "block";
        document.getElementById("timesheetNoteAndComments").style.display = "block";
        document.getElementById("sidebar").style.display = "block";

        let link = document.createElement("a");
        link.href = canvas.toDataURL("image/png");
        link.download = fileName + ".png";
        link.click();
    });
}