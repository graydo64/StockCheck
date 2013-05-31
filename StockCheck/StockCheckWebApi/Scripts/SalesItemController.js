function SalesItemController($scope, $http, $routeParams, $window) {
    $scope.loading = true;
    $scope.editMode = false;

    var id = $routeParams.id;
    $http.get('../api/salesitem/?Id=' + id).success(function (data) {
        $scope.salesitem = data;
        $scope.loading = false;
        $scope.idealGPpc = $window.Math.round(10000 * data.IdealGP) / 100;
        $scope.taxRatepc = $window.Math.round(100 * data.TaxRate);
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