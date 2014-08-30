app.filter('localeDate', function() {
    return function (date) {
        if (typeof (date) == "undefined" || date == null) {
            return "";
        }
        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        return date.toLocaleDateString();
    }
});
