function SalesItemsController($scope, $http) {
    $scope.loading = true;
    $scope.editMode = false;

    $http.get('../api/salesitems/').success(function (data) {
        $scope.salesitems = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading Sales Items."
        $scope.loading = false;
    });
}