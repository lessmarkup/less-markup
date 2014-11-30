/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

app.directive("captcha", function() {
    return {
        restrict: 'A',
        scope: {
            parameter: '=captcha'
        },
        link: function(scope, element) {
            Recaptcha.create(scope.parameter, element[0], {
                theme: "clean"
            });
        }
    }
});

function InputFormController($scope, $modalInstance, definition, object, success, getTypeahead, $sce) {

    $scope.definition = definition;
    $scope.validationErrors = {};
    $scope.isModal = $modalInstance != null;
    $scope.submitError = "";
    $scope.isApplying = false;
    $scope.submitWithCaptcha = definition.submitWithCaptcha;

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

    $scope.okDisabled = function() {
        return $scope.isApplying;
    }

    $scope.isNewObject = object == null;

    $scope.object = object != null ? jQuery.extend({}, object) : {};

    $scope.fields = [];

    $scope.fieldValueSelected = function(object, field, select) {
        var value = object[field.property];
        switch (field.type) {
            case "Select":
                return select.value == value;
            case "MultiSelect":
                if (!value) {
                    return false;
                }
                for (var i = 0; i < value.length; i++) {
                    if (value[i] == select.value) {
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    $scope.getValue = function (object, field) {
        if (field.type === "RichText" && $scope.readOnly(field)) {
            return $sce.trustAsHtml(object[field.property]);
        }
        return object[field.property];
    }

    $scope.hasErrors = function (property) {
        return $scope.validationErrors.hasOwnProperty(property);
    }

    $scope.errorText = function (property) {
        return $scope.validationErrors[property];
    }

    $scope.helpText = function (field) {
        var ret = field.helpText;
        if (ret == null) {
            ret = "";
        }
        if ($scope.hasErrors(field.property)) {
            if (ret.length) {
                ret += " / ";
            }
            ret += $scope.errorText(field.property);
        }
        return ret;
    }

    $scope.fieldVisible = function (field) {
        if (!field.visibleFunction) {
            return true;
        }
        return field.visibleFunction($scope.object);
    }

    $scope.getTypeahead = function (field, searchText) {
        if (typeof (getTypeahead) != "function") {
            return [];
        }
        return getTypeahead(field, searchText);
    }

    $scope.readOnly = function (field) {
        if (!field.readOnlyFunction) {
            return field.readOnly ? "readonly" : "";
        }
        return field.readOnlyFunction($scope.object) ? "readonly" : "";
    }

    $scope.submit = function () {
        var valid = true;

        $scope.validationErrors = {}

        for (var i = 0; i < $scope.fields.length; i++) {
            var field = $scope.fields[i];

            if (!$scope.fieldVisible(field) || $scope.readOnly(field)) {
                continue;
            }

            var value = $scope.object[field.property];

            switch (field.type) {
                case 'File':
                    if (field.required && $scope.isNewObject && (value == null || value.file == null || value.file.length == 0)) {
                        $scope.validationErrors[field.property] = "Field is required";
                        valid = false;
                    }

                    if (value.file.length > $scope.maximumFileSize) {
                        $scope.validationErrors[field.property] = "File is too big";
                        valid = false;
                    }
                    continue;

                case 'FileList':
                    if (field.required && $scope.isNewObject && (value == null || value.length == 0)) {
                        $scope.validationErrors[field.property] = "Field is required";
                        valid = false;
                    }

                    if (value != null) {
                        for (var j = 0; j < value.length; j++) {
                            var file = value[j];
                            if (file.file != null && file.file.length > $scope.maximumFileSize) {
                                $scope.validationErrors[field.property] = "File is too big";
                                valid = false;
                                break;
                            }
                        }
                    }

                    continue;
                case "MultiSelect":
                    continue;
            }

            if (typeof (value) == 'undefined' || value == null || value.toString().trim().length == 0) {
                if (field.required) {
                    $scope.validationErrors[field.property] = "Field is required";
                    valid = false;
                }
                continue;
            }

            switch (field.type) {
                case 'Number':
                    if (parseFloat(value) == NaN) {
                        $scope.validationErrors[field.property] = "Field '" + field.text + "' is not a number";
                        valid = false;
                    }
                    break;
                case 'Email':
                    if (!value.search(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,4}/)) {
                        $scope.validationErrors[field.property] = "Field'" + field.text + "' is not an e-mail";
                        valid = false;
                    }
                    break;
                case 'PasswordRepeat':
                    var repeatPassword = $scope.object[field.property + "$Repeat"];
                    if (typeof (repeatPassword) == 'undefined' || repeatPassword == null || repeatPassword != value) {
                        $scope.validationErrors[field.property] = 'Passwords must be equal';
                        valid = false;
                    }
            }
        }

        if (!valid) {
            return;
        }

        for (var i = 0; i < $scope.fields.length; i++) {
            var field = $scope.fields[i];

            if (field.dynamicSource) {
                field.dynamicSource.value = $scope.object[field.property];
            }
        }

        if ($scope.submitWithCaptcha) {
            $scope.object["-RecaptchaChallenge-"] = Recaptcha.get_challenge();
            $scope.object["-RecaptchaResponse-"] = Recaptcha.get_response();
        }

        $scope.submitError = "";

        if (typeof (success) == "function") {
            $scope.isApplying = true;
            try {

                var changed = angular.copy($scope.object);

                for (var i = 0; i < $scope.fields.length; i++) {
                    var field = $scope.fields[i];
                    if (field.dynamicSource) {
                        delete changed[field.property];
                    } else if (field.type == "PasswordRepeat") {
                        delete changed[field.property + "$Repeat"];
                    }
                }

                for (var i = 0; i < definition.fields.length; i++) {
                    var field = definition.fields[i];
                    if (field.type == 'DynamicFieldList') {
                        if ($scope.object == null) {
                            continue;
                        }

                        var dynamicFields = changed[field.property];

                        if (dynamicFields != null) {
                            for (var j = 0; j < dynamicFields.length; j++) {
                                var dynamicField = dynamicFields[j];
                                dynamicField.field = {
                                    property: dynamicField.field.property
                                }
                            }
                        }
                    }
                }

                success(changed, function () {
                    $scope.isApplying = false;
                    $modalInstance.close();
                }, function (message) {
                    $scope.isApplying = false;
                    $scope.submitError = message;
                    if ($scope.submitWithCaptcha) {
                        Recaptcha.reload();
                    }
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

    $scope.showDateTimeField = function(event, field) {
        event.preventDefault();
        event.stopPropagation();
        field.isOpen = true;
    }

    function initializeField(field) {
        if (field.type == 'PasswordRepeat') {
            $scope.object[field.property] = "";
            $scope.object[field.property + "$Repeat"] = "";
        } else if (field.type == 'Image' || field.type == 'File') {
            $scope.object[field.property] = null;
        }
        if (field.type == 'Select' && field.selectValues != null && field.selectValues.length > 0) {
            $scope.object[field.property] = field.selectValues[0].value;
        }

        if (field.type == 'Date') {
            field.isOpen = false;
        }

        if (field.visibleCondition && field.visibleCondition.length > 0) {
            field.visibleFunction = new Function("obj", "with(obj) { return " + field.visibleCondition + "; }");
        } else {
            field.visibleFunction = null;
        }

        if (field.readOnlyCondition && field.readOnlyCondition.length > 0) {
            field.readOnlyFunction = new Function("obj", "with(obj) { return " + field.readOnlyCondition + "; }");
        } else {
            field.readOnlyFunction = null;
        }
    }

    for (var i = 0; i < definition.fields.length; i++) {
        var field = definition.fields[i];
        if (!$scope.object.hasOwnProperty(field.property)) {
            if (typeof (field.defaultValue) != "undefined") {
                $scope.object[field.property] = field.defaultValue;
            } else {
                $scope.object[field.property] = "";
            }
        }

        if (field.type == 'DynamicFieldList') {
            if ($scope.object == null) {
                continue;
            }

            var dynamicFields = $scope.object[field.property];

            if (dynamicFields == null) {
                continue;
            }

            for (var j = 0; j < dynamicFields.length; j++) {
                var dynamicField = dynamicFields[j];
                var dynamicDefinition = angular.copy(dynamicField.field);
                dynamicDefinition.property = field.property + "$" + dynamicDefinition.property;
                $scope.fields.push(dynamicDefinition);
                dynamicDefinition.dynamicSource = dynamicField;
                initializeField(dynamicDefinition);
                $scope.object[dynamicDefinition.property] = dynamicField.value;
            }
            continue;
        }

        if (field.type != 'Hidden') {
            $scope.fields.push(field);
            initializeField(field);
        }
    }
}
