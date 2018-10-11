angular.module("umbraco").controller("leblenderproperties.controller",
    function ($scope, $rootScope, assetsService, $http, LeBlenderRequestHelper, dialogService) {



        // Inir render with the value of frontView
        // render have to be always = /App_Plugins/LeBlender/editors/leblendereditor/views/Base.cshtml
        $scope.model.parentValue.render = $scope.model.parentValue.config.frontView ? $scope.model.parentValue.config.frontView : "";

        $scope.openPropertyConfig = function (parameter) {

            var dialog = dialogService.open({
                template: '/App_Plugins/LeBlender/editors/leblendereditor/Dialogs/parameterconfig.prevalues.html',
                show: true,
                dialogData: {
                    parameter: parameter,
                    availableDataTypes: $scope.availableDataTypes
                },
                callback: function (data) {
                    if (!$scope.model.value) {
                        $scope.model.value = [];
                    }
                    $scope.model.value.splice($scope.model.value.length + 1, 0, data);
                }
            });

        }

        // remove a property
        $scope.remove = function ($index) {
            $scope.model.value.splice($index, 1);
        }

        // Init again the render and frontView value
        $scope.$on('gridEditorSaving', function () {
            $scope.model.parentValue.config.frontView = $scope.model.parentValue.render;
            $scope.model.parentValue.render = "/App_Plugins/LeBlender/editors/leblendereditor/views/Base.cshtml";
        });

        // Get a list of datatype
        LeBlenderRequestHelper.getAllDataTypes().then(function (data) {
            $scope.availableDataTypes = data;
        });

    });