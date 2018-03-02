angular.module('umbraco.resources').factory('Our.Umbraco.NestedContent.Resources.NestedContentResources',
    function ($q, $http, umbRequestHelper) {
        return {
            getContentTypes: function () {
                var url = Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + "/backoffice/NestedContent/NestedContentApi/GetContentTypes";
                return umbRequestHelper.resourcePromise(
                    $http.get(url),
                    'Failed to retrieve content types'
                );
            },
        };
    });