app.directive('ckEditor', [
    function () {

        var options = {};

        var smileIdToCode = {};
        var smileCodeToId = {};

        return {
            require: '?ngModel',
            link: function($scope, elm, attr, ngModel) {

                if ($scope.smilesBase && $scope.smiles && !options.hasOwnProperty("smiley_path")) {
                    options.smiley_path = $scope.smilesBase;
                    options.smiley_descriptions = [];
                    options.smiley_images = [];
                    for (var i = 0; i < $scope.smiles.length; i++) {
                        var smile = $scope.smiles[i];
                        smileCodeToId[smile.Code] = smile.Id;
                        smileIdToCode[smile.Id] = smile.Code;
                        options.smiley_descriptions.push(smile.Code);
                        options.smiley_images.push(smile.Id);
                    }
                }

                var ck = CKEDITOR.replace(elm[0], options);

                if (!ngModel) {
                    return;
                }

                function applyChanges() {
                    $scope.$apply(function () {

                        var text = ck.getData();

                        if ($scope.smilesExpr != null) {
                            text = text.replace(/<img\s+alt="([^"]*)"\s+src="[^"]*"\s+(?:style="[^"]*"\s+)?title="([^"]*)"\s+\/?>/gi, function (match, alt, title) {
                                if (!alt || !title || alt != title) {
                                    return match;
                                }
                                if (!smileCodeToId.hasOwnProperty(alt)) {
                                    return match;
                                }
                                return alt;
                            });
                        }

                        ngModel.$setViewValue(text);
                    });
                }

                ck.on('change', applyChanges);
                ck.on('key', applyChanges);

                ngModel.$render = function () {

                    var text = ngModel.$modelValue;

                    if ($scope.smilesExpr != null) {
                        text = $scope.smilesToImg(text);
                    }

                    ck.setData(text);
                };
            }
        };
    }
]);