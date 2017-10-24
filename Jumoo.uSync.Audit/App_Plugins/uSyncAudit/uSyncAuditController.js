(function () {

    'use strict';

    function uSyncAuditController($scope, uSyncAuditService) {
        var vm = this;

        vm.loaded = false;
        vm.getItems = getItems
        vm.showChange = showChange;
        vm.showDetail = showDetail;
        vm.getChanges = getChanges;

        vm.nextPage = nextPage;
        vm.prevPage = prevPage;
        vm.goToPage = goToPage;

        vm.page = 1;

        getChanges(vm.page);

        //////////////////

        function getChanges(page) {
            uSyncAuditService.getChanges(page)
                .then(function (result) {
                    vm.changes = result.data;
                    vm.loaded = true;
                });
        }


        function getItems(id) {
            uSyncAuditService.getItems(id)
                .then(function (result) {
                    vm.items = result.data;
                });
        }

        vm.changeTypes = ['Create', 'Update', 'Delete', 'Error'];

        function showChange(change) {
            return vm.changeTypes[change]
        }

        function showDetail(change) {
            if (change.showDetail == true) {
                change.showDetail = false;
            }
            else {
                change.showDetail = true;
            }
        }

        // pagination
        ////////////////////////////
        function nextPage() {
            vm.page++;
            vm.getChanges(vm.page);
        }

        function prevPage() {
            vm.page--;
            vm.getChanges(vm.page);
        }

        function goToPage (pageNo) {
            vm.page = pageNo;
            vm.getChanges(vm.page);
        }

    }

    angular.module('umbraco')
        .controller('uSyncAuditController', uSyncAuditController);

})();