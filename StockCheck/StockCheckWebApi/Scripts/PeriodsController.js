'use strict';

function PeriodsController($scope, $http) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/periods/').success(function (data) {
        $scope.periods = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Periods."
        $scope.loading = false;
    });

    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $http.put('../api/periods/', $scope.periods).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving Periods." + data;
            $scope.loading = false;
        });
    };
};