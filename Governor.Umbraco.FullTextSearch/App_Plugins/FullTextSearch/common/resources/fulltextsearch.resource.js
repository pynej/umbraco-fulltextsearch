/**
    * @ngdoc service
    * @name umbraco.resources.dataTypeResource
    * @description Loads in data for data types
    **/
function fullTextSearchResource($q, $http, umbDataFormatter, umbRequestHelper) {

    return {

        reindex: function (data) {
            return umbRequestHelper.resourcePromise(
               $http.post(
                   umbRequestHelper.getApiUrl(
                       "fullTextSearchApiBaseUrl",
                       "ReindexAllFullTextNodes"), data, {}),
               'Failed to retrieve data');
        },
        recreate: function (data) {
            return umbRequestHelper.resourcePromise(
               $http.post(
                   umbRequestHelper.getApiUrl(
                       "fullTextSearchApiBaseUrl",
                       "RebuildFullTextIndex"), data, {}),
               'Failed to retrieve data');
        },

    };
}

angular.module('umbraco.resources').factory('fullTextSearchResource', fullTextSearchResource);
