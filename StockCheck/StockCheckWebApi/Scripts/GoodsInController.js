function GoodsInController($scope, $http, $routeParams) {
    $scope.loading = true;
    $scope.editMode = false;
    $scope.filter = '';

    $scope.quantity = 0;
    $scope.receivedDate = (new Date()).toLocaleDateString();
    $scope.invoicedAmountEx = 0;
    $scope.invoicedAmountInc = 0;

    var periodId = $routeParams.id;

    $http.get('../api/salesitems/').success(function (data) {
        $scope.salesItems = data;
        $scope.loading = false;
    })
    .error(function () {
        $scope.error = "An Error has occurred while loading the Sales Item list."
        $scope.loading = false;
    });

    var items = function (data) {
        $scope.filteredItems = $scope.salesItems.filter(function (salesItem) {
            if (salesItem.Name.toLowerCase().indexOf($scope.filter.toLowerCase()) > -1) {
                return salesItem;
            }
        });
    }
    $scope.filterSalesItems = items;

    $scope.toggleRow = function (salesitem) {
        $scope.active = salesitem;
    };

    $scope.save = function () {
        alert("Save me!");
    }
}