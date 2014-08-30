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
    $scope.submitWithCaptcha = definition.SubmitWithCaptcha;

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

    $scope.getValue = function (object, field) {
        if (field.Type === "RichText" && $scope.readOnly(field)) {
            return $sce.trustAsHtml(object[field.Property]);
        }
        return object[field.Property];
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
            return field.ReadOnly ? "readonly" : "";
        }
        return field.ReadOnlyFunction($scope.object) ? "readonly" : "";
    }

    $scope.submit = function () {
        var valid = true;

        $scope.validationErrors = {}

        for (var i = 0; i < $scope.fields.length; i++) {
            var field = $scope.fields[i];

            if (!$scope.fieldVisible(field) || $scope.readOnly(field)) {
                continue;
            }

            var value = $scope.object[field.Property];

            if (field.Type == 'File') {
                if (field.Required && $scope.isNewObject && (value == null || value.length == 0)) {
                    $scope.validationErrors[field.Property] = "Field is required";
                    valid = false;
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
                case 'PasswordRepeat':
                    var repeatPassword = $scope.object[field.Property + "$Repeat"];
                    if (typeof (repeatPassword) == 'undefined' || repeatPassword == null || repeatPassword != value) {
                        $scope.validationErrors[field.Property] = 'Passwords must be equal';
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
                field.dynamicSource.Value = $scope.object[field.Property];
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
                        delete changed[field.Property];
                    } else if (field.Type == "PasswordRepeat") {
                        delete changed[field.Property + "$Repeat"];
                    }
                }

                for (var i = 0; i < definition.Fields.length; i++) {
                    var field = definition.Fields[i];
                    if (field.Type == 'DynamicFieldList') {
                        if ($scope.object == null) {
                            continue;
                        }

                        var dynamicFields = changed[field.Property];

                        for (var j = 0; j < dynamicFields.length; j++) {
                            var dynamicField = dynamicFields[j];
                            dynamicField.Field = {
                                Property: dynamicField.Field.Property
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
        if (field.Type == 'PasswordRepeat') {
            $scope.object[field.Property] = "";
            $scope.object[field.Property + "$Repeat"] = "";
        } else if (field.Type == 'Image' || field.Type == 'File') {
            $scope.object[field.Property] = null;
        }
        if (field.Type == 'Select' && field.SelectedValues != null && field.SelectValues.length > 0) {
            $scope.object[field.Property] = field.SelectValues[0].Value;
        }

        if (field.Type == 'Date') {
            field.isOpen = false;
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

    for (var i = 0; i < definition.Fields.length; i++) {
        var field = definition.Fields[i];
        if (!$scope.object.hasOwnProperty(field.Property)) {
            if (typeof (field.DefaultValue) != "undefined") {
                $scope.object[field.Property] = field.DefaultValue;
            } else {
                $scope.object[field.Property] = "";
            }
        }

        if (field.Type == 'DynamicFieldList') {
            if ($scope.object == null) {
                continue;
            }

            var dynamicFields = $scope.object[field.Property];

            if (dynamicFields == null) {
                continue;
            }

            for (var j = 0; j < dynamicFields.length; j++) {
                var dynamicField = dynamicFields[j];
                var dynamicDefinition = angular.copy(dynamicField.Field);
                dynamicDefinition.Property = field.Property + "$" + dynamicDefinition.Property;
                $scope.fields.push(dynamicDefinition);
                dynamicDefinition.dynamicSource = dynamicField;
                initializeField(dynamicDefinition);
                $scope.object[dynamicDefinition.Property] = dynamicField.Value;
            }
            continue;
        }

        if (field.Type != 'Hidden') {
            $scope.fields.push(field);
            initializeField(field);
        }
    }
}
