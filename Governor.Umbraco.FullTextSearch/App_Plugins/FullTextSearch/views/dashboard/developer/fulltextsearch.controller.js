angular.module("umbraco").controller("FullTextSearch.DeveloperController",
	function ($scope, $element, $routeParams, $filter, notificationsService, fullTextSearchResource, angularHelper) {
	 
	    $scope.model = {};

	    $scope.reindex = function () {

	        fullTextSearchResource.reindex($scope.model).then(function (data) {
	            $scope.isreindexing = true;
	        });
	    };

	    $scope.recreate = function () {

	        fullTextSearchResource.recreate($scope.model).then(function (data) {
	            $scope.isrebuilding = true;
	        });
	    };

	});