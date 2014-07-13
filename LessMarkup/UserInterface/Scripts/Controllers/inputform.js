/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['app'], function (app) {

    app.ensureModule('ui.codemirror');
    app.ensureModule('ui.tinymce');

    var controllerFunction = function($scope, $modalInstance, definition, object, success, getTypeahead) {

        $scope.definition = definition;
        $scope.validationErrors = {};
        $scope.isModal = $modalInstance != null;
        $scope.submitError = "";
        $scope.isApplying = false;

        $scope.codeMirrorDefaultOptions = {
            mode: 'text/html',
            lineNumbers: true,
            lineWrapping: true,
            indentWithTabs: true,
            theme: 'default',
            extraKeys: {
                "F11": function (cm) {
                    cm.setOption("fullScreen", !cm.getOption("fullScreen"));
                },
                "Esc": function (cm) {
                    if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false);
                }
            }
        };

        $scope.isNewObject = object == null;

        $scope.object = object != null ? jQuery.extend({}, object) : {};

        $scope.fields = [];

        for (var i = 0; i < definition.Fields.length; i++) {
            var field = definition.Fields[i];
            if (!$scope.object.hasOwnProperty(field.Property)) {
                if (typeof (field.DefaultValue) != "undefined") {
                    $scope.object[field.Property] = field.DefaultValue;
                } else {
                    $scope.object[field.Property] = "";
                }
            }
            if (field.Type == 'Password') {
                $scope.object[field.Property] = "";
                $scope.object[field.Property + "-Repeat"] = "";
            }

            if (field.Type != 'Hidden') {
                $scope.fields.push(field);
            }

            if (field.Type == 'Select' && field.SelectValues.length > 0) {
                $scope.object[field.Property] = field.SelectValues[0].Value;
            }

            if (typeof (field.VisibleCondition) != "undefined" && field.VisibleCondition != null && field.VisibleCondition.length > 0) {
                field.VisibleFunction = new Function("obj", "with(obj) { return " + field.VisibleCondition + "; }");
            } else {
                field.VisibleFunction = null;
            }

            if (typeof (field.ReadOnlyCondition) != "undefined" && field.ReadOnlyCondition != null && field.ReadOnlyCondition.length > 0) {
                field.ReadOnlyFunction = new Function("obj", "with(obj) { return " + field.ReadOnlyCondition + "; }");
            } else {
                field.ReadOnlyFunction = null;
            }
        }

        $scope.hasErrors = function (property) {
            return $scope.validationErrors.hasOwnProperty(property);
        }

        $scope.errorText = function (property) {
            return $scope.validationErrors[property];
        }

        $scope.helpText = function (field) {
            var ret = field.HelpText;
            if (ret == null) {
                ret = "";
            }
            if ($scope.hasErrors(field.Property)) {
                if (ret.length) {
                    ret += " / ";
                }
                ret += $scope.errorText(field.Property);
            }
            return ret;
        }

        $scope.fieldVisible = function (field) {
            if (field.VisibleFunction == null) {
                return true;
            }
            return field.VisibleFunction($scope.object);
        }

        $scope.getTypeahead = function (field, searchText) {
            if (typeof (getTypeahead) != "function") {
                return [];
            }
            return getTypeahead(field, searchText);
        }

        $scope.readOnly = function (field) {
            if (field.ReadOnlyFunction == null) {
                return "";
            }
            return field.ReadOnlyFunction($scope.object) ? "readonly" : "";
        }

        $scope.submit = function () {
            var valid = true;

            $scope.validationErrors = {}

            for (var i = 0; i < definition.Fields.length; i++) {
                var field = definition.Fields[i];

                if (!$scope.fieldVisible(field) || $scope.readOnly(field)) {
                    continue;
                }

                var value = $scope.object[field.Property];

                if (field.Type == 'File') {
                    if (field.Required && $scope.isNewObject && (value == null || value.length == 0)) {
                        $scope.validationErrors[field.Property] = "Field is required";
                        valid = false;
                    }
                    else {
                        var pos = value.indexOf("base64,");
                        if (pos > 0) {
                            $scope.object[field.Property] = value.substring(pos + 7);
                        }
                    }
                    continue;
                }

                if (typeof (value) == 'undefined' || value == null || value.toString().trim().length == 0) {
                    if (field.Required) {
                        $scope.validationErrors[field.Property] = "Field is required";
                        valid = false;
                    }
                    continue;
                }

                switch (field.Type) {
                    case 'Number':
                        if (parseFloat(value) == NaN) {
                            $scope.validationErrors[field.Property] = "Field '" + field.Text + "' is not a number";
                            valid = false;
                        }
                        break;
                    case 'Email':
                        if (!value.search(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}/)) {
                            $scope.validationErrors[field.Property] = "Field'" + field.Text + "' is not an e-mail";
                            valid = false;
                        }
                        break;
                    case 'Password':
                        var repeatPassword = $scope.object[field.Property + "-Repeat"];
                        if (typeof (repeatPassword) == 'undefined' || repeatPassword == null || repeatPassword != value) {
                            $scope.validationErrors[field.Property] = 'Passwords must be equal';
                            valid = false;
                        }
                }
            }

            if (!valid) {
                return;
            }

            $scope.submitError = "";

            if (typeof (success) == "function") {
                $scope.isApplying = true;
                try {
                    success($scope.object, function () {
                        $scope.isApplying = false;
                        $modalInstance.close();
                    }, function (message) {
                        $scope.isApplying = false;
                        $scope.submitError = message;
                    });
                } catch (err) {
                    $scope.isApplying = false;
                    $scope.submitError = err.toString();
                }
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

