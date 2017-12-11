(function (root) {

    var packageName = 'Terratype';

    if (!root.terratype) {
        root.terratype = {
            loading: false,
            providers: {}
        };
    }

    angular.module('umbraco.directives').directive('terratypeJson', function () {
        return {
            restrict: 'A', // only activate on element attribute
            require: 'ngModel', // get a hold of NgModelController
            link: function (scope, element, attrs, ngModelCtrl) {

                var lastValid;

                // push() if faster than unshift(), and avail. in IE8 and earlier (unshift isn't)
                ngModelCtrl.$parsers.push(fromUser);
                ngModelCtrl.$formatters.push(toUser);

                // clear any invalid changes on blur
                element.bind('blur', function () {
                    element.val(toUser(scope.$eval(attrs.ngModel)));
                });

                // $watch(attrs.ngModel) wouldn't work if this directive created a new scope;
                // see http://stackoverflow.com/questions/14693052/watch-ngmodel-from-inside-directive-using-isolate-scope how to do it then
                scope.$watch(attrs.ngModel, function (newValue, oldValue) {
                    lastValid = lastValid || newValue;

                    if (newValue != oldValue) {
                        ngModelCtrl.$setViewValue(toUser(newValue));

                        // TODO avoid this causing the focus of the input to be lost
                        ngModelCtrl.$render();
                    }
                }, true); // MUST use objectEquality (true) here, for some reason

                function fromUser(text) {
                    // Beware: trim() is not available in old browsers
                    if (!text || text.trim() === '') {
                        return {};
                    } else {
                        try {
                            lastValid = angular.fromJson(text);
                            ngModelCtrl.$setValidity('invalidJson', true);
                        } catch (e) {
                            ngModelCtrl.$setValidity('invalidJson', false);
                        }
                        return lastValid;
                    }
                }

                function toUser(object) {
                    // better than JSON.stringify(), because it formats + filters $$hashKey etc.
                    return angular.toJson(object, true);
                }
            }
        };
    });


    //  Display language values that contain {{}} variables.
    angular.module('umbraco.directives').directive('terratypeTranslate', ['$compile', 'localizationService', function ($compile, localizationService) {
        return function (scope, element, attr) {
            attr.$observe('terratypeTranslate', function (key) {
                localizationService.localize(key).then(function (value) {
                    var c = $compile('<span>' + value + '</span>')(scope);
                    element.append(c);
                });
            })
        }
    }]);

    //  Don't allow Enter Key to bubble up to submit form
    angular.module('umbraco.directives').directive('terratypeIgnoreEnterKey', ['$rootScope', function ($rootScope) {
        return function (scope, element, attrs) {
            element.on("keydown keypress", function (event) {
                if (event.which === 13) {
                    event.preventDefault();
                }
            });
        };
    }]);

    angular.module('umbraco').controller('terratype', ['$scope', '$timeout', '$http', 'localizationService',
        function ($scope, $timeout, $http, localizationService) {
        $scope.config = null;
        $scope.store = null;
        $scope.vm = null;
        $scope.identifier = $scope.$id + (new Date().getTime());

        $scope.viewmodel = {
            showMap: false,
            config: {
                label: {
                    enable: false,
                    editPosition: 0
                }
            },
            configs: [],
            position: {},
            providers: [],
            provider: {
                id: null,
                referenceUrl: null,
                name: null
            },
            label: {},
            labels: [],
            loading: true,
            configgering: false,
            error: null,
            isPreview: false,
            icon: {
                anchor: {
                    horizontal: {},
                    vertical: {}
                }
            }
        }

        $scope.terratype = {
            urlProvider: function (id, file, cache) {
                var r = Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/' + id + '/' + file;
                if (cache == true) {
                    r += '?cache=1.0.13';
                }
                return r;
            },
            images: {
                loading: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/assets/img/loader.gif',
                failed: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/images/false.png',
                success: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/images/true.png',
            },
            controller: function (a) {
                return Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/backoffice/' + packageName + '/ajax/' + a;
            },
            poll: 250,
            mapId: function (array, id) {
                for (var i = 0; i != array.length; i++) {
                    if (array[i].id == id) {
                        return i;
                    }
                }
                return -1;
            },
            translate: function (m) {
                localizationService.localize(m.name).then(function (value) {
                    m.name = value;
                });
                localizationService.localize(m.description).then(function (value) {
                    m.description = value;
                });
                if (m.referenceUrl) {
                    localizationService.localize(m.referenceUrl).then(function (value) {
                        m.referenceUrl = value;
                    });
                }
            },
            initLabels: function (done) {
                $http.get($scope.terratype.controller('labels')).then(function success(response) {
                    angular.forEach(response.data, function (m) {
                        $scope.terratype.translate(m);
                        $scope.terratype.setLabelView(m);
                    });
                    $timeout(function () {
                        $scope.vm().labels = response.data;
                        done();
                    });
                });
            },
            setLabelView: function (m) {
                if (!m.view) {
                    m.view = $scope.terratype.urlProvider(packageName, 'views/label.' + m.id + '.html', true);
                }
                if (!m.controller) {
                    m.controller = 'terratype.label.' + m.id;
                }
            },
            initConfig: function () {
                $scope.vm = function () {
                    return $scope.viewmodel;
                }
                $scope.vm().configgering = true;
                $scope.terratype.initLabels(function () {
                    if (typeof ($scope.model.value) === 'string') {
                        $scope.model.value = ($scope.model.value != '') ? JSON.parse($scope.model.value) : {};
                    }
                    $scope.config = function () {
                        return $scope.model.value.config;
                    }
                    $scope.store = function () {
                        return $scope.model.value;
                    }

                    $scope.terratype.setIcon();
                    $http.get($scope.terratype.controller('providers')).then(function success(response) {
                        angular.forEach(response.data, function (p) {
                            $scope.terratype.translate(p);
                            angular.forEach(p.coordinateSystems, function (c) {
                                $scope.terratype.translate(c);
                            });
                        });
                        $timeout(function () {
                            $scope.vm().providers = response.data;

                            if ($scope.config && $scope.config().provider && $scope.config().provider.id != null) {
                                $scope.terratype.setProvider($scope.config().provider.id);
                            }
                            $timeout(function () {
                                $scope.vm().loading = false;
                            });
                        })
                    }, function error(response) {
                        $scope.vm().loading = false;
                    });
                });
            },
            loadProvider: function (id, done) {
                var wait = setInterval(function () {
                    if (!root.terratype.loading) {
                        clearInterval(wait);
                        if (!angular.isUndefined(root.terratype.providers[id])) {
                            done();
                        } else {
                            root.terratype.loading = true;
                            var script = $scope.terratype.urlProvider(id, 'scripts/' + id + '.js', true);
                            LazyLoad.js(script, function () {
                                $timeout(function () {
                                    if (angular.isUndefined(root.terratype.providers[id])) {
                                        throw script + ' does not define global variable root.terratype.providers[\'' + id + '\']';
                                    }
                                    root.terratype.providers[id].script = script;
                                    done(id);
                                    root.terratype.loading = false;
                                });
                            });
                        }
                    }
                }, $scope.terratype.poll);
            },
            setIcon: function () {
                if ($scope.config().icon && $scope.config().icon.id) {
                    $scope.terratype.iconPredefineChangeInternal($scope.config().icon.id);
                }
                $scope.terratype.iconAnchor();
                if ($scope.config().icon && !$scope.config().icon.id) {
                    $scope.terratype.iconCustom();
                }
            },
            loadResources: function (id, provider, complete) {
                if (provider.css && provider.css.length != 0) {
                    for (var i = 0; i != provider.css.length; i++) {
                        LazyLoad.css($scope.terratype.urlProvider(id, provider.css[i], true));
                    }
                }
                if (provider.js && provider.js.length != 0) {
                    var files = [];
                    for (var i = 0; i != provider.js.length; i++) {
                        files.push($scope.terratype.urlProvider(id, provider.js[i], true));
                    }
                    LazyLoad.js(files, complete);
                }
                else {
                    complete();
                }
            },
            setProvider: function (id) {
                var index = $scope.terratype.mapId($scope.vm().providers, id);
                if (index == -1) {
                    //  Asked for a provider we don't have
                    return;
                }
                if ($scope.config().provider) {
                    if (id != $scope.config().provider.id) {
                        if ($scope.vm().provider && $scope.vm().provider.events && $scope.vm().provider.events.destroy) {
                            $scope.vm().provider.events.destroy();
                        }
                        $scope.vm().configs[$scope.config().provider.id] = angular.copy($scope.config().provider);
                        $scope.config().provider = $scope.vm().configs[id] || {};
                    } else {
                        $scope.config().provider.id = id;
                    }
                } else {
                    $scope.config().provider = {
                        id: id,
                        grid: {
                            enable: false
                        }
                    }
                }

                $scope.vm().providerLoading = true;
                $timeout(function () {
                    $scope.terratype.loadProvider(id, function () {
                        $scope.vm().providers[index] = angular.extend($scope.vm().providers[index], root.terratype.providers[id]);
                        $scope.terratype.loadResources(id, $scope.vm().providers[index], function () {
                            $scope.vm().providers[index].boot($scope.identifier, $scope.terratype.urlProvider,
                                $scope.store, $scope.config, $scope.vm, function () {
                                    $scope.$apply();
                                }, function (key, done) {
                                    localizationService.localize(key).then(function (value) {
                                        done(value);
                                    })
                                }, function (e) {
                                    $scope.vm().providers[index].events = e;
                                    $scope.vm().provider = angular.copy($scope.vm().providers[index]);
                                    $scope.vm().provider.events.setProvider();
                                    if ($scope.store().position && $scope.store().position.id != null) {
                                        $scope.terratype.setCoordinateSystem($scope.store().position.id);
                                    }
                                    $timeout(function () {
                                        if ($scope.vm().configgering) {
                                            for (var httpCall in $scope.vm().provider.events.httpCalls) {
                                                with ({
                                                    hc: $scope.vm().provider.events.httpCalls[httpCall]
                                                }) {
                                                    $http.get($scope.terratype.controller('DataTypeFields?when=' + hc.when + '&field=' + hc.field)).then(function success(response) {
                                                        hc.values = response.data;
                                                    });
                                                }
                                            }
                                        }
                                        $scope.vm().providerLoading = false;
                                    });
                                }
                            );
                        });
                    });
                });
            },
            setCoordinateSystem: function (id) {
                var index = $scope.terratype.mapId($scope.vm().provider.coordinateSystems, id);
                $scope.vm().position = (index != -1) ? angular.copy($scope.vm().provider.coordinateSystems[index]) : { id: null, referenceUrl: null, name: null, datum: null, precision: 6 };
                if ($scope.vm().configgering) {
                    $scope.store().position.precision = $scope.vm().position.precision;
                }
                $scope.vm().provider.events.setCoordinateSystem();
            },
            setLabel: function (id) {
                if (id) {
                    var index = $scope.terratype.mapId($scope.vm().labels, id);
                    if (index == -1) {
                        index = 0;
                    }
                    $scope.vm().label = $scope.vm().labels[index];
                }
                //angular.extend($scope.vm().label, $scope.store().label);
                //$scope.vm().provider.events.labelChange($scope.vm().label);

                angular.extend($scope.store().label, $scope.vm().label);
                $scope.vm().provider.events.labelChange();
            },
            iconAnchor: function () {
                if (isNaN($scope.config().icon.anchor.horizontal)) {
                    $scope.vm().icon.anchor.horizontal.isManual = false;
                    $scope.vm().icon.anchor.horizontal.automatic = $scope.config().icon.anchor.horizontal;
                } else {
                    $scope.vm().icon.anchor.horizontal.isManual = true;
                    $scope.vm().icon.anchor.horizontal.manual = $scope.config().icon.anchor.horizontal;
                }
                if (isNaN($scope.config().icon.anchor.vertical)) {
                    $scope.vm().icon.anchor.vertical.isManual = false;
                    $scope.vm().icon.anchor.vertical.automatic = $scope.config().icon.anchor.vertical;
                } else {
                    $scope.vm().icon.anchor.vertical.isManual = true;
                    $scope.vm().icon.anchor.vertical.manual = $scope.config().icon.anchor.vertical;
                }
            },
            iconPredefineChangeInternal: function (id) {
                var index = 0;
                if (id) {
                    var index = $scope.terratype.mapId($scope.terratype.icon.predefine, id);
                    if (id == -1) {
                        index = 0;
                    }
                }
                $scope.config().icon.id = id;
                $scope.config().icon.url = $scope.terratype.icon.predefine[index].url;
                $scope.config().icon.size = $scope.terratype.icon.predefine[index].size;
                $scope.config().icon.anchor = $scope.terratype.icon.predefine[index].anchor;
                $scope.terratype.iconAnchor();
            },
            iconPredefineChange: function (id) {
                $scope.terratype.iconPredefineChangeInternal(id);
                $scope.vm().provider.events.setIcon();
            },
            absoluteUrl: function (url) {
                if (!url) {
                    return '';
                }
                if (url.indexOf('//') != -1) {
                    //  Is an absolute address
                    return url;
                }
                //  Must be a relative address
                if (url.substring(0, 1) != '/') {
                    url = '/' + url;
                }

                return root.location.protocol + '//' + root.location.hostname + (root.location.port ? ':' + root.location.port : '') + url;
            },
            iconCustom: function () {
                $scope.config().icon.id = $scope.vm().icon.predefine[0].id;
                if (!$scope.vm().icon.anchor.horizontal.isManual) {
                    switch ($scope.vm().icon.anchor.horizontal.automatic) {
                        case 'left':
                            $scope.vm().icon.anchor.horizontal.manual = 0;
                            break;
                        case 'center':
                            $scope.vm().icon.anchor.horizontal.manual = (($scope.config().icon.size.width - 1) / 2) | 0;
                            break;
                        case 'right':
                            $scope.vm().icon.anchor.horizontal.manual = $scope.config().icon.size.width - 1;
                            break;
                    }
                }
                if (!$scope.vm().icon.anchor.vertical.isManual) {
                    switch ($scope.vm().icon.anchor.vertical.automatic) {
                        case 'top':
                            $scope.vm().icon.anchor.vertical.manual = 0;
                            break;
                        case 'center':
                            $scope.vm().icon.anchor.vertical.manual = (($scope.config().icon.size.height - 1) / 2) | 0;
                            break;
                        case 'bottom':
                            $scope.vm().icon.anchor.vertical.manual = $scope.config().icon.size.height - 1;
                            break;
                    }
                }
            },
            iconImageChange: function () {
                $scope.vm().icon.urlFailed = '';
                $http({
                    url: $scope.terratype.controller('image'),
                    method: 'GET',
                    params: {
                        url: $scope.config().icon.url
                    }
                }).then(function success(response) {
                    if (response.data.status == 200) {
                        $scope.config().icon.size = {
                            width: response.data.width,
                            height: response.data.height
                        };
                        $scope.config().icon.format = response.data.format;
                    } else {
                        $scope.vm().icon.urlFailed = response.data.error;
                    }
                }, function fail(response) {
                    $scope.vm().icon.urlFailed = response.data;
                });
                $scope.terratype.iconCustom();
            },
            icon: {
                anchor: {
                    horizontal: {},
                    vertical: {}
                },
                predefine: [
                {
                    id: '',
                    name: '[Custom]',
                    url: '',
                    shadowUrl: '',
                    size: {
                        width: 32,
                        height: 32
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'bottom'
                    }
                },
                {
                    id: 'redmarker',
                    name: 'Red Marker',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/spotlight-poi.png',
                    shadowUrl: '',
                    size: {
                        width: 22,
                        height: 40
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'bottom'
                    }
                },
                {
                    id: 'greenmarker',
                    name: 'Green Marker',
                    url: 'https://mt.google.com/vt/icon?psize=30&font=fonts/arialuni_t.ttf&color=ff304C13&name=icons/spotlight/spotlight-waypoint-a.png&ax=43&ay=48&text=%E2%80%A2',
                    shadowUrl: '',
                    size: {
                        width: 22,
                        height: 43
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'bottom'
                    }
                },
                {
                    id: 'bluemarker',
                    name: 'Blue Marker',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/spotlight-waypoint-blue.png',
                    shadowUrl: '',
                    size: {
                        width: 22,
                        height: 40
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'bottom'
                    }
                },
                {
                    id: 'purplemarker',
                    name: 'Purple Marker',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/spotlight-ad.png',
                    shadowUrl: '',
                    size: {
                        width: 22,
                        height: 40
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'bottom'
                    }
                },
                {
                    id: 'goldstar',
                    name: 'Gold Star',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/star_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 42,
                        height: 42
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'greyhome',
                    name: 'Grey Home',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/home_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redshoppingcart',
                    name: 'Red Shopping Cart',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/supermarket_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'blueshoppingcart',
                    name: 'Blue Shopping Cart',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/supermarket_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redhotspring',
                    name: 'Red Hot Spring',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/jp/hot_spring_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'reddharma',
                    name: 'Red Dharma',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/worship_dharma_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'browndharma',
                    name: 'Brown Dharma',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/worship_dharma_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redjain',
                    name: 'Red Jain',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/worship_jain_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'brownjain',
                    name: 'Brown Jain',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/worship_jain_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redshopping',
                    name: 'Red Shopping',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/shopping_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'blueshopping',
                    name: 'Blue Shopping',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/shopping_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redharbour',
                    name: 'Red Harbour',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/harbour_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'blueharbour',
                    name: 'Blue Harbour',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/harbour_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'orangeumbraco',
                    name: 'Orange Umbraco',
                    url: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/assets/img/application/logo.png',
                    shadowUrl: '',
                    size: {
                        width: 32,
                        height: 32
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'blackumbraco',
                    name: 'Black Umbraco',
                    url: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/assets/img/application/logo_black.png',
                    shadowUrl: '',
                    size: {
                        width: 32,
                        height: 32
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'whiteumbraco',
                    name: 'White Umbraco',
                    url: Umbraco.Sys.ServerVariables.umbracoSettings.umbracoPath + '/assets/img/application/logo_white.png',
                    shadowUrl: '',
                    size: {
                        width: 32,
                        height: 32
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'redcircle',
                    name: 'Red Circle',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/generic_search_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'orangecircle',
                    name: 'Orange Circle',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/ad_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'browncircle',
                    name: 'Brown Circle',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/generic_establishment_v_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'greencircle',
                    name: 'Green Circle',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/generic_recreation_v_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                {
                    id: 'bluecircle',
                    name: 'Blue Circle',
                    url: 'https://mt.google.com/vt/icon/name=icons/spotlight/generic_retail_v_L_8x.png&scale=2',
                    shadowUrl: '',
                    size: {
                        width: 48,
                        height: 48
                    },
                    anchor: {
                        horizontal: 'center',
                        vertical: 'center'
                    }
                },
                ]
            },
            loadEditor: function (id, initial, completed) {
                //console.log('loadEditor(): ' + id)
                if (!$scope.store().zoom) {
                    $scope.store().zoom = initial.zoom;
                }
                if (!$scope.store().position || !$scope.store().position.id || !$scope.store().position.datum) {
                    $scope.store().position = {
                        id: initial.position.id,
                        datum: initial.position.datum
                    }
                    $scope.terratype.loadEditor2(id, initial, completed);
                } else if ($scope.store().position.id != initial.position.id) {
                    //  Convert coords from old system to new
                    $http({
                        url: $scope.terratype.controller('convertcoordinatesystem'),
                        method: 'GET',
                        params: {
                            sourceId: $scope.store().position.id,
                            sourceDatum: $scope.store().position.datum,
                            destinationId: initial.position.id
                        }
                    }).then(function success(response) {
                        $scope.store().position.datum = response.data;
                        $scope.store().position.id = initial.position.id;
                        $scope.terratype.loadEditor2(id, initial, completed);
                    });
                } else {
                    $scope.terratype.loadEditor2(id, initial, completed);
                }
            },
            loadEditor2: function (id, initial, completed) {
                $scope.terratype.loadProvider($scope.config().provider.id, function () {
                    $scope.vm().isPreview = !angular.isUndefined($scope.model.sortOrder);
                    $scope.vm().provider = angular.copy(root.terratype.providers[$scope.config().provider.id]);
                    var position = angular.copy($scope.store().position);
                    position.precision = initial.position.precision;
                    $scope.vm().provider.coordinateSystems = [];
                    $scope.vm().provider.coordinateSystems.push(position);
                    $scope.vm().position = angular.copy(position);
                    $scope.vm().label = $scope.config().label;
                    $scope.terratype.setLabelView($scope.vm().label);
                    $scope.vm().loading = false;
                    setTimeout(function () {
                        $scope.terratype.loadResources($scope.config().provider.id, $scope.vm().provider, function () {
                            //  Simple way to wait for any destroy to have finished
                            $scope.vm().provider.boot(id, $scope.terratype.urlProvider, $scope.store, $scope.config, $scope.vm, function () {
                                $scope.$apply();
                            }, function (key, done) {
                                localizationService.localize(key).then(function (value) {
                                    done(value);
                                })
                            }, function (e) {
                                $scope.vm().provider.events = e;
                                $scope.terratype.labelOverlay.view = $scope.terratype.urlProvider(packageName, 'views/label.' + $scope.config().label.id + '.html', true);
                                if ($scope.config().label.enable == true && $scope.config().label.editPosition == 0) {
                                    with ({
                                        display: $scope.terratype.labelOverlay.display
                                    }) {
                                        $scope.vm().provider.events.addEvent('icon-click', function () {
                                            display()
                                        }, $scope);
                                    }
                                }
                                $scope.vm().showMap = true;
                                if (completed) {
                                    completed();
                                }
                            });
                        });
                    }, 150);
                });
            },
            initEditor: function (completed) {
                $scope.vm = function () {
                    return $scope.viewmodel;
                }
                $scope.vm().error = false;
                try {
                    if (typeof ($scope.model.value) === 'string') {
                        $scope.model.value = ($scope.model.value != '') ? JSON.parse($scope.model.value) : null;
                    }
                    if (!$scope.model.value) {
                        $scope.model.value = {};
                    }
                }
                catch (oh) {
                    //  Can't even read our own values
                    $scope.vm().error = true;
                    $scope.model.value = {};
                }
                try {
                    $scope.terratype.labelOverlay.init();
                    $scope.config = function () {
                        return $scope.model.config.definition.config;
                    }
                    $scope.store = function () {
                        return $scope.model.value;
                    }
                    $scope.terratype.loadEditor($scope.identifier, $scope.model.config.definition);
                }
                catch (oh) {
                    //  Error so might as well show debug
                    console.log(oh);
                    $scope.vm().loading = false;
                    $scope.vm().error = true;
                    $scope.config().debug = 1;
                }
            },
            gridOverlay: {
                identifier: null,
                title: 'terratypeGridOverlay_title',
                subtitle: 'terratypeGridOverlay_subtitle',
                show: false,
                display: function () {
                    //console.log('display(): ' + $scope.identifier);
                    if ($scope.terratype.gridOverlay.show == true) {
                        return;
                    }
                    $scope.vm = function () {
                        return $scope.terratype.gridOverlay.vm;
                    }
                    $scope.config = function () {
                        return $scope.terratype.gridOverlay.vm.config;
                    }
                    if ($scope.control.value) {
                        $scope.terratype.gridOverlay.store = angular.copy($scope.control.value);
                    }
                    $scope.store = function () {
                        return $scope.terratype.gridOverlay.store;
                    }
                    $scope.terratype.setLabelView($scope.config().label);
                    if ($scope.terratype.gridOverlay.dataTypes.length == 0) {
                        $http.get($scope.terratype.controller('datatypes')).then(function success(response) {
                            $scope.terratype.gridOverlay.dataTypes = response.data;
                            $scope.terratype.gridOverlay.displayLoad();
                        });
                    } else {
                        $scope.terratype.gridOverlay.displayLoad();
                    }
                },
                displayLoad: function () {
                    //console.log('displayLoad(): ' + $scope.identifier);
                    $scope.terratype.gridOverlay.show = true;
                    if ($scope.store().datatypeId) {
                        $scope.terratype.gridOverlay.setDatatype($scope.store().datatypeId);
                    }
                },
                submit: function (model) {
                    $scope.control.value = angular.copy($scope.terratype.gridOverlay.store);
                    $scope.terratype.gridOverlay.close();
                },
                close: function () {
                    $scope.viewmodel.loading = true;
                    $scope.terratype.gridOverlay.show = false;
                    if ($scope.vm().provider.events) {
                        $scope.vm().provider.events.destroy();
                    }
                    $timeout(function () {
                        $scope.terratype.loadGrid();
                    }, $scope.terratype.poll);
                },
                view: 'uninitalized',
                dataTypes: [],
                vm: {
                    showMap: false,
                    config: {
                        label: {
                            enable: false,
                            editPosition: 0
                        }
                    },
                    position: [],
                    providers: [],
                    provider: {
                        id: null,
                        referenceUrl: null,
                        name: null
                    },
                    label: {},
                    labels: [],
                    loading: false,
                    configgering: false,
                    error: null,
                    isPreview: false,
                    icon: {
                        anchor: {
                            horizontal: {},
                            vertical: {}
                        }
                    }
                },
                store: {},
                rte: {},
                setDatatype: function (dd) {
                    //console.log('setDatatype():' + $scope.terratype.gridOverlay.identifier)

                    $scope.vm().showMap = false;
                    if ($scope.vm().provider.events) {
                        $scope.vm().provider.events.destroy();
                    }
                    $timeout(function () {
                        var d = $scope.terratype.gridOverlay.dataTypes;
                        for (var i = 0; i != d.length; i++) {
                            if (d[i].id == dd) {
                                //$scope.identifier = $scope.$id + id + (new Date().getTime());
                                var c = angular.copy(d[i].config);
                                $scope.terratype.gridOverlay.store.datatypeId = dd;
                                $scope.terratype.gridOverlay.vm.config = c.config;
                                $scope.terratype.loadEditor($scope.terratype.gridOverlay.identifier, c);
                                break;
                            }
                        }
                    }, $scope.terratype.poll);
                },
                parentScope: function () {
                    return $scope.parentScope();
                }
            },
            labelOverlay: {
                init: function () {
                    localizationService.localize($scope.terratype.labelOverlay.title).then(function (value) {
                        $scope.terratype.labelOverlay.title = value;
                    });
                    localizationService.localize($scope.terratype.labelOverlay.subtitle).then(function (value) {
                        $scope.terratype.labelOverlay.subtitle = value;
                    });
                },
                title: 'terratypeLabelOverlay_title',
                subtitle: 'terratypeLabelOverlay_subtitle',
                show: false,
                display: function () {
                    if ($scope.terratype.labelOverlay.show == true) {
                        return false;
                    }
                    $scope.terratype.labelOverlay.show = true;
                    return false;
                },
                submit: function (model) {          //  model = $scope.vm().labelOverlay
                    $scope.terratype.setLabel();
                    model.show = false;
                },
                view: 'uninitalized',
                vm: $scope.vm,
                config: function () {
                    return $scope.config().label;
                },
                store: function () {
                    return $scope.store().label;
                }
            },
            loadGrid: function () {
                try {
                    $scope.vm = function () {
                        return $scope.viewmodel;
                    }
                    $scope.config = function () {
                        return $scope.viewmodel.config;
                    }
                    $scope.store = function () {
                        return $scope.control.value;
                    }
                    $http.get($scope.terratype.controller('datatypes?id=' + $scope.control.value.datatypeId)).then(function success(response) {
                        if (response.data.length == 1) {
                            var c = angular.copy(response.data[0].config);
                            $scope.viewmodel.config = c.config;
                            $scope.terratype.loadEditor($scope.identifier, c, function () {
                                with ({
                                    display: $scope.terratype.gridOverlay.display
                                }) {
                                    $scope.vm().provider.events.addEvent('map-click', function () {
                                        display()
                                    }, $scope);
                                    $scope.vm().provider.events.addEvent('icon-click', function () {
                                        display()
                                    }, $scope);
                                }
                            });
                        }
                    });
                }
                catch (oh) {
                    //  Error so might as well show debug
                    console.log(oh);
                    $scope.vm().loading = false;
                    $scope.vm().error = true;
                    $scope.config().debug = 1;
                }
            },
            initGrid: function () {
                //console.log('initGrid(): ' + $scope.identifier);

                $scope.terratype.gridOverlay.view = $scope.terratype.urlProvider(packageName, 'views/grid.overlay.html', true);
                localizationService.localize($scope.terratype.gridOverlay.title).then(function (value) {
                    $scope.terratype.gridOverlay.title = value;
                });
                localizationService.localize($scope.terratype.gridOverlay.subtitle).then(function (value) {
                    $scope.terratype.gridOverlay.subtitle = value;
                });
                $scope.terratype.labelOverlay.init();
                $timeout(function () {
                    $scope.vm = function () {
                        return $scope.viewmodel;
                    }
                    $scope.vm().loading = false;
                    if ($scope.control.$initializing) {
                        //  No map has been selected yet
                    } else if ($scope.control.value) {
                        //  Map has been previous set
                        try {
                            if (typeof ($scope.control.value) === 'string') {
                                $scope.control.value = ($scope.control.value != '') ? JSON.parse($scope.control.value) : null;
                            }
                            $scope.terratype.loadGrid();
                        }
                        catch (oh) {
                            //  Can't even read our own values
                            console.log(oh);
                            $scope.vm().error = true;
                            $scope.control.value = {};
                        }
                    }
                }, $scope.terratype.poll);
            }
        }

        $scope.parentScope = function () {
            return $scope;
        }
    }]);

    angular.module('umbraco').controller('terratype.label.standard', ['$scope', '$timeout', 'localizationService', '$controller', 'tinyMceService', 'macroService',
    function ($scope, $timeout, localizationService, $controller, tinyMceService, macroService) {

        $scope.identifier = $scope.$id + (new Date().getTime());
        $scope.colors = [
            { id: '#ffffff' },      // White
            { id: '#faebd7' },      // Antique White
            { id: '#f5f5dc' },      // Beige
            { id: '#ffe4c4' },      // Bisque
            { id: '#c0c0c0' },      // Silver
            { id: '#808080' },      // Grey
            { id: '#000000' },      // Black
            { id: '#ff0000' },      // Red
            { id: '#800000' },      // Maroon
            { id: '#ffff00' },      // Yellow
            { id: '#808000' },      // Olive
            { id: '#00ff00' },      // Lime
            { id: '#008000' },      // Green
            { id: '#00ffff' },      // Aqua
            { id: '#008080' },      // Teal
            { id: '#0000ff' },      // Blue
            { id: '#000080' },      // Navy
            { id: '#ff00ff' },      // Fuchsia
            { id: '#800080' }       // Purple
        ]
        $scope.init = function () {
            var parent = $scope.$parent;
            while (parent) {
                if (parent.terratype) {
                    break;
                }
                parent = parent.$parent;
            };

            $scope.vm = parent.vm;
            $scope.config = parent.config;
            $scope.store = parent.store;
            $scope.terratype = parent.terratype;

            $scope.setForeground = function (id) {
                $scope.store().label.foreground = id;
            }

            $scope.setBackground = function (id) {
                $scope.store().label.background = id;
            }

            $scope.rte = {
                id: $scope.identifier + 'rte',
                getEditor: function () {
                    for (var i = 0; i != tinymce.editors.length; i++) {
                        if (tinymce.editors[i].id == $scope.rte.id) {
                            return tinymce.editors[i];
                        }
                    }
                    return null;
                },
                createLabel: function (editor) {
                    var text = editor.getElement().getAttribute("placeholder") || editor.settings.placeholder;
                    var attrs = editor.settings.placeholder_attrs || { style: { 'position': 'absolute', 'width': '100%', 'overflow': 'hidden', 'display': 'none' } };
                    var el = root.tinymce.DOM.create('div', attrs);
                    if (el.addEventListener) {
                        el.addEventListener('click', $scope.rte.focusEvent, false);
                    } else if (el.attachEvent) {
                        el.attachEvent('onclick', $scope.rte.focusEvent);
                    }
                    var inner = root.tinymce.DOM.add(el, 'span', { style: { 'padding': '10px', 'color': '#aaaaaa', 'font-size': '17px !important;', 'white-space': 'pre-wrap', 'display': 'inline-block' } }, text)
                    var parent = editor.getContentAreaContainer();
                    parent.insertBefore(el, parent.firstChild);
                    return el;
                },
                label: null,
                focusEvent: function () {
                    $scope.rte.focus();
                    root.tinymce.execCommand('mceFocus', false);
                },
                focus: function () {
                    var editor = $scope.rte.getEditor();
                    if (!editor.settings.readonly === true) {
                        $scope.rte.label.style.display = 'none'
                    }
                },
                blur: function () {
                    var editor = $scope.rte.getEditor();
                    if (editor.getContent() == '') {
                        $scope.rte.label.style.display = '';
                    } else {
                        $scope.rte.label.style.display = 'none'
                    }
                    $scope.terratype.setLabel();
                },
                config: {
                    selector: "textarea",
                    toolbar: ['code', 'styleselect', 'bold', 'italic', 'forecolor', 'backcolor', 'alignleft', 'aligncenter', 'alignright', 'bullist', 'numlist', 'link', 'umbmediapicker', 'umbembeddialog'],
                    stylesheets: []
                },
                linkPickerOverlay: {},
                openLinkPicker: function (editor, currentTarget, anchorElement) {
                    $scope.rte.linkPickerOverlay = {
                        view: "linkpicker",
                        currentTarget: currentTarget,
                        show: true,
                        submit: function (model) {
                            tinyMceService.insertLinkInEditor(editor, model.target, anchorElement);
                            $scope.rte.linkPickerOverlay.show = false;
                            $scope.rte.linkPickerOverlay = null;
                        }
                    };
                },
                mediaPickerOverlay: {},
                openMediaPicker: function (editor, currentTarget, userData) {
                    $scope.rte.mediaPickerOverlay = {
                        currentTarget: currentTarget,
                        onlyImages: true,
                        showDetails: true,
                        startNodeId: userData.startMediaId,
                        view: "mediapicker",
                        show: true,
                        submit: function (model) {
                            tinyMceService.insertMediaInEditor(editor, model.selectedImages[0]);
                            $scope.rte.mediaPickerOverlay.show = false;
                            $scope.rte.mediaPickerOverlay = null;
                        },
                        onImageLoaded: function () {
                            // Not sure what we do here
                        }
                    };
                },
                embedOverlay: {},
                openEmbed: function (editor) {
                    $scope.rte.embedOverlay = {
                        view: "embed",
                        show: true,
                        submit: function (model) {
                            tinyMceService.insertEmbeddedMediaInEditor(editor, model.embed.preview);
                            $scope.rte.embedOverlay.show = false;
                            $scope.rte.embedOverlay = null;
                        }
                    };
                },
                macroPickerOverlay: {},
                openMacroPicker: function (editor, dialogData) {
                    $scope.rte.macroPickerOverlay = {
                        view: "macropicker",
                        dialogData: dialogData,
                        show: true,
                        submit: function (model) {
                            var macroObject = macroService.collectValueData(model.selectedMacro, model.macroParams, dialogData.renderingEngine);
                            tinyMceService.insertMacroInEditor(editor, macroObject, $scope);
                            $scope.rte.macroPickerOverlay.show = false;
                            $scope.rte.macroPickerOverlay = null;
                        }
                    };
                }
            }

            var timer = setInterval(function () {
                if (!root.tinymce) {
                    return;
                }
                var editor = $scope.rte.getEditor();
                if (editor == null) {
                    return;
                }
                clearInterval(timer);
                $scope.rte.label = new $scope.rte.createLabel(editor);
                $scope.rte.blur();
            }, 100);
        }
    }]);

    angular.module('umbraco').controller('terratype.grid.overlay', ['$scope', 
        function ($scope) {

        $scope.gridOverlay = $scope.$parent.model;
        if (!$scope.gridOverlay.identifier) {
            $scope.gridOverlay.identifier = $scope.$id + (new Date().getTime());
        }

        $scope.identifier = $scope.gridOverlay.identifier;
        $scope.vm = function () {
            return $scope.gridOverlay.vm;
        }
        $scope.config = function () {
            return $scope.gridOverlay.vm.config;
        }
        $scope.store = function () {
            return $scope.gridOverlay.store;
        }
        $scope.parentScope = $scope.gridOverlay.parentScope;

        $scope.init = function () {
        }

    }]);

}(window));
