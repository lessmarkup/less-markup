/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['app', 'providers/lazyload'], function (app) {
    app.provider('inputForm', function () {
        var definitions = {};

        function getDefinition(type, $http, result) {
            if (definitions.hasOwnProperty(type)) {
                result(definitions[type]);
            } else {
                $http.post("", {
                    "-id-": type,
                    "-command-": "InputFormDefinition"
                }).success(function (data) {
                    if (!data.Success) {
                        result(null);
                        return;
                    }
                    definitions[type] = data.Data;
                    result(data.Data);
                });
            }
        }

        this.$get = ['$modal', '$http', '$location', 'lazyLoad',
            function ($modal, $http, $location, lazyLoad) {
                return {
                    editObject: function (object, type, success, getTypeahead) {
                        getDefinition(type, $http, function (definition) {
                            require([
                                'controllers/inputform',
                                'lib/codemirror/codemirror',
                                'lib/codemirror/ui-codemirror',
                                'lib/tinymce/tinymce',
                                'lib/tinymce/config',
                                'lib/tinymce/tinymce-angular'],
                                function (inputFormController) {
                                    app.ensureModule('ui.tinymce');
                                    app.ensureModule('ui.codemirror');
                                    lazyLoad.loadModules();
                                    $modal.open({
                                        template: $('#inputform-template').html(),
                                        controller: inputFormController,
                                        size: 'lg',
                                        resolve: {
                                            definition: function() { return definition; },
                                            object: function() { return object; },
                                            success: function() { return success; },
                                            getTypeahead: function() { return getTypeahead; }
                                        }
                                    }
                                );
                            });
                        });
                    },
                    question: function (message, title, success) {
                        require(['controllers/question'], function(questionController) {
                            $modal.open({
                                template: $('#inputform-question-template').html(),
                                controller: questionController,
                                resolve: {
                                    title: function () { return title; },
                                    message: function () { return message; },
                                    success: function () { return success; }
                                }
                            });
                        });
                    },
                    message: function (message, title) {
                        require(['controllers/message'], function(messageController) {
                            $modal.open({
                                template: $('inputform-message-template').html(),
                                controller: messageController,
                                resolve: {
                                    title: function() { return title; },
                                    message: function() { return message; }
                                }
                            });
                        });
                    }
                };
            }
        ];
    });
});
