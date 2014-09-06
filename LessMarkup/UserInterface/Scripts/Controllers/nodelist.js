/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

define([], function() {
    app.controller('nodelist', function ($scope, inputForm) {

        $scope.nodes = [];
        $scope.updateProgress = false;
        $scope.updateError = "";
        $scope.rootNode = $scope.viewData.root;

        function addNodeToFlatList(node, level, parent, parentIndex) {
            var target = {
                data: node,
                level: level,
                parent: parent,
                index: $scope.nodes.length,
                parentIndex: parentIndex
            }

            $scope.nodes.push(target);

            for (var i = 0; i < node.children.length; i++) {
                addNodeToFlatList(node.children[i], level + 1, target, i);
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
            return $scope.path + "/" + node.data.nodeId.toString() + "/access";
        }

        $scope.nodeEnabled = function (node) {
            for (; node != null; node = node.parent) {
                if (!node.data.enabled) {
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
            return node.parentIndex == node.parent.data.children.length - 1 && node.parent.parent == null;
        }

        $scope.leftDisabled = function (node) {
            return node.parent == null || node.index == 0;
        }

        $scope.rightDisabled = function (node) {
            return node.parentIndex == 0;
        }

        function changeParent(node, parent, order) {
            if (parent != null && order > parent.data.children.length) {
                order = parent.data.children.length;
            }

            $scope.sendCommand("UpdateParent", {
                nodeId: node.data.nodeId,
                parentId: parent != null ? parent.data.nodeId : null,
                order: order
            }, function (data) {
                $scope.rootNode = data.root;
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

            if (node.parentIndex + 1 < node.parent.data.children.length) {
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
                    node.parentId = null;
                    node.order = 0;
                } else {
                    node.parentId = parentNode.data.nodeId;
                    node.order = parentNode.data.children.length;
                }
                $scope.sendCommand("CreateNode", {
                    node: node
                }, function (data) {
                    if (parentNode == null) {
                        $scope.rootNode = data;
                    } else {
                        parentNode.data.children.push(data);
                    }
                    refreshFlatList();
                    success();
                }, function (message) {
                    error(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.canBeDeleted = function (node) {
            return node.data.children.length == 0;
        }

        $scope.deleteNode = function (node) {
            if (node.data.children.length != 0) {
                return;
            }

            inputForm.question("Do you want to delete node?", "Delete Nodes", function (success, fail) {
                $scope.sendCommand("DeleteNode", { id: node.data.nodeId }, function () {
                    if (node.parent == null) {
                        $scope.rootNode = null;
                    } else {
                        node.parent.data.children.splice(node.parentIndex, 1);
                    }
                    refreshFlatList();
                    success();
                }, function (message) {
                    fail(message);
                });
            });
        }

        $scope.hasSettings = function (node) {
            return node.data.customizable;
        }

        $scope.changeSettings = function (node) {
            if (!node.data.customizable) {
                return;
            }
            inputForm.editObject($scope, node.data.settings, node.data.settingsModelId, function (settings, success, fail) {
                $scope.sendCommand("ChangeSettings", {
                    nodeId: node.data.nodeId,
                    settings: settings
                }, function (data) {
                    node.data.settings = data;
                    refreshFlatList();
                    success();
                }, function (message) {
                    fail(message);
                });
            }, $scope.getTypeahead);
        }

        $scope.changeProperties = function (node) {
            inputForm.editObject($scope, node.data, $scope.viewData.nodeSettingsModelId, function (updatedNode, success, fail) {
                updatedNode.children = null;
                $scope.sendCommand("UpdateNode", {
                    node: updatedNode
                }, function (returnedNode) {

                    for (var property in returnedNode) {
                        if (property != "Children") {
                            node.data[property] = returnedNode[property];
                        }
                    }

                    refreshFlatList();
                    success();
                }, function (message) {
                    fail(message);
                });
            }, $scope.getTypeahead);
        }

        refreshFlatList();
    });
});
