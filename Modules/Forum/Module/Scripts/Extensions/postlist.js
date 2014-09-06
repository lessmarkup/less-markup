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
                    user.signature = $scope.getSafeValue($scope.getFriendlyHtml(user.signature));
                    if ($scope.users.hasOwnProperty(user.id)) {
                        var target = $scope.users[user.id];
                        for (var property in user) {
                            target[property] = user[property];
                        }
                    } else {
                        $scope.users[user.id] = user;
                    }
                }
            }

            function updateRecord(record) {

                var body = $scope.getFriendlyHtml(record.text);

                if (record.attachments) {
                    for (var i = 0; i < record.attachments.length; i++) {
                        var attachment = record.attachments[i];
                        if (!attachment.fileName) {
                            continue;
                        }
                        var extPos = attachment.fileName.lastIndexOf('.');
                        if (extPos < 0) {
                            continue;
                        }
                        var ext = attachment.fileName.substring(extPos + 1).toLowerCase();
                        if (ext == "jpg" || ext == "png" || ext == "gif") {
                            if (i == 0) {
                                body += "<br/>";
                            }
                            body += "<img src=\"" + attachment.url + "\"/><span> </span>";
                        }
                    }
                }

                record.safeText = $scope.getSafeValue(body);
                if (record.userId != null) {
                    record.user = $scope.users[record.userId];
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
                        if (lastRead == null || record.created > lastRead) {
                            lastRead = record.created;
                        }
                    }
                } else if (data.hasOwnProperty("record")) {
                    var record = data.record;
                    if (lastRead == null || record.created > lastRead) {
                        lastRead = record.created;
                    }
                }

                if (lastRead != null && ($scope.viewData.lastRead == null || $scope.viewData.lastRead < lastRead)) {
                    $scope.sendCommand("UpdateRead", {
                        lastRead: lastRead
                    }, function() {
                        $scope.viewData.lastRead = lastRead;
                    });
                }
            }
        }
    }
});