angular.module('umbraco').controller('uSyncDashboardContentController',
        function ($scope, $http) {

            $scope.content = true;

            function loadMappers() {
                $http.get('backoffice/uSync/ContentEditionApi/GetMappers')
                .then(function (response) {
                    $scope.mappers = response.data;
                });
            }

            $scope.mappingTypes = [
                "Content", "DataType", 'DataTypeKey', 'Custom'
            ];

            $scope.getTypeName = function (typeId) {
                return $scope.mappingTypes[typeId];
            };

            $scope.getAssembly = function (customType) {
                if (customType && customType.indexOf(',') != -1) {
                    return customType.substr(customType.indexOf(',')+1);
                }
                return customType;
            };
            loadMappers();
});


