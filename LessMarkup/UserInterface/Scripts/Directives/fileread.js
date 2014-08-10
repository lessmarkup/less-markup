/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.directive("fileread", [function () {
    return {
        restrict: 'A',
        replace: false,
        scope: {
            fileread: "="
        },
        link: function (scope, element) {
            $(element).css({
                'display': 'none'
            });

            var button = $("<button class=\"btn btn-default\" style=\"margin-right: 0.5em;\">Choose File</button>");
            var clear = $("<button class=\"btn btn-default\" style=\"margin-right: 0.5em;\">Clear</button>");
            var fileComment = $("<span></span>");

            $(element).after(button);
            button.after(clear);
            clear.after(fileComment);

            button.on('click', function() {
                $(element).click();
            });

            clear.on('click', function() {
                $(element).val('');
                fileComment.html('');
            });

            element.bind("change", function (changeEvent) {
                fileComment.html(changeEvent.target.files[0].name);
                var reader = new FileReader();
                reader.onload = function (loadEvent) {
                    scope.$apply(function () {

                        var value = loadEvent.target.result;

                        var pos = value.indexOf("base64,");
                        if (pos > 0) {
                            value = value.substring(pos + 7);
                        }

                        scope.fileread = {
                            File: value,
                            Type: changeEvent.target.files[0].type,
                            Name: changeEvent.target.files[0].name
                        };
                        
                    });
                }
                reader.readAsDataURL(changeEvent.target.files[0]);
            });
        }
    }
}]);

