// Prevalue Editors
angular.module("umbraco").controller("Our.Umbraco.InnerContent.Controllers.DocTypePickerController", [

    "$scope",
    "innerContentService",

    function ($scope, innerContentService) {

        var vm = this;
        vm.add = add;
        vm.remove = remove;
        vm.tooltipMouseOver = tooltipMouseOver;
        vm.tooltipMouseLeave = tooltipMouseLeave;

        vm.sortableOptions = {
            axis: "y",
            containment: "parent",
            cursor: "move",
            handle: ".icon-navigation",
            opacity: 0.7,
            scroll: true,
            tolerance: "pointer",
            stop: function (e, ui) {
                setDirty();
            }
        };

        vm.tooltip = {
            show: false,
            event: null,
            content: null
        };

        innerContentService.getAllContentTypes().then(function (docTypes) {
            vm.docTypes = docTypes;
        });

        if (!$scope.model.value) {
            $scope.model.value = [];
            add();
        }

        function add() {
            $scope.model.value.push({
                icContentTypeGuid: "",
                nameTemplate: ""
            });
            setDirty();
        };

        function remove(index) {
            $scope.model.value.splice(index, 1);
            setDirty();
        };

        function tooltipMouseOver($event) {
            vm.tooltip = {
                show: true,
                event: $event,
                content: $event.currentTarget.dataset.tooltip
            };
        };

        function tooltipMouseLeave() {
            vm.tooltip = {
                show: false,
                event: null,
                content: null
            };
        };

        function setDirty() {
            if ($scope.propertyForm) {
                $scope.propertyForm.$setDirty();
            }
        };
    }
]);

// Property Editors
angular.module("umbraco").controller("Our.Umbraco.InnerContent.Controllers.InnerContentCreateController",
    [
        "$scope",
        "blueprintConfig",

        function ($scope, blueprintConfig) {

            function initialize() {

                $scope.allowedTypes = $scope.model.availableItems;
                $scope.allowBlank = blueprintConfig.allowBlank;

                if ($scope.allowedTypes.length === 1) {
                    $scope.selectedDocType = $scope.allowedTypes[0];
                    $scope.selectContentType = false;
                    $scope.selectBlueprint = true;
                } else {
                    $scope.selectContentType = true;
                    $scope.selectBlueprint = false;
                }
            };

            function createBlank(docTypeKey) {
                $scope.model.selectedItem = { "key": docTypeKey, "blueprint": null };
                $scope.model.submit($scope.model);
            };

            function createOrSelectBlueprintIfAny(docType) {
                var blueprintIds = _.keys(docType.blueprints || {});
                $scope.selectedDocType = docType;
                if (blueprintIds.length) {
                    if (blueprintConfig.skipSelect) {
                        createFromBlueprint(docType.key, blueprintIds[0]);
                    } else {
                        $scope.selectContentType = false;
                        $scope.selectBlueprint = true;
                    }
                } else {
                    createBlank(docType.key);
                }
            };

            function createFromBlueprint(docTypeKey, blueprintId) {
                $scope.model.selectedItem = { "key": docTypeKey, "blueprint": blueprintId };
                $scope.model.submit($scope.model);
            };

            $scope.createBlank = createBlank;
            $scope.createOrSelectBlueprintIfAny = createOrSelectBlueprintIfAny;
            $scope.createFromBlueprint = createFromBlueprint;

            initialize();
        }
    ]);

angular.module("umbraco").controller("Our.Umbraco.InnerContent.Controllers.InnerContentDialogController",
    [
        "$scope",
        "overlayHelper",

        function ($scope, overlayHelper) {
            $scope.item = $scope.model.dialogData.item;

            // Set a nodeContext property as nested property editors
            // can use this to know what doc type this node is etc
            // NC + DTGE do the same
            $scope.nodeContext = $scope.item;

            // When using doctype compositions, the tab Id may conflict with any nested inner-content items.
            // This attempts to make the tab ID to be unique.
            $scope.tabIdSuffix = "_" + $scope.item.contentTypeAlias + "_" + overlayHelper.getNumberOfOverlays();
        }
    ]);

// Directives
angular.module("umbraco.directives").directive("innerContentOverlay", [

    "$q",
    "innerContentService",

    function ($q, innerContentService) {

        function link(scope, el, attr, ctrl) {

            scope.config.editorModels = scope.config.editorModels || {};
            scope.currentItem = null;

            var getContentType = function (guid) {
                return _.find(scope.config.contentTypes, function (ct) {
                    return ct.icContentTypeGuid.toLowerCase() === guid.toLowerCase();
                });
            };

            // Helper function to createEditorModel but at the same time
            // cache the scaffold so that if we create another item of the same
            // content type, we don't need to fetch the scaffold again
            var createEditorModel = function (contentType, dbModel, blueprintId) {

                var process = function (editorModel, dbModel2) {
                    var n = angular.copy(editorModel);
                    n.key = innerContentService.generateUid(); // Create new ID for item
                    return innerContentService.extendEditorModel(n, dbModel2);
                };

                var cacheKey = contentType.icContentTypeGuid + ":" + blueprintId;
                if (scope.config.editorModels.hasOwnProperty(cacheKey)) {
                    var res = process(scope.config.editorModels[cacheKey], dbModel);
                    return $q.when(res);
                } else {
                    return innerContentService.createEditorModel(contentType, dbModel, blueprintId).then(function (em) {
                        scope.config.editorModels[cacheKey] = em;
                        var res = process(scope.config.editorModels[cacheKey], dbModel);
                        return res;
                    });
                }

            };

            scope.contentTypePickerOverlay = {
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + "/innercontent/views/innercontent.create.html",
                title: "Insert Content",
                show: false,
                hideSubmitButton: true,
                closeButtonLabelKey: "general_cancel",
                submit: function (model) {
                    var ct = getContentType(model.selectedItem.key);
                    var bp = model.selectedItem.blueprint;
                    createEditorModel(ct, null, bp).then(function (em) {
                        scope.currentItem = em;
                        scope.closeContentTypePickerOverlay();
                        scope.openContentEditorOverlay();
                    });
                },
                close: function () {
                    scope.closeAllOverlays();
                }
            };

            scope.contentEditorOverlay = {
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + "/innercontent/views/innercontent.dialog.html",
                show: false,
                submitButtonLabelKey: "bulk_done",
                closeButtonLabelKey: "general_cancel",
                submit: function (model) {
                    if (scope.config.callback) {
                        // Convert model to basic model
                        scope.config.data.model = innerContentService.createDbModel(model.dialogData.item);

                        // Notify callback
                        scope.config.callback(scope.config.data);
                    }
                    scope.closeAllOverlays();
                },
                close: function () {
                    scope.closeAllOverlays();
                }
            };

            scope.openContentTypePickerOverlay = function () {

                if (scope.contentTypePickerOverlay.availableItems.length === 0) {
                    scope.closeAllOverlays();
                    return;
                }

                if (scope.contentTypePickerOverlay.availableItems.length === 1 && _.isEmpty(scope.contentTypePickerOverlay.availableItems[0].blueprints)) {
                    var ct = getContentType(scope.contentTypePickerOverlay.availableItems[0].key);
                    createEditorModel(ct).then(function (em) {
                        scope.currentItem = em;
                        scope.openContentEditorOverlay();
                    });
                } else {
                    scope.contentTypePickerOverlay.event = scope.config.event;
                    scope.contentTypePickerOverlay.show = true;
                }

            };

            scope.closeContentTypePickerOverlay = function () {
                scope.contentTypePickerOverlay.show = false;
            };

            scope.openContentEditorOverlay = function () {
                scope.contentEditorOverlay.title = "Edit " + scope.currentItem.contentTypeName;
                scope.contentEditorOverlay.dialogData = { item: scope.currentItem };
                scope.contentEditorOverlay.show = true;
            };

            scope.closeContentEditorOverlay = function () {
                scope.contentEditorOverlay.show = false;
            };

            scope.closeAllOverlays = function () {
                scope.closeContentTypePickerOverlay();
                scope.closeContentEditorOverlay();
                scope.config.show = false;
            };

            var initOpen = function () {

                // Map scaffolds to content type picker list
                scope.contentTypePickerOverlay.availableItems = scope.config.contentTypePickerItems;

                // Open relevant dialog
                if (!scope.config.data || !scope.config.data.model) {
                    scope.openContentTypePickerOverlay();
                } else {
                    var ct = getContentType(scope.config.data.model.icContentTypeGuid);
                    createEditorModel(ct, scope.config.data.model).then(function (em) {
                        scope.currentItem = em;
                        scope.openContentEditorOverlay();
                    });
                }

            };

            // Initialize
            if (scope.config) {

                // If overlay items haven't be initialized, then intialize them
                if (!scope.config.contentTypePickerItems) {

                    var guids = scope.config.contentTypes.map(function (itm) {
                        return itm.icContentTypeGuid;
                    });
                    innerContentService.getContentTypesByGuid(guids).then(function (contentTypes) {

                        // Cache items in the PE's config so we only request these once per PE instance
                        scope.config.contentTypePickerItems = contentTypes;

                        initOpen();

                    });

                } else {

                    initOpen();

                }

            }
        }

        var directive = {
            restrict: "E",
            replace: true,
            templateUrl: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + "/innercontent/views/innercontent.overlay.html",
            scope: {
                config: "="
            },
            link: link
        };

        return directive;

    }
]);

angular.module("umbraco.directives").directive("innerContentUnsavedChanges", [

    "$rootScope",

    function ($rootScope) {

        function link(scope) {

            scope.canConfirmClose = false;
            scope.showConfirmClose = false;

            // This is by no means ideal as we are overriding a core method to prevent the overlay closing
            // put without coding a custom overlay, I couldn't think of a better way of doing it. We'll
            // have to keep a close eye on the overlay api to ensure the method name doesn't change, but
            // for now it works.
            var overlayScope = scope;
            while (overlayScope.$id !== $rootScope.$id) {
                if (overlayScope.hasOwnProperty("overlayForm")) {
                    scope.canConfirmClose = true;
                    break;
                }
                overlayScope = overlayScope.$parent;
            }

            if (scope.canConfirmClose) {
                overlayScope.oldCloseOverLay = overlayScope.closeOverLay;
                overlayScope.closeOverLay = function () {
                    if (overlayScope.overlayForm && overlayScope.overlayForm.$dirty) {
                        scope.showConfirmClose = true;
                    } else {
                        overlayScope.oldCloseOverLay.apply(overlayScope);
                    }
                };
            }

            scope.confirmClose = function () {
                scope.showConfirmClose = false;
                overlayScope.oldCloseOverLay.apply(overlayScope);
            };

            scope.cancelClose = function () {
                scope.showConfirmClose = false;
            };

        }

        var directive = {
            restrict: "E",
            replace: true,
            templateUrl: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + "/innercontent/views/innercontent.unsavedchanges.html",
            link: link
        };

        return directive;

    }
]);

// Services
angular.module("umbraco").factory("innerContentService", [

    "$interpolate",
    "localStorageService",
    "Our.Umbraco.InnerContent.Resources.InnerContentResources",

    function ($interpolate, localStorageService, icResources) {

        var self = {};

        var getScaffold = function (contentType, blueprintId) {

            var process = function (scaffold) {

                // remove all tabs except the specified tab
                if (contentType.hasOwnProperty("icTabAlias")) {

                    var tab = _.find(scaffold.tabs, function (tab) {
                        return tab.id !== 0 && (tab.alias.toLowerCase() === contentType.icTabAlias.toLowerCase() || contentType.icTabAlias === "");
                    });
                    scaffold.tabs = [];
                    if (tab) {
                        scaffold.tabs.push(tab);
                    }

                } else {

                    if (self.compareCurrentUmbracoVersion("7.8", { zeroExtend: true }) < 0) {
                        // Remove general properties tab for pre 7.8 umbraco installs
                        scaffold.tabs.pop();
                    }

                }

                return scaffold;

            };

            if (blueprintId > 0) {
                return icResources.getContentTypeScaffoldByBlueprintId(blueprintId).then(process);
            } else {
                return icResources.getContentTypeScaffoldByGuid(contentType.icContentTypeGuid).then(process);
            }
        };

        self.populateName = function (itm, idx, contentTypes) {

            var contentType = _.find(contentTypes, function (itm2) {
                return itm2.icContentTypeGuid === itm.icContentTypeGuid;
            });

            var nameTemplate = contentType.nameTemplate || "Item {{$index+1}}";
            var nameExp = $interpolate(nameTemplate);

            if (nameExp) {

                // Inject temporary index property
                itm.$index = idx;

                // Execute the name expression
                var newName = nameExp(itm);
                if (newName && (newName = $.trim(newName)) && itm.name !== newName) {
                    itm.name = newName;
                }

                // Remove temporary index property
                delete itm.$index;
            }

        };

        self.getAllContentTypes = function () {
            return icResources.getAllContentTypes();
        };

        self.getContentTypesByGuid = function (guids) {
            return icResources.getContentTypesByGuid(guids);
        };

        self.getContentTypeIconsByGuid = function (guids) {
            return icResources.getContentTypeIconsByGuid(guids);
        };

        self.createEditorModel = function (contentType, dbModel, blueprintId) {

            return getScaffold(contentType, blueprintId).then(function (scaffold) {

                scaffold.key = self.generateUid();
                scaffold.icContentTypeGuid = contentType.icContentTypeGuid;
                scaffold.name = "Untitled";

                return self.extendEditorModel(scaffold, dbModel);

            });

        };

        self.extendEditorModel = function (editorModel, dbModel) {

            editorModel.key = dbModel && dbModel.key ? dbModel.key : editorModel.key;
            editorModel.name = dbModel && dbModel.name ? dbModel.name : editorModel.name;

            if (!editorModel.key) {
                editorModel.key = self.generateUid();
            }

            if (dbModel) {
                for (var t = 0; t < editorModel.tabs.length; t++) {
                    var tab = editorModel.tabs[t];
                    for (var p = 0; p < tab.properties.length; p++) {
                        var prop = tab.properties[p];
                        if (dbModel.hasOwnProperty(prop.alias)) {
                            prop.value = dbModel[prop.alias];
                        }
                    }
                }
            }

            return editorModel;

        };

        self.createDbModel = function (model) {

            var dbModel = {
                key: model.key,
                name: model.name,
                icon: model.icon,
                icContentTypeGuid: model.icContentTypeGuid
            };

            for (var t = 0; t < model.tabs.length; t++) {
                var tab = model.tabs[t];
                for (var p = 0; p < tab.properties.length; p++) {
                    var prop = tab.properties[p];
                    if (typeof prop.value !== "function") {
                        dbModel[prop.alias] = prop.value;
                    }
                }
            }

            return dbModel;
        };

        self.createDefaultDbModel = function (contentType) {
            return self.createEditorModel(contentType).then(function (editorModel) {
                return self.createDbModel(editorModel);
            });
        };

        self.compareCurrentUmbracoVersion = function compareCurrentUmbracoVersion(v, options) {
            return this.compareVersions(Umbraco.Sys.ServerVariables.application.version, v, options);
        };

        self.compareVersions = function compareVersions(v1, v2, options) {

            var lexicographical = options && options.lexicographical,
                zeroExtend = options && options.zeroExtend,
                v1parts = v1.split("."),
                v2parts = v2.split(".");

            function isValidPart(x) {
                return (lexicographical ? /^\d+[A-Za-z]*$/ : /^\d+$/).test(x);
            }

            if (!v1parts.every(isValidPart) || !v2parts.every(isValidPart)) {
                return NaN;
            }

            if (zeroExtend) {
                while (v1parts.length < v2parts.length) {
                    v1parts.push("0");
                }
                while (v2parts.length < v1parts.length) {
                    v2parts.push("0");
                }
            }

            if (!lexicographical) {
                v1parts = v1parts.map(Number);
                v2parts = v2parts.map(Number);
            }

            for (var i = 0; i < v1parts.length; ++i) {
                if (v2parts.length === i) {
                    return 1;
                }

                if (v1parts[i] === v2parts[i]) {
                    continue;
                } else if (v1parts[i] > v2parts[i]) {
                    return 1;
                } else {
                    return -1;
                }
            }

            if (v1parts.length !== v2parts.length) {
                return -1;
            }

            return 0;

        };

        self.canCopyContent = function () {
            return localStorageService.isSupported;
        };

        self.canPasteContent = function () {
            return localStorageService.isSupported;
        };

        self.setCopiedContent = function (itm) {
            if (itm && itm.icContentTypeGuid) {
                localStorageService.set("icContentTypeGuid", itm.icContentTypeGuid);
                itm.key = undefined;
                localStorageService.set("icContentJson", itm);
                return true;
            }
            return false;
        };

        self.getCopiedContent = function () {
            var itm = localStorageService.get("icContentJson");
            itm.key = self.generateUid();
            return itm;
        };

        self.getCopiedContentTypeGuid = function () {
            return localStorageService.get("icContentTypeGuid");
        };

        // Helpful methods
        var lut = []; for (var i = 0; i < 256; i++) { lut[i] = (i < 16 ? "0" : "") + i.toString(16); }
        self.generateUid = function () {
            var d0 = Math.random() * 0xffffffff | 0;
            var d1 = Math.random() * 0xffffffff | 0;
            var d2 = Math.random() * 0xffffffff | 0;
            var d3 = Math.random() * 0xffffffff | 0;
            return lut[d0 & 0xff] + lut[d0 >> 8 & 0xff] + lut[d0 >> 16 & 0xff] + lut[d0 >> 24 & 0xff] + "-" +
                lut[d1 & 0xff] + lut[d1 >> 8 & 0xff] + "-" + lut[d1 >> 16 & 0x0f | 0x40] + lut[d1 >> 24 & 0xff] + "-" +
                lut[d2 & 0x3f | 0x80] + lut[d2 >> 8 & 0xff] + "-" + lut[d2 >> 16 & 0xff] + lut[d2 >> 24 & 0xff] +
                lut[d3 & 0xff] + lut[d3 >> 8 & 0xff] + lut[d3 >> 16 & 0xff] + lut[d3 >> 24 & 0xff];
        };

        return self;
    }

]);

// Resources
angular.module("umbraco.resources").factory("Our.Umbraco.InnerContent.Resources.InnerContentResources", [

    "$http",
    "umbRequestHelper",

    function ($http, umbRequestHelper) {
        return {
            getAllContentTypes: function () {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetAllContentTypes"),
                        method: "GET"
                    }),
                    "Failed to retrieve content types"
                );
            },
            getContentTypesByGuid: function (guids) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetContentTypesByGuid"),
                        method: "GET",
                        params: { guids: guids }
                    }),
                    "Failed to retrieve content types"
                );
            },
            getContentTypesByAlias: function (aliases) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetContentTypesByAlias"),
                        method: "GET",
                        params: { aliases: aliases }
                    }),
                    "Failed to retrieve content types"
                );
            },
            getContentTypeIconsByGuid: function (guids) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetContentTypeIconsByGuid"),
                        method: "GET",
                        params: { guids: guids }
                    }),
                    "Failed to retrieve content type icons"
                );
            },
            getContentTypeScaffoldByGuid: function (guid) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetContentTypeScaffoldByGuid"),
                        method: "GET",
                        params: { guid: guid }
                    }),
                    "Failed to retrieve content type scaffold by Guid"
                );
            },
            getContentTypeScaffoldByBlueprintId: function (blueprintId) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/GetContentTypeScaffoldByBlueprintId"),
                        method: "GET",
                        params: { blueprintId: blueprintId }
                    }),
                    "Failed to retrieve content type scaffold by blueprint Id"
                );
            },
            createBlueprintFromContent: function (data, userId) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        url: umbRequestHelper.convertVirtualToAbsolutePath("~/umbraco/backoffice/InnerContent/InnerContentApi/CreateBlueprintFromContent"),
                        method: "POST",
                        params: { userId: userId },
                        data: data
                    }),
                    "Failed to create blueprint from content"
                );
            }
        };
    }
]);
