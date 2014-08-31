define([], function() {

    return function ($scope) {

        $scope.users = {};
        $scope.userProperties = $scope.viewData.userProperties;

        $scope.onDataReceived = function ($scope, data) {

            if (data == null) {
                return;
            }

            if (data.hasOwnProperty("users")) {
                for (var i = 0; i < data.users.length; i++) {
                    var user = data.users[i];
                    user.Signature = $scope.getSafeValue($scope.getFriendlyHtml(user.Signature));
                    if ($scope.users.hasOwnProperty(user.Id)) {
                        var target = $scope.users[user.Id];
                        for (var property in user) {
                            target[property] = user[property];
                        }
                    } else {
                        $scope.users[user.Id] = user;
                    }
                }
            }

            function updateRecord(record) {
                record.safeText = $scope.getSafeValue($scope.getFriendlyHtml(record.Text));
                if (record.UserId != null) {
                    record.user = $scope.users[record.UserId];
                }
            }

            if (data.hasOwnProperty("records")) {
                for (var i = 0; i < data.records.length; i++) {
                    var record = data.records[i];
                    updateRecord(record);
                }
            }

            if (data.hasOwnProperty("record")) {
                updateRecord(data.record);
            }

            if ($scope.viewData.hasOwnProperty("lastRead")) {
                var lastRead = $scope.viewData.lastRead;
                if (data.hasOwnProperty("records")) {
                    for (var i = 0; i < data.records.length; i++) {
                        var record = data.records[i];
                        if (lastRead == null || record.Created > lastRead) {
                            lastRead = record.Created;
                        }
                    }
                } else if (data.hasOwnProperty("record")) {
                    var record = data.record;
                    if (lastRead == null || record.Created > lastRead) {
                        lastRead = record.Created;
                    }
                }

                if (lastRead != null && ($scope.viewData.lastRead == null || $scope.viewData.lastRead < lastRead)) {
                    $scope.sendAction("UpdateRead", {
                        lastRead: lastRead
                    }, function() {
                        $scope.viewData.lastRead = lastRead;
                    });
                }
            }
        }
    }
});