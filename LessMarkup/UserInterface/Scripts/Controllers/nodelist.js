/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define(['app'], function(app) {
    app.controller('nodelist', function ($scope, inputForm) {

        $scope.nodes = [];
        $scope.updateProgress = false;
        $scope.updateError = "";

        $scope.getLevelStyle = function (node) {
            return {
                "padding-left": (node.data.Level * 35).toString() + "px"
            };
        }

        $scope.nodeAccessPage = function (node) {
            return $scope.path + "/" + node.data.NodeId.toString() + "/access";
        }

        $scope.nodeEnabled = function (node) {
            if (!node.data.Enabled) {
                return false;
            }

            var index = nodeIndex(node);
            var level = node.data.Level;

            for (index--; index >= 0; index--) {
                var previousNode = $scope.nodes[index];
                if (previousNode.data.Level >= level) {
                    continue;
                }
                if (!previousNode.data.Enabled) {
                    return false;
                }
                level = previousNode.data.Level;
            }

            return true;
        }

        function nodeIndex(node) {
            for (var i = 0; i < $scope.nodes.length; i++) {
                if ($scope.nodes[i].data.NodeId == node.data.NodeId) {
                    return i;
                }
            }
            return null;
        }

        $scope.upDisabled = function (node) {
            return nodeIndex(node) <= 1;
        }

        $scope.downDisabled = function (node) {
            var from = nodeIndex(node);
            var count = nodeScope(from);
            return from + count >= $scope.nodes.length;
        }

        $scope.leftDisabled = function (node) {
            var index = nodeIndex(node);
            if (index <= 1) {
                return true;
            }
            var previousNode = $scope.nodes[index - 1];
            return previousNode.data.Level >= node.data.Level;
        }

        $scope.rightDisabled = function (node) {
            var index = nodeIndex(node);
            if (index <= 1) {
                return true;
            }
            var previousNode = $scope.nodes[index - 1];
            return previousNode.data.Level < node.data.Level;
        }

        function createLayout() {
            var layout = [];

            for (var i = 0; i < $scope.nodes.length; i++) {
                var node = $scope.nodes[i].data;
                layout.push({
                    NodeId: node.NodeId,
                    Level: node.Level
                });
            }

            return layout;
        }

        function nodeScope(from) {
            var node = $scope.nodes[from];
            var count;
            for (count = 1; from + count < $scope.nodes.length; count++) {
                var nextNode = $scope.nodes[from + count];
                if (nextNode.data.Level <= node.data.Level) {
                    break;
                }
            }
            return count;
        }

        $scope.moveUp = function (node) {

            var from = nodeIndex(node);

            if (from <= 1) {
                return;
            }
            var count = nodeScope(from);

            from -= 1;

            var previousNode = $scope.nodes[from];
            var decreaseLevel = previousNode.data.Level < node.data.Level;

            var layout = createLayout();

            var previousLayout = layout[from];

            layout.splice(from, 1);
            layout.splice(from + count, 0, previousLayout);

            if (decreaseLevel) {
                for (var i = 0; i < count; i++) {
                    layout[from + i].Level--;
                }
            }

            updateLayout(layout, function () {
                $scope.nodes.splice(from, 1);
                $scope.nodes.splice(from + count, 0, previousNode);
                if (decreaseLevel) {
                    for (var i = 0; i < count; i++) {
                        $scope.nodes[from + i].data.Level--;
                    }
                }
                updateOrder();
            });
        }

        $scope.moveDown = function (node) {
            var from = nodeIndex(node);
            if (from >= $scope.nodes.length - 1) {
                return;
            }
            var count = nodeScope(from);

            var nextNode = $scope.nodes[from + count];

            var increaseLevel = nextNode.data.Level > node.data.Level;

            var layout = createLayout();

            var nextLayout = layout[from + count];

            layout.splice(from + count, 1);
            layout.splice(from, 0, nextLayout);

            if (increaseLevel) {
                for (var i = 0; i < count; i++) {
                    layout[i + from + 1].Level++;
                }
            }

            updateLayout(layout, function () {
                $scope.nodes.splice(from + count, 1);
                $scope.nodes.splice(from, 0, nextNode);
                if (increaseLevel) {
                    for (var i = 0; i < count; i++) {
                        $scope.nodes[i + from + 1].data.Level++;
                    }
                }
                updateOrder();
            });
        }

        $scope.moveLeft = function (node) {
            var from = nodeIndex(node);
            if (from <= 1) {
                return;
            }
            var count = nodeScope(from);
            var previousNode = $scope.nodes[from - 1];
            if (previousNode.data.Level >= node.data.Level) {
                return;
            }

            var layout = createLayout();

            for (var i = 0; i < count; i++) {
                layout[i + from].Level--;
            }

            updateLayout(layout, function () {
                for (var i = 0; i < count; i++) {
                    $scope.nodes[i + from].data.Level--;
                }
                updateOrder();
            });
        }

        $scope.moveRight = function (node) {
            var from = nodeIndex(node);
            if (from <= 1) {
                return;
            }
            var count = nodeScope(node);
            var previousNode = $scope.nodes[from - 1];
            if (previousNode.data.Level < node.data.Level) {
                return;
            }

            var layout = createLayout();

            for (var i = 0; i < count; i++) {
                layout[i + from].Level++;
            }

            updateLayout(layout, function () {
                for (var i = 0; i < count; i++) {
                    $scope.nodes[i + from].data.Level++;
                }
                updateOrder();
            });
        }

        function updateOrder() {
            for (var i = 0; i < $scope.nodes.length; i++) {
                $scope.nodes[i].data.Order = i;
            }
        }

        function updateLayout(layout, success) {
            $scope.updateProgress = true;
            $scope.updateError = "";
            $scope.sendAction("UpdateLayout", {
                layout: layout
            }, function () {
                $scope.updateProgress = false;
                success();
            }, function (message) {
                $scope.updateError = message;
                $scope.updateProgress = false;
            });
        }

        $scope.createNode = function (parentNode) {

            if (parentNode == null && $scope.nodes.length > 0) {
                return;
            }

            inputForm.editObject(null, $scope.viewData.NodeSettingsModelId, function (node, success, error) {
                var index;
                if (parentNode == null) {
                    // create root node
                    node.Order = 0;
                    node.Level = 0;
                    index = 0;
                } else {
                    index = nodeIndex(parentNode) + 1;
                    // create child node
                    node.Order = index;
                    node.Level = parentNode.data.Level + 1;
                }
                $scope.sendAction("CreateNode", {
                    node: node
                }, function (data) {
                    var newNode = {
                        data: data
                    };
                    $scope.nodes.splice(index, 0, newNode);
                    updateOrder();
                    success();
                }, function (message) {
                    error(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.canBeDeleted = function (node) {
            var index = nodeIndex(node);
            // we can remove only nodes with empty child list
            return index + 1 >= $scope.nodes.length || $scope.nodes[index + 1].data.Level <= node.data.Level;
        }

        $scope.deleteNode = function (node) {
            var from = nodeIndex(node);
            var count = nodeScope(from);

            var message = "Do you want to delete " + count.toString() + " nodes: ";

            var ids = [];
            for (var i = 0; i < count; i++) {
                if (i > 0) {
                    message += "; ";
                }
                message += $scope.nodes[i + from].data.Title;
                ids.push($scope.nodes[i + from].data.NodeId);
            }

            inputForm.question(message, "Delete Nodes", function (success, fail) {
                $scope.sendAction("DeleteNodes", { ids: ids }, function () {
                    for (var i = 0; i < count; i++) {
                        $scope.nodes.splice(from, 1);
                    }
                    updateOrder();
                    success();
                }, function (message) {
                    fail(message);
                });
            });
        }

        $scope.hasSettings = function (node) {
            return node.data.Customizable;
        }

        $scope.changeSettings = function (node) {
            if (!node.data.Customizable) {
                return;
            }
            inputForm.editObject(node.data.Settings, node.data.SettingsModelId, function (settings, success, fail) {
                $scope.sendAction("ChangeSettings", {
                    nodeId: node.data.NodeId,
                    settings: settings
                }, function (data) {
                    node.data.Settings = data;
                    success();
                }, function (message) {
                    fail(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.changeProperties = function (node) {
            var index = nodeIndex(node);
            inputForm.editObject(node.data, $scope.viewData.NodeSettingsModelId, function (updatedNode, success, fail) {
                $scope.sendAction("UpdateNode", {
                    node: updatedNode
                }, function (returnedNode) {
                    $scope.nodes[index].data = returnedNode;
                    updateOrder();
                    success();
                }, function (message) {
                    fail(message);
                });
            }, $scope.getTypeahead);
        }

        for (var i = 0; i < $scope.viewData.Nodes.length; i++) {
            var node = {
                data: $scope.viewData.Nodes[i]
            }
            $scope.nodes.push(node);
        }
    });
});
