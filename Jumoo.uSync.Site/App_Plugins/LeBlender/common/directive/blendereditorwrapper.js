angular.module("umbraco").
    directive('blenderEditorWrapper', function ($timeout) {
        return {
            scope: {
                property: "=",
                item: "=",
            },
            restrict: 'E',
            replace: true,
            template: '<ng-form name="propertyForm"><div ng-include="model.view"></div></ng-form>',

            controller: function ($scope) {
              
                var initEditorPath = function (property) {
                    if (property && property.$editor && property.$editor.propretyType) {
                        return property.$editor.propretyType.view;
                    }
                };

                $scope.model = {
                    alias: $scope.property.$editor ? angular.copy($scope.property.$editor.alias) : "",
                    label: $scope.property.$editor ? angular.copy($scope.property.$editor.name) : "",
                    config: $scope.property.$editor ? angular.copy($scope.property.$editor.propretyType.config) : {},
                    validation: {
                        mandatory:false
                    },
                    value: angular.copy($scope.property.value),
                    view: initEditorPath($scope.property)
                }

                $scope.validateMandatory = false;

                $scope.$watch("model.value", function (newValue, oldValue) {
                    $scope.property.$valid = $scope.propertyForm.$valid;

                    /* TODO HACK FOR TAG PROPERTY EDITOR */
                    if (newValue != undefined && $scope.model.view == "views/propertyeditors/tags/tags.html" && newValue.join) {
                        $scope.property.value = angular.copy(newValue.join());
                    }
                    else {
                        $scope.property.value = newValue;
                    }

                }, true);

            }

        };
    });