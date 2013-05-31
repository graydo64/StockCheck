function PeriodController($scope, $http, $routeParams, $location) {
    $scope.loading = true;
    $scope.editMode = false;

    var id = $routeParams.id;
    if ($routeParams.action === 'init-from') {
        var action = $routeParams.action;
        if (action === 'init-from') {
            $http.get('../api/period/InitFrom/' + id).success(function (data) {
                $scope.period = data;
                $scope.loading = false;
            })
            .error(function () {
                $scope.error = "An Error has occurred while loading Period."
                $scope.loading = false;
            });
        }
    }
    else {
        $http.get('../api/period/' + id).success(function (data) {
            $scope.period = data;
            $scope.loading = false;
        })
        .error(function () {
            $scope.error = "An Error has occurred while loading Period."
            $scope.loading = false;
        });
    }

    $scope.toggleEdit = function () {
        $scope.editMode = !$scope.editMode;
    };

    $scope.save = function () {
        $http.put('../api/period/', $scope.period).success(function (data) {
            alert("Saved Successfully");
        }).error(function (data) {
            $scope.error = "An error occurred while saving the Period." + data;
            $scope.loading = false;
        });
    };
}