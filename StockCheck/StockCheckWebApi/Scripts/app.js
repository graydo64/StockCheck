'use strict';

angular.module('stockCheck', ['ngResource']).
    config(['$routeProvider', function ($routeProvider) {
        $routeProvider.
            when('/sales-items', {
                controller: SalesItemsController,
                templateUrl: 'Views/sales-items.html'
            }).
            when('/periods', {
                controller: PeriodsController,
                templateUrl: 'Views/periods.html'
            }).
            when('/sales-item/:id', {
                controller: SalesItemController,
                templateUrl: 'Views/sales-item.html'
            }).
            when('/period/:id', {
                controller: PeriodController,
                templateUrl: 'Views/period.html'
            }).
            when('/period/:action/:id', {
                controller: PeriodController,
                templateUrl: 'Views/period.html'
            }).
            when('/goods-in/:id', {
                controller: GoodsInController,
                templateUrl: 'Views/goods-in.html'
            }).
            when('/help', {
                templateUrl: 'Views/help.html'
            });
    }]);