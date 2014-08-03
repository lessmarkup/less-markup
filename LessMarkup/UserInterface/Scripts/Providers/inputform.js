/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

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

    this.$get = ['$modal', '$http', 'lazyLoad',
        function ($modal, $http, lazyLoad) {
            return {
                editObject: function (object, type, success, getTypeahead) {
                    getDefinition(type, $http, function (definition) {

                        var hasTinymce = false;
                        var hasCodemirror = false;

                        var requires = [];

                        for (var i = 0; i < definition.Fields.length && (!hasTinymce || !hasCodemirror) ; i++) {
                            var field = definition.Fields[i];

                            if (field.Type == "RichText" && !hasTinymce) {
                                hasTinymce = true;
                                requires.push("lib/tinymce/tinymce");
                                requires.push("lib/tinymce/config");
                                requires.push("lib/tinymce/tinymce-angular");
                                app.ensureModule('ui.tinymce');
                            }

                            if (field.Type == "CodeText" && !hasCodemirror) {
                                hasCodemirror = true;
                                requires.push("lib/codemirror/codemirror");
                                requires.push("lib/codemirror/plugins/css");
                                requires.push("lib/codemirror/plugins/css-hint");
                                requires.push("lib/codemirror/plugins/dialog");
                                requires.push("lib/codemirror/plugins/anyword-hint");
                                requires.push("lib/codemirror/plugins/brace-fold");
                                requires.push("lib/codemirror/plugins/closebrackets");
                                requires.push("lib/codemirror/plugins/closetag");
                                requires.push("lib/codemirror/plugins/colorize");
                                requires.push("lib/codemirror/plugins/comment");
                                requires.push("lib/codemirror/plugins/comment-fold");
                                requires.push("lib/codemirror/plugins/continuecomment");
                                requires.push("lib/codemirror/plugins/foldcode");
                                requires.push("lib/codemirror/plugins/fullscreen");
                                requires.push("lib/codemirror/plugins/html-hint");
                                requires.push("lib/codemirror/plugins/htmlembedded");
                                requires.push("lib/codemirror/plugins/htmlmixed");
                                requires.push("lib/codemirror/plugins/indent-fold");
                                requires.push("lib/codemirror/plugins/javascript");
                                requires.push("lib/codemirror/plugins/javascript-hint");
                                requires.push("lib/codemirror/plugins/mark-selection");
                                requires.push("lib/codemirror/plugins/markdown-fold");
                                requires.push("lib/codemirror/plugins/match-highlighter");
                                requires.push("lib/codemirror/plugins/matchbrackets");
                                requires.push("lib/codemirror/plugins/matchtags");
                                requires.push("lib/codemirror/plugins/placeholder");
                                requires.push("lib/codemirror/plugins/rulers");
                                requires.push("lib/codemirror/plugins/scrollpastend");
                                requires.push("lib/codemirror/plugins/search");
                                requires.push("lib/codemirror/plugins/searchcursor");
                                requires.push("lib/codemirror/plugins/xml");
                                requires.push("lib/codemirror/plugins/xml-fold");
                                requires.push("lib/codemirror/plugins/xml-hint");
                                requires.push("lib/codemirror/ui-codemirror");
                                app.ensureModule('ui.codemirror');
                            }
                        }

                        function open() {
                            $modal.open({
                                template: $('#inputform-template').html(),
                                controller: InputFormController,
                                size: 'lg',
                                resolve: {
                                    definition: function () { return definition; },
                                    object: function () { return object; },
                                    success: function () { return success; },
                                    getTypeahead: function () { return getTypeahead; }
                                }
                            });
                        }

                        if (requires.length > 0) {
                            require(requires, function() {
                                lazyLoad.loadModules();
                                open();
                            });
                        } else {
                            open();
                        }

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
                            template: $('#inputform-message-template').html(),
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
