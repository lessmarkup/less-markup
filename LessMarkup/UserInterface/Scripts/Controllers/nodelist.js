/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([], function() {
    app.controller('nodelist', function ($scope, inputForm) {

        $scope.nodes = [];
        $scope.updateProgress = false;
        $scope.updateError = "";
        $scope.rootNode = $scope.viewData.Root;

        function addNodeToFlatList(node, level, parent, parentIndex) {
            var target = {
                data: node,
                level: level,
                parent: parent,
                index: $scope.nodes.length,
                parentIndex: parentIndex
            }

            $scope.nodes.push(target);

            for (var i = 0; i < node.Children.length; i++) {
                addNodeToFlatList(node.Children[i], level + 1, target, i);
            }
        }

        function refreshFlatList() {
            $scope.nodes = [];

            if ($scope.rootNode != null) {
                addNodeToFlatList($scope.rootNode, 0, null, 0);
            }
        }

        $scope.getLevelStyle = function (node) {
            return {
                "padding-left": (node.level * 35).toString() + "px"
            };
        }

        $scope.nodeAccessPage = function (node) {
            return $scope.path + "/" + node.data.NodeId.toString() + "/access";
        }

        $scope.nodeEnabled = function (node) {
            for (; node != null; node = node.parent) {
                if (!node.data.Enabled) {
                    return false;
                }
            }
            return true;
        }

        $scope.upDisabled = function (node) {
            return node.index == 0;
        }

        $scope.downDisabled = function (node) {
            if (node.parent == null) {
                return true;
            }
            return node.parentIndex == node.parent.data.Children.length - 1 && node.parent.parent == null;
        }

        $scope.leftDisabled = function (node) {
            return node.parent == null || node.index == 0;
        }

        $scope.rightDisabled = function (node) {
            return node.parentIndex == 0;
        }

        function changeParent(node, parent, order) {
            if (parent != null && order > parent.data.Children.length) {
                order = parent.data.Children.length;
            }

            $scope.sendAction("UpdateParent", {
                nodeId: node.data.NodeId,
                parentId: parent != null ? parent.data.NodeId : null,
                order: order
            }, function (data) {
                $scope.rootNode = data.Root;
                refreshFlatList();
            }, function (message) {
                inputForm.message(message);
            });
        }

        $scope.moveUp = function (node) {
            if (node.index <= 0) {
                return;
            }

            if (node.index == 1) {
                changeParent(node, null, 0);
                return;
            }

            if (node.parent == null) {
                return;
            }

            if (node.parentIndex > 0) {
                changeParent(node, node.parent, node.parentIndex - 1);
                return;
            }

            if (node.parent.parent == null) {
                return;
            }

            changeParent(node, node.parent.parent, node.parent.parentIndex);
        }

        $scope.moveDown = function (node) {
            if (node.parent == null) {
                return;
            }

            if (node.parentIndex + 1 < node.parent.data.Children.length) {
                changeParent(node, node.parent, node.parentIndex + 1);
                return;
            }

            if (node.parent.parent == null) {
                return;
            }

            changeParent(node, node.parent.parent, node.parent.parentIndex + 1);
        }

        $scope.moveLeft = function (node) {
            if (node.parent == null) {
                return;
            }

            changeParent(node, node.parent.parent, node.parent.parentIndex);
        }

        $scope.moveRight = function (node) {
            if (node.parentIndex == 0) {
                return;
            }

            changeParent(node, $scope.nodes[node.index - 1], 0);
        }

        $scope.createNode = function (parentNode) {

            if (parentNode == null && $scope.nodes.length > 0) {
                return;
            }

            inputForm.editObject($scope, null, $scope.viewData.NodeSettingsModelId, function (node, success, error) {
                if (parentNode == null) {
                    // create root node
                    node.ParentId = null;
                    node.Order = 0;
                } else {
                    node.ParentId = parentNode.data.NodeId;
                    node.Order = parentNode.data.Children.length;
                }
                $scope.sendAction("CreateNode", {
                    node: node
                }, function (data) {
                    if (parentNode == null) {
                        $scope.rootNode = data;
                    } else {
                        parentNode.data.Children.push(data);
                    }
                    refreshFlatList();
                    success();
                }, function (message) {
                    error(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.canBeDeleted = function (node) {
            return node.data.Children.length == 0;
        }

        $scope.deleteNode = function (node) {
            if (node.data.Children.length != 0) {
                return;
            }

            inputForm.question("Do you want to delete node?", "Delete Nodes", function (success, fail) {
                $scope.sendAction("DeleteNode", { id: node.data.NodeId }, function () {
                    if (node.parent == null) {
                        $scope.rootNode = null;
                    } else {
                        node.parent.data.Children.splice(node.parentIndex, 1);
                    }
                    refreshFlatList();
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
            inputForm.editObject($scope, node.data.Settings, node.data.SettingsModelId, function (settings, success, fail) {
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
            inputForm.editObject($scope, node.data, $scope.viewData.NodeSettingsModelId, function (updatedNode, success, fail) {
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

        refreshFlatList();
    });
});
