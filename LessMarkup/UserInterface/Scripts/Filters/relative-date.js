app.filter('relativeDate', function() {
    return function(date) {
        var now = new Date();

        function calculateDelta() {
            return Math.round((now - date) / 1000);
        }

        if (!(date instanceof Date)) {
            date = new Date(date);
        }
        var delta = calculateDelta();
        var minute = 60;
        var hour = minute * 60;
        var day = hour * 24;
        var week = day * 7;
        var month = day * 30;
        var year = day * 365;
        if (delta > day && delta < week) {
            date = new Date(date.getFullYear(), date.getMonth(), date.getDate(), 0, 0, 0);
            delta = calculateDelta();
        }
        switch (true) {
        case (delta < 30):
            return '[[[translate JustNow]]]';
        case (delta < minute):
            return "" + delta + " [[[translate SecondsAgo]]]";
        case (delta < 2 * minute):
            return '[[[translate MinuteAgo]]]';
        case (delta < hour):
            return "" + (Math.floor(delta / minute)) + " [[[translate MinutesAgo]]]";
        case Math.floor(delta / hour) == 1:
            return '[[[translate HourAgo]]]';
        case (delta < day):
            return "" + (Math.floor(delta / hour)) + " [[[translate HoursAgo]]]";
        case (delta < day * 2):
            return '[[[translate Yesterday]]]';
        case (delta < week):
            return "" + (Math.floor(delta / day)) + " [[[translate DaysAgo]]]";
        case Math.floor(delta / week) == 1:
            return '[[[translate WeekAgo]]]';
        case (delta < month):
            return "" + (Math.floor(delta / week)) + " [[[translate WeeksAgo]]]";
        case Math.floor(delta / month) == 1:
            return '[[[translate MonthAgo]]]';
        case (delta < year):
            return "" + (Math.floor(delta / month)) + " [[[translate MonthsAgo]]]";
        case Math.floor(delta / year) == 1:
            return '[[[translate YearAgo]]]';
        default:
            return '[[[translate OverYearAgo]]]';
        }
    };
});
