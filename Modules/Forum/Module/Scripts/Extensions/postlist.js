define([], function() {

    return function ($scope) {

        $scope.users = {};

        $scope.onDataReceived = function($scope, data) {
            if (data.hasOwnProperty("users")) {
                for (var i = 0; i < data.users.length; i++) {
                    var user = data.users[i];
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
        }
    }

});