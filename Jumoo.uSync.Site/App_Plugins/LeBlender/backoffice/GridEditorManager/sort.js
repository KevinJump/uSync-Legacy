angular.module("umbraco").controller("leblender.editormanager.sort",
    function ($scope, assetsService, $http, LeBlenderRequestHelper, dialogService, $routeParams, navigationService, treeService) {

    $scope.save = function () {
        LeBlenderRequestHelper.setGridEditors($scope.editors).then(function (response) {
            treeService.loadNodeChildren({ node: $scope.currentNode });
            navigationService.hideMenu();
        });
    };

    $scope.close = function () {
        navigationService.hideNavigation();
    };
    
    LeBlenderRequestHelper.getGridEditors().then(function (response) {
        $scope.editors = response.data
    });

});