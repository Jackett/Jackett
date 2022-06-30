
Handlebars.registerHelper('dateFormat', function (context, block) {
    if (window.moment) {
        var f = block.hash.format || "YYYY-MM-DD HH:mm:ss";
        return moment(context).format(f); //had to remove Date(context)
    } else {
        return context;   //  moment plugin not available. return data as is.
    }
});

Handlebars.registerHelper('jacketTimespan', function (context, block) {
    var now = moment();
    var from = moment(context);
    var timeSpan = moment.duration(now.diff(from));

    var minutes = timeSpan.asMinutes();
    if (minutes < 120) {
        return Math.round(minutes) + 'm ago';
    }

    var hours = timeSpan.asHours();
    if (hours < 48) {
        return Math.round(hours) + 'h ago';
    }

    var days = timeSpan.asDays();
    if (days < 365) {
        return Math.round(days) + 'd ago';
    }

    var years = timeSpan.asYears();
    return Math.round(years) + 'y ago';
});

Handlebars.registerHelper('jacketSize', function (context, block) {
    return filesize(context, { round: 2 });
});
