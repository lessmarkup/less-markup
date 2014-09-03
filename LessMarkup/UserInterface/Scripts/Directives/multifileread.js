/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.directive("multiFileRead", [function () {

    var fileElementTemplate = "<input type=\"file\" style=\"display: none\" multiple />";
    var buttonChooseTemplate = "<button class=\"btn btn-default\" style=\"margin-right: 0.5em;\">[[[translate ChooseFile]]]</button>";
    var listElementsTemplate = "<ul class=\"file-choose-list\"></ul>";
    var fileItemTemplate = "<li></li>";
    var fileNameItemTemplate = "<span></span>";
    var fileRemoveItemTemplate = "<span class=\"file-remove glyphicon glyphicon-remove\"></span>";

    return {
        restrict: 'A',
        replace: false,
        scope: {
            multiFileRead: "=",
        },
        link: function (scope, element) {

            var fileId = 1;

            var listElements = $(listElementsTemplate);
            var fileElement = $(fileElementTemplate);
            var buttonChoose = $(buttonChooseTemplate);

            $(element).append(listElements);
            $(element).append(fileElement);
            $(element).append(buttonChoose);

            buttonChoose.on('click', function() {
                fileElement.click();
            });

            fileElement.bind('change', function (changeEvent) {
                var filesBefore = 0;
                if (scope.multiFileRead) {
                    filesBefore = scope.multiFileRead.length;
                }

                function readFile(sourceFile) {
                    var fileName = sourceFile.name;
                    var reader = new FileReader();
                    reader.onload = function (loadEvent) {
                        scope.$apply(function () {
                            var value = loadEvent.target.result;
                            var pos = value.indexOf("base64,");
                            if (pos > 0) {
                                value = value.substring(pos + 7);
                            }
                            var file = {
                                File: value,
                                Type: sourceFile.type,
                                Name: sourceFile.name,
                                Id: fileId++
                            };
                            if (scope.multiFileRead == null) {
                                scope.multiFileRead = [];
                            }
                            scope.multiFileRead.push(file);
                            if (scope.multiFileRead.length - filesBefore == sourceFiles.length) {
                                fileElement.val('');
                            }
                            var fileItem = $(fileItemTemplate);
                            var fileNameItem = $(fileNameItemTemplate);
                            var fileRemoveItem = $(fileRemoveItemTemplate);
                            fileNameItem.html(fileName);
                            fileItem.append(fileNameItem);
                            fileItem.append(fileRemoveItem);
                            listElements.append(fileItem);
                            fileRemoveItem.on('click', function () {
                                for (var i = 0; i < scope.multiFileRead.length; i++) {
                                    if (scope.multiFileRead[i].Id == file.Id) {
                                        scope.multiFileRead.splice(i, 1);
                                        break;
                                    }
                                }
                                fileItem.remove();
                            });
                        });
                    }
                    reader.readAsDataURL(sourceFile);
                }

                var sourceFiles = changeEvent.target.files;
                for (var i = 0; i < sourceFiles.length; i++) {
                    var sourceFile = sourceFiles[i];
                    readFile(sourceFile);
                }
            });
        }
    }
}]);

