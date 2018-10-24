angular.module("umbraco").controller("leblender.editormanager.edit",
    function ($scope, assetsService, $http, LeBlenderRequestHelper, dialogService, $routeParams, notificationsService, navigationService, contentEditingHelper, editorState) {


        /***************************************/
        /* legacy adaptor 0.9.15 */
        /***************************************/
        $scope.legacyAdaptor = function (editor) {

            if (editor) {
                                    
                if (editor.view == "/App_Plugins/Lecoati.LeBlender/core/LeBlendereditor.html" ||
                    editor.view == "/App_Plugins/Lecoati.LeBlender/editors/leblendereditor/LeBlendereditor.html") {
                    editor.view = "/App_Plugins/LeBlender/editors/leblendereditor/LeBlendereditor.html";
                    editor.render = "/App_Plugins/LeBlender/editors/leblendereditor/views/Base.cshtml"
                }

                if (editor.view == "/App_Plugins/LeBlender/editors/leblendereditor/LeBlendereditor.html") {

                    if (editor.frontView) {
                        if (!editor.config) {
                            editor.config = {};
                        }
                        editor.config.frontView = editor.frontView;
                        delete editor.frontView;
                    }

                    if (editor.config) {

                        if (editor.config.renderInGrid == true) {
                            editor.config.renderInGrid = "1";
                        }

                        if (editor.config.renderInGrid == false) {
                            editor.config.renderInGrid = "0";
                        }

                        if (editor.config.fixed != undefined &&
                            editor.config.limit &&
                            !editor.config.min &&
                            !editor.config.max) {
                            if (editor.config.fixed && (editor.config.fixed == true && editor.config.fixed == 1)) {
                                editor.config.min = editor.config.limit;
                                editor.config.max = editor.config.limit;
                            }
                            else {
                                editor.config.min = 1;
                                editor.config.max = editor.config.limit;
                            }
                            delete editor.config.fixed;
                            delete editor.config.limit;
                        }

                    }
                }
            }

        }


        /***************************************/
        /* Init editor data */
        /***************************************/
        $scope.getSetting = function (editorAlias) {
            LeBlenderRequestHelper.getGridEditors().then(function (response) {

                // init model
                $scope.editors  = response.data

                // Init model value
                $scope.model = {
                    value : {
                        name: "",
                        alias: "",
                        view: "",
                        icon: "icon-settings-alt"
                    }
                };

                if (editorAlias == -1) {
                    $scope.editors.push($scope.model.value);
                }
                else {
                    _.each($scope.editors, function (editor, editorIndex) {
                        if (editor.alias === editorAlias) {
                            $scope.legacyAdaptor(editor);
                            angular.extend($scope, {
                                model: {
                                    value: editor
                                }
                            });
                            navigationService.syncTree({ tree: "GridEditorManager", path: [$scope.model.value.alias], forceReload: false });
                        }
                    });
                }

                $scope.getConfigAsText();
                $scope.setSelectedPropertyGridEditor();
                $scope.initAutoPopulateAlias();
                $scope.loaded = true;
                $scope.$broadcast('gridEditorLoaded');
                
            })
        };


        /***************************************/
        /* grid editor */
        /***************************************/

        // init editor values
        $scope.initEditorFields = function () {
            delete $scope.model.value.config;
            $scope.model.value.render = "";
            $scope.textAreaconfig = "";
        }

        // save editor values
        $scope.save = function () {

            var submitPlease = true;
            if ($scope.model.value) {
                $scope.$broadcast('gridEditorSaving');
            }

            _.each($scope.editors, function (editor, editorIndex) {
                if (editor.render === "") {
                    delete editor.render;
                }
            });

            LeBlenderRequestHelper.setGridEditors($scope.editors).then(function (response) {
                notificationsService.success("Success", $scope.model.value.name + " has been saved");
                delete $scope.selectedPropertyGridEditor;
                $scope.getSetting($scope.model.value.alias);
                if ($scope.model.value) {
                    $scope.$broadcast('gridEditorSaved');
                }

                if ($routeParams.id == -1) {
                    var editormanagerForm = angular.element('form[name=editormanagerForm]').scope().editormanagerForm;
                    editormanagerForm.$dirty = false;
                    contentEditingHelper.redirectToCreatedContent($scope.model.value.alias, true);
                }

            });

        }

        // get config value 
        $scope.getConfigAsText = function () {

            $scope.textAreaconfig = "";

            if ($scope.model.value.config) {

                var config = JSON.stringify($scope.model.value.config, null, 4)

                if (config && config != {}) {
                    $scope.textAreaconfig = config;
                }
                else {
                    $scope.textAreaconfig = "";
                }


            }
            $scope.$watch('textAreaconfig', function () {
                try {
                    $scope.model.value.config = JSON.parse($scope.textAreaconfig);
                } catch (exp) {
                    //Exception handler
                };
            });
        };

        // open icon picker
        $scope.openIconPicker = function () {
            var dialog = dialogService.iconPicker({
                show: true,
                callback: function (data) {
                    $scope.model.value.icon = data;
                }
            });
        }



        /***************************************/
        /* property grid editor */
        /***************************************/

        //// init pge
        //$scope.propertyGridEditors = $scope.dialogData.propertyGridEditors;

        // search a pge by view
        $scope.searchPropertyGridEditor = function (view) {
            var sEditor = undefined;
            _.each($scope.propertyGridEditors, function (propertyGridEditor, editorIndex) {
                if (propertyGridEditor.editor && propertyGridEditor.editor.view === view) {
                    sEditor = propertyGridEditor
                }
            })
            return sEditor;
        }

        // set the selected pge
        $scope.setSelectedPropertyGridEditor = function () {
            $scope.selectedPropertyGridEditor = $scope.searchPropertyGridEditor($scope.model.value.view);
        }

        // init default Editor value for a new pge
        $scope.propertyGridEditorChanged = function () {
            $scope.setSelectedPropertyGridEditor();
            $scope.initEditorFields();
        }

        // get pge field view
        $scope.getFieldView = function (view) {
            if (view.indexOf('/') >= 0) {
                return view;
            }
            else {
                return '/umbraco/views/prevalueeditors/' + view + '.html';
            }
        }

        // check if current pge is custom 
        $scope.isCustom = function () {
            if ($scope.selectedPropertyGridEditor) {
                return false;
            }
            else {
                return true;
            }
        }

        /***************************************/
        /* autoPopulateAlias */
        /***************************************/

        // main method for autoPopulateAlias
        $scope.autoPopulateAlias = function (name) {
            var s = name.replace(/[^a-zA-Z0-9\s\.-]+/g, ''); 
            return s.toCamelCase();
        }

        // init autoPopulateAlias
        $scope.initAutoPopulateAlias = function () {
            if ($scope.model.value.name === "" && $scope.model.value.name === "") {
                $scope.$watch("model.value.name", function () {
                    $scope.model.value.alias = $scope.autoPopulateAlias($scope.model.value.name);
                })
            }
        }

        // toCamelCase
        var toCamelCase = function (name) {
            var s = name.toPascalCase();
            if ($.trim(s) == "")
                return "";
            if (s.length > 1)
                s = s.substr(0, 1).toLowerCase() + s.substr(1);
            else
                s = s.toLowerCase();
            return s;
        };

        // toPascalCase
        var toPascalCase = function (name) {
            var s = "";
            angular.each($.trim(name).split(/[\s\.-]+/g), function (val, idx) {
                if ($.trim(val) == "")
                    return;
                if (val.length > 1)
                    s += val.substr(0, 1).toUpperCase() + val.substr(1);
                else
                    s += val.toUpperCase();
            });
            return s;
        };

        /***************************************/
        /* init */
        /***************************************/

        // Init

        $scope.loaded = false;

        LeBlenderRequestHelper.getAllPropertyGridEditors().then(function (data) {
            $scope.propertyGridEditors = data;
            $scope.getSetting($routeParams.id);
        });

    });