define(['app'], function(app) {
    var controllerFunction = function($scope, $modalInstance, title, message) {
        $scope.title = title;
        $scope.message = message;

        $scope.submit = function () {
            $modalInstance.dismiss('cancel');
        }
    }

    return controllerFunction;
});
