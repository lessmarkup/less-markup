define(['app'], function(app) {
    var controllerFunction = function($scope, $modalInstance, message, title, success) {
        $scope.title = title;
        $scope.message = message;
        $scope.submitError = "";
        $scope.isApplying = false;

        $scope.submit = function () {
            $scope.submitError = "";
            if (typeof (success) == "function") {
                $scope.isApplying = true;
                success(function () {
                    $scope.isApplying = false;
                    $modalInstance.close();
                }, function (message) {
                    $scope.isApplying = false;
                    $scope.submitError = message;
                });
            } else {
                $modalInstance.close();
            }
        };

        $scope.cancel = function () {
            $modalInstance.dismiss('cancel');
        }
    }

    return controllerFunction;
});
