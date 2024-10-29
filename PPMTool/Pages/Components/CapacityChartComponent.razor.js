function disableMouseWheelZoom(id) {
    console.log('Disabling mouse wheel zoom on chart ' + id);
    try {
        ApexCharts.exec(id, 'updateOptions', {
            chart: {
                zoom: {
                    allowMouseWheelZoom: false
                }
            }
        }, false, true);
    }
    catch (error) {
        console.error('An error occurred:', error.message);
    }
}