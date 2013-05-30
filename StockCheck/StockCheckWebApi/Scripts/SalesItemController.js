function SalesItemController($scope, $http, $location) {
    $scope.loading = true;
    $scope.editMode = false;

    //$locationProvider.html5Mode(true);
    var qs = $location
    $http.get('../api/salesitem/').success(function (data) {
        $scope.salesitem = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Sales Item."
        $scope.loading = false;
    });

    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $http.put('../api/salesitem/', $scope.salesitem).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Sales Item." + data;
            $scope.loading = false;
        });
    };
}