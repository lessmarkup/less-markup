app.controller("allforums", function($scope) {
    $scope.groups = $scope.viewData.Groups;
    $scope.isSubForum = $scope.viewData.IsSubForum;
});
