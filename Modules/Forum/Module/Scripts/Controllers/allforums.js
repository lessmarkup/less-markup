app.controller("allforums", function($scope) {
    $scope.groups = $scope.viewData.groups;
    $scope.isSubForum = $scope.viewData.isSubForum;
    $scope.showStatistics = $scope.viewData.showStatistics;
    if ($scope.showStatistics) {
        $scope.activeUsers = $scope.viewData.activeUsers;
        $scope.statistics = $scope.viewData.statistics;
    }
});
