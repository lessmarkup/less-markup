/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

getApplication().directive("bindCompiledHtml", function ($compile) {
    return {
        template: '<div></div>',
        scope: {
            bindFunction: '@bindCompiledHtml',
        },
        link: function (scope, element) {
            scope.$parent[scope.bindFunction] = function(value) {
                element.contents().remove();
                if (value) {
                    element.append($compile(value)(scope.$parent));
                }
            };
        }
    };
});

getApplication().controller('main', function ($scope, $http, commandHandler, inputForm, $location, $browser) {
    var initialData = window.viewInitialData;
    window.viewInitialData = null;

    $scope.toolbarButtons = [];
    $scope.templates = {};
    $scope.title = "";
    $scope.breadcrumbs = [];
    $scope.viewData = null;
    $scope.staticPages = {};
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
    $scope.navigationBar = initialData.NavigationBar;
    $scope.topMenu = initialData.TopMenu;
    $scope.profilePath = initialData.ProfilePath;

    var browserUrl = $browser.url();
    // dirty hack to prevent AngularJS from reloading the page on pushState and fix $location.$$parse bug
    $browser.url = function() {
        return browserUrl;
    }

    $(window).on('popstate', function() {
        $scope.navigateToView(location.pathname);
    });

    $scope.isToolbarButtonEnabled = function(id) {
        return commandHandler.isEnabled(id, this);
    }

    $scope.onToolbarButtonClick = function(id) {
        commandHandler.invoke(id, this);
    }

    $scope.doLogout = function() {
        $http.post("", {
            "-command-": "Logout"
        }).success(function(data) {
            if (!data.Success) {
                $scope.showError(data.Message);
                return;
            }

            $scope.showConfiguration = false;
            $scope.userLoggedIn = false;
            $scope.userName = "";
            $scope.staticPages = {};
            $scope.navigateToView($scope.path);
        });
    }

    $scope.showError = function (message) {
        $scope.alerts.push({
            message: message,
            type: 'danger',
            id: $scope.alertId++
        });
    }

    $scope.showWarning = function(message) {
        $scope.alerts.push({
            message: message,
            type: 'warning',
            id: $scope.alertId++
        });
    }

    $scope.showMessage = function(message) {
        $scope.alerts.push({
            message: message,
            type: 'success',
            id: $scope.alertId++
        });
    }

    $scope.doLogin = function () {
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

        $http.post("", {
            "-command-": "LoginStage1",
            user: userEmail
        }).success(function(data) {
            if (!data.Success) {
                $scope.userLoggedIn = false;
                $scope.userLoginProgress = false;
                $scope.userLoginError = data.Message;
                return;
            }

            var pass1 = CryptoJS.SHA512(data.Data.Pass1 + userPassword);
            var pass2 = CryptoJS.SHA512(data.Data.Pass2 + pass1);

            $http.post("", {
                "-command-": "LoginStage2",
                user: userEmail,
                hash: data.Data.Pass2 + ';' + pass2,
                remember: $scope.loginUserRemember
            }).success(function(data) {
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
                $scope.staticPages = {};
                $scope.navigateToView($scope.path);

            }).error(function (data, status) {
                addLoginError(status);
            });

        }).error(function (data, status) {
            addLoginError(status);
        });
    }

    $scope.alertId = 1;

    $scope.closeAlert = function(alertId) {
        for (var i = 0; i < $scope.alerts.length; i++) {
            if ($scope.alerts[i].id == alertId) {
                $scope.alerts.splice(i, 1);
                break;
            }
        }
    }

    $scope.sendAction = function(action, data, success, failure) {
        data["-action-"] = action;
        return $scope.sendCommand("Action", data, success, failure);
    }

    $scope.sendCommand = function (command, data, success, failure) {
        data["-command-"] = command;
        data["-path-"] = $scope.path;
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

    $scope.getTypeahead = function(field, searchText) {
        return $scope.sendCommandAsync("Typeahead", { property: field.Property, searchText: searchText }, function (data) {
            return data.Records;
        });
    }

    $scope.sendActionAsync = function (action, data, success, failure) {
        data["-action-"] = action;
        return $scope.sendCommandAsync("Action", data, success, failure);
    }

    $scope.sendCommandAsync = function(command, data, success, failure) {
        data["-command-"] = command;
        data["-path-"] = $scope.path;
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

    $scope.resetAlerts = function() {
        $scope.alerts = [];
    }

    function onPageLoaded(data, url) {

        if (url.substring(0, 1) != '/') {
            url = "/" + url;
        }

        $scope.path = url;
        $scope.resetAlerts();

        var newFullPath = window.location.origin + url;

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
            $scope.staticPages[url] = data;
        }

        commandHandler.reset();

        $scope.toolbarButtons = data.ToolbarButtons;
        $scope.viewData = data.ViewData;
        $scope.breadcrumbs = data.Breadcrumbs;
        $scope.title = data.Title;
        $scope.bindBody(template);

        if (!$scope.$$phase) {
            $scope.$apply();
        }
    }

    $scope.navigateToView = function (url) {

        if ($scope.staticPages.hasOwnProperty(url)) {
            onPageLoaded($scope.staticPages[url], url);
            return;
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
        }).success(function(data) {
            if (!data.Success) {
                $scope.showError(data.Message);
                return;
            }
            onPageLoaded(data.Data, url);
        }).error(function (data, status) {
            $scope.showError(status > 0 ? "Request failed, error " + status.toString() : "Request failed, unknown communication error");
        });
    };

    if (initialData.PageLoadError && initialData.PageLoadError.length > 0) {
        $scope.showError(initialData.PageLoadError);
    }

    $.connection.hub.disconnected(function() {
        $scope.showError("Server callback connection is closed");
    });

    $.connection.hub.start().done(function() {
        onPageLoaded(initialData.ViewData, initialData.Path);
    }).fail(function() {
        $scope.showError("Cannot establish server callback connection");
    });
});
