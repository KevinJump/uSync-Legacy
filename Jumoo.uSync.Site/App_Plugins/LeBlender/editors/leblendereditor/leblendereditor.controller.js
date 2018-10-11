angular.module("umbraco").controller("LeBlenderEditor.controller",
    function ($scope, assetsService, $http, dialogService, $routeParams, umbRequestHelper, LeBlenderRequestHelper) {

        /***************************************/
        /* legacy adaptor 0.9.15 */
        /***************************************/

        $scope.dialogData = {
            editor: $scope.control.editor
        };

        if ($scope.dialogData.editor) {

            if ($scope.dialogData.editor.view == "/App_Plugins/LeBlender/core/LeBlendereditor.html") {
                $scope.dialogData.editor.view = "/App_Plugins/LeBlender/editors/leblendereditor/LeBlendereditor.html";
                $scope.dialogData.editor.render = "/App_Plugins/LeBlender/editors/leblendereditor/views/Base.cshtml"
            }

            if ($scope.dialogData.editor.view == "/App_Plugins/LeBlender/editors/leblendereditor/LeBlendereditor.html") {

                if ($scope.dialogData.editor.frontView) {
                    if (!$scope.dialogData.editor.config) {
                        $scope.dialogData.editor.config = {};
                    }
                    $scope.dialogData.editor.config.frontView = $scope.dialogData.editor.frontView;
                    delete $scope.dialogData.editor.frontView;
                }

                if ($scope.dialogData.editor.config) {

                    if ($scope.dialogData.editor.config.renderInGrid == true) {
                        $scope.dialogData.editor.config.renderInGrid = "1";
                    }

                    if ($scope.dialogData.editor.config.renderInGrid == false) {
                        $scope.dialogData.editor.config.renderInGrid = "0";
                    }

                    if ($scope.dialogData.editor.config.fixed != undefined &&
                        $scope.dialogData.editor.config.limit &&
                        !$scope.dialogData.editor.config.min &&
                        !$scope.dialogData.editor.config.max) {
                        if ($scope.dialogData.editor.config.fixed) {
                            $scope.dialogData.editor.config.min = $scope.dialogData.editor.config.limit;
                            $scope.dialogData.editor.config.max = $scope.dialogData.editor.config.limit;
                        }
                        else {
                            $scope.dialogData.editor.config.min = 1;
                            $scope.dialogData.editor.config.max = $scope.dialogData.editor.config.limit;
                        }
                        delete $scope.dialogData.editor.config.fixed;
                        delete $scope.dialogData.editor.config.limit;
                    }

                }
            }
        }

        /***************************************/
        /*  */
        /***************************************/

        $scope.preview = "";

        $scope.openListParameter = function () {
            if ($scope.control.editor.config && $scope.control.editor.config.editors ) {
        		var dialog = dialogService.open({
        		    template: '/App_Plugins/LeBlender/editors/leblendereditor/dialogs/parameterconfig.html',
        			show: true,
        			dialogData: {
        				name: $scope.control.editor.name,
        				value: angular.copy($scope.control.value),
        				config: $scope.control.editor.config
        			},
        			callback: function (data) {
        				$scope.control.value = data;
        				$scope.setPreview();
        				if (!$scope.control.guid)
        				    $scope.control.guid = guid()
        			}
        		});
        	}
        }

        var guid = function () {
            function s4() {
                return Math.floor((1 + Math.random()) * 0x10000)
                  .toString(16)
                  .substring(1);
            }
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
              s4() + '-' + s4() + s4() + s4();
        }

        if ((!$scope.control.value || $scope.control.value.length == 0) &&
            ($scope.control.editor.config && $scope.control.editor.config.editors && $scope.control.editor.config.editors.length > 0)) {
            $scope.openListParameter();
        }
        else {
            if (!$scope.control.guid)
                $scope.control.guid = guid()
        }

        $scope.setPreview = function () {
            if ($scope.control.editor.config
                && ($scope.control.value || !$scope.control.editor.config.editors || $scope.control.editor.config.editors.length == 0)
                && $scope.control.editor.config.renderInGrid && $scope.control.editor.config.renderInGrid != "0") {
                LeBlenderRequestHelper.GetPartialViewResultAsHtmlForEditor($scope.control).success(function (htmlResult) {
                    $scope.preview = htmlResult;
                });
            }
        };

        $scope.setPreview();

    	// Load css asset
        assetsService.loadCss("/App_Plugins/LeBlender/views_samples/sample_styles.css");

    });