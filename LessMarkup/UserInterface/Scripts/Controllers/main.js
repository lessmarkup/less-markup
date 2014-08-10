/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

function scopeProperty(scope, name) {
    for (;;) {
        if (scope.hasOwnProperty("name")) {
            return scope.name;
        }
        if (!scope.hasOwnProperty("$parent")) {
            return undefined;
        }
        scope = scope.$parent;
    }
}

app.controller('main', function ($scope, $http, commandHandler, inputForm, $location, $browser, $timeout, lazyLoad) {
    var initialData = window.viewInitialData;
    window.viewInitialData = null;

    $scope.toolbarButtons = [];
    $scope.templates = {};
    $scope.title = "";
    $scope.breadcrumbs = [];
    $scope.viewData = null;
    $scope.staticNodes = {};
    $scope.path = "";
    $scope.loginUserEmail = "";
    $scope.loginUserPassword = "";
    $scope.loginUserRemember = false;
    $scope.userLoggedIn = initialData.UserLoggedIn;
    $scope.userName = initialData.UserName;
    $scope.userLoginError = "";
    $scope.userLoginProgress = false;
    $scope.alerts = [];
    $scope.hasLogin = initialData.HasLogin;
    $scope.hasSearch = initialData.HasSearch;
    $scope.showConfiguration = initialData.ShowConfiguration;
    $scope.configurationPath = initialData.ConfigurationPath;
    $scope.rootPath = initialData.RootPath;
    $scope.rootTitle = initialData.RootTitle;
    $scope.navigationTree = initialData.NavigationTree;
    $scope.hasNavigationTree = $scope.navigationTree != null && $scope.navigationTree.length > 0;
    $scope.topMenu = initialData.TopMenu;
    $scope.profilePath = initialData.ProfilePath;
    $scope.getViewScope = function () { return $scope; }
    $scope.showXsMenu = false;

    var browserUrl = $browser.url();
    // dirty hack to prevent AngularJS from reloading the page on pushState and fix $location.$$parse bug
    $browser.url = function () {
        return browserUrl;
    }

    $(window).on('popstate', function () {
        $scope.navigateToView(location.pathname);
    });

    $scope.isToolbarButtonEnabled = function (id) {
        return commandHandler.isEnabled(id, this);
    }

    $scope.onToolbarButtonClick = function (id) {
        commandHandler.invoke(id, this);
    }

    $scope.doLogout = function () {
        $http.post("", {
            "-command-": "Logout"
        }).success(function (data) {
            if (!data.Success) {
                $scope.showError(data.Message);
                return;
            }

            $scope.showConfiguration = false;
            $scope.userLoggedIn = false;
            $scope.userName = "";
            $scope.staticNodes = {};
            $scope.navigateToView("/");
        });
    }

    $scope.showError = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'danger',
            id: $scope.alertId++
        });
    }

    $scope.showWarning = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'warning',
            id: $scope.alertId++
        });
    }

    $scope.showMessage = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'success',
            id: $scope.alertId++
        });
    }

    $scope.doRegister = function () {
        $scope.sendCommand("GetRegisterObject", {}, function (data) {
            var registerObject = data.RegisterObject;
            var modelId = data.ModelId;
            inputForm.editObject(registerObject, modelId, function (object, success, failure) {
                $scope.sendCommand("Register", { user: object }, function (data) {
                    $scope.userLoggedIn = true;
                    $scope.userName = data.UserName;
                    $scope.showConfiguration = data.ShowConfiguration;
                    $scope.loginUserPassword = "";
                    $scope.loginUserEmail = "";
                    $scope.loginUserRemember = false;
                    $scope.staticNodes = {};
                    $scope.navigateToView($scope.path);
                    success();
                }, failure);
            });
        }, $scope.showError);
    }

    $scope.doLogin = function (administratorKey) {
        $scope.userLoginError = "";

        var userEmail = $scope.loginUserEmail.trim();
        var userPassword = $scope.loginUserPassword.trim();

        if (userEmail.length == 0 || userPassword.length == 0) {
            $scope.userLoginError = "Please fill all required fields";
            return;
        }

        if (!/\b[\w\.-]+@[\w\.-]+\.\w{2,4}\b/ig.test(userEmail)) {
            $scope.userLoginError = "Invalid e-mail";
            return;
        }

        $scope.userLoginProgress = true;

        function addLoginError(status) {
            if (status > 0) {
                $scope.userLoginError = "Request failed, unknown communication error";
            } else {
                $scope.userLoginError = "Request failed, status: " + status.toString();
            }
            $scope.userLoginProgress = false;
        }

        var stage1Data = {
            "-command-": "LoginStage1",
            user: userEmail
        };

        if (administratorKey) {
            stage1Data.administratorKey = administratorKey;
        }

        $http.post("", stage1Data).success(function (data) {
            if (!data.Success) {
                $scope.userLoggedIn = false;
                $scope.userLoginProgress = false;
                $scope.userLoginError = data.Message;
                return;
            }

            require(['lib/sha512'], function() {
                var pass1 = CryptoJS.SHA512(data.Data.Pass1 + userPassword);
                var pass2 = CryptoJS.SHA512(data.Data.Pass2 + pass1);

                var stage2Data = {
                    "-command-": "LoginStage2",
                    user: userEmail,
                    hash: data.Data.Pass2 + ';' + pass2,
                    remember: $scope.loginUserRemember
                }

                if (administratorKey) {
                    stage2Data.administratorKey = administratorKey;
                }

                $http.post("", stage2Data).success(function (data) {
                    $scope.userLoginProgress = false;
                    if (!data.Success) {
                        $scope.userLoginError = data.Message;
                        $scope.userLoggedIn = false;
                        return;
                    }
                    $scope.userLoggedIn = true;
                    $scope.userName = data.Data.UserName;
                    $scope.showConfiguration = data.Data.ShowConfiguration;
                    $scope.loginUserPassword = "";
                    $scope.loginUserEmail = "";
                    $scope.loginUserRemember = false;
                    $scope.staticNodes = {};

                    var path = data.Data.Path;

                    if (!path || path.length == 0) {
                        path = $scope.path;
                    }

                    $scope.navigateToView(path);

                }).error(function (data, status) {
                    addLoginError(status);
                });
            });
        }).error(function (data, status) {
            addLoginError(status);
        });
    }

    $scope.alertId = 1;

    $scope.closeAlert = function (alertId) {
        for (var i = 0; i < $scope.alerts.length; i++) {
            if ($scope.alerts[i].id == alertId) {
                $scope.alerts.splice(i, 1);
                break;
            }
        }
    }

    $scope.sendAction = function (action, data, success, failure, path) {
        data["-action-"] = action;
        return $scope.sendCommand("Action", data, success, failure, path);
    }

    $scope.sendCommand = function (command, data, success, failure, path) {
        data["-command-"] = command;
        if (!path) {
            data["-path-"] = $scope.path;
        } else {
            data["-path-"] = path;
        }
        $scope.alerts = [];
        $http.post("", data).success(function (data) {
            validateLoggedIn(data.UserLoggedIn);
            if (!data.Success) {
                if (failure) {
                    failure(data.Message);
                } else {
                    $scope.showError(data.Message);
                }
                return;
            }
            if (success) {
                try {
                    success(data.Data);
                } catch (e) {
                    failure(e.toString());
                }
            } else {
                $scope.showMessage("Command successful");
            }
        }).error(function (data, status) {
            var message = status > 0 ? "Request failed, error " + status.toString() : "Request failed, unknown communication error";
            if (failure) {
                failure(message);
            } else {
                $scope.showError(message);
            }
        });
    }

    function validateLoggedIn(userLoggedIn) {
        if ($scope.userLoggedIn && !userLoggedIn) {
            $scope.userLoggedIn = false;
            $scope.userName = "";
            $scope.showConfiguration = false;
        }
    }

    $scope.getTypeahead = function (field, searchText) {
        return $scope.sendCommandAsync("Typeahead", { property: field.Property, searchText: searchText }, function (data) {
            return data.Records;
        });
    }

    $scope.sendActionAsync = function (action, data, success, failure, path) {
        data["-action-"] = action;
        return $scope.sendCommandAsync("Action", data, success, failure, path);
    }

    $scope.sendCommandAsync = function (command, data, success, failure, path) {
        data["-command-"] = command;
        if (!path) {
            data["-path-"] = $scope.path;
        } else {
            data["-path-"] = path;
        }
        $scope.alerts = [];
        return $http.post("", data).then(function (result) {
            data = result.data;
            validateLoggedIn(data.UserLoggedIn);
            if (!data.Success) {
                if (failure) {
                    failure(data.Message);
                } else {
                    $scope.showError(data.Message);
                }
                return null;
            }
            if (success) {
                try {
                    return success(data.Data);
                } catch (e) {
                    failure(e.toString());
                }
            } else {
                $scope.showMessage("Command successful");
            }
            return null;
        });
    }

    $scope.resetAlerts = function () {
        $scope.alerts = [];
    }

    function onNodeLoaded(data, url) {

        if (url.substring(0, 1) != '/') {
            url = "/" + url;
        }

        $scope.path = url;
        $scope.resetAlerts();

        var newFullPath = window.location.protocol + "//" + window.location.host + url;

        if (window.location.href != newFullPath) {
            history.pushState(newFullPath, data.Title, newFullPath);
        }

        var template;
        if (data.Template != null && data.Template.length > 0) {
            $scope.templates[data.TemplateId] = data.Template;
            template = data.Template;
        } else {
            template = $scope.templates[data.TemplateId];
        }

        if (data.IsStatic) {
            $scope.staticNodes[url] = data;
        }

        commandHandler.reset();

        var finishNodeLoaded = function() {
            $scope.toolbarButtons = data.ToolbarButtons;
            $scope.viewData = data.ViewData;
            $scope.breadcrumbs = data.Breadcrumbs;
            $scope.title = data.Title;
            $scope.$broadcast("onNodeLoaded", {});

            if (!$scope.bindBody) {
                $timeout(function() {
                    $scope.bindBody(template);
                    if (!$scope.$$phase) {
                        $scope.$apply();
                    }
                });
            } else {
                $scope.bindBody(template);
                if (!$scope.$$phase) {
                    $scope.$apply();
                }
            }
        }

        if (data.Require && data.Require != null && data.Require.length > 0) {
            require(data.Require, function() {
                finishNodeLoaded();
            });
        } else {
            finishNodeLoaded();
        }
    }

    $scope.hideXsMenu = function() {
        if ($scope.showXsMenu) {
            $scope.showXsMenu = false;
            if (!$scope.$$phase) {
                $scope.$apply();
            }
        }
    }

    $scope.getFullPath = function(path) {
        return $scope.path + "/" + path;
    }

    $scope.navigateToView = function (url) {
        $scope.hideXsMenu();

        if ($scope.staticNodes.hasOwnProperty(url)) {
            onNodeLoaded($scope.staticNodes[url], url);
            return false;
        }

        var cachedItems = [];
        for (var key in $scope.templateSources) {
            if (!$scope.templateSources.hasOwnProperty(key)) {
                continue;
            }
            cachedItems.push(key);
        }

        $http.post("", {
            "-command-": "View",
            "-cached-": cachedItems,
            "-path-": url
        }).success(function (data) {
            if (!data.Success) {
                $scope.showError(data.Message);
                return;
            }
            onNodeLoaded(data.Data, url);
        }).error(function (data, status) {
            $scope.showError(status > 0 ? "Request failed, error " + status.toString() : "Request failed, unknown communication error");
        });

        return false;
    };

    if (initialData.NodeLoadError && initialData.NodeLoadError.length > 0) {
        $scope.showError(initialData.NodeLoadError);
    }

    lazyLoad.initialize();
    onNodeLoaded(initialData.ViewData, initialData.Path);
});

