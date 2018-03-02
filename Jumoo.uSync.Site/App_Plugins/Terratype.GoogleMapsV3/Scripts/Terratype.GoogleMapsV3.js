(function (root) {
    var identifier = 'Terratype.GoogleMapsV3';
    var Wgs84 = 'WGS84';
    var Gcj02 = 'GCJ02';

    var event = {
        events: [],
        register: function (id, name, scope, object, func) {
            //gm.originalConsole.log("Register " + name + ":" + id);

            event.events.push({
                id: id,
                name: name,
                func: func,
                scope: scope,
                object: object
            });
        },
        cancel: function (id) {
            var newEvents = [];
            angular.forEach(event.events, function (e, i) {
                if (e.id != id) {
                    newEvents.push(e);
                } else {
                    //gm.originalConsole.log("Cancel " + e.name + ":" + e.id);
                }
            });
            event.events = newEvents;
        },
        broadcast: function (name) {
            var log = 'Broadcast ' + name + ' ';
            angular.forEach(event.events, function (e, i) {
                if (e.name == name) {
                    log += e.id + ',';
                    e.func.call(e.scope, e.object);
                }
            });
            //gm.originalConsole.log(log);
        },
        broadcastSingle: function (name, counter) {
            var loop = 0;
            while (loop != 2 && event.events.length != 0) {
                if (counter >= event.events.length) {
                    counter = 0;
                    loop++;
                }

                var e = event.events[counter++];
                if (e.name == name) {
                    e.func.call(e.scope, e.object);
                    return counter;
                }
            }
            return null;
        },
        present: function (id) {
            if (id) {
                var count = 0;
                angular.forEach(event.events, function (e, i) {
                    if (e.id != id) {
                        count++;
                    }
                });
                return count;
            }
            return event.events.length;
        }
    }

    //  Subsystem that loads or destroys Google Map library
    var gm = {
        originalConsole: root.console,
        domain: null,
        version: null,
        apiKey : null,
        coordinateSystem: null,
        forceHttps: false,
        language: null,
        subsystemUninitiated: 0,
        subsystemInit: 1,
        subsystemReadGoogleJs: 2,
        subsystemCheckGoogleJs: 3,
        subsystemLoadedGoogleJs: 4,
        subsystemCooloff: 5,
        subsystemCompleted: 6,
        status: 0,
        killswitch: false,
        poll: 100,
        timeout: 15000,
        fakeConsole: {
            isFake: true,
            error: function (a) {
                if ((a.indexOf('Google Maps API') != -1 || a.indexOf('Google Maps Javascript API') != -1) &&
                    (a.indexOf('MissingKeyMapError') != -1 || a.indexOf('ApiNotActivatedMapError') != -1 ||
                    a.indexOf('InvalidKeyMapError') != -1 || a.indexOf('not authorized') != -1 || a.indexOf('RefererNotAllowedMapError') != -1)) {
                    event.broadcast('gmaperror');
                    gm.destroySubsystem();
                    return;
                }
                try {
                    gm.originalConsole.warn(a);
                }
                catch (oh) {
                }
            },
            warn: function (a) {
                try {
                    gm.originalConsole.warn(a);
                }
                catch (oh) {
                }
            },
            log: function (a) {
                try {
                    gm.originalConsole.log(a);
                }
                catch (oh) {
                }
            }
        },
        installFakeConsole: function () {
            if (typeof (root.console.isFake) === 'undefined') {
                root.console = gm.fakeConsole;
            }
        },
        uninstallFakeConsole: function () {
            root.console = gm.originalConsole;
        },
        isGoogleMapsLoaded: function () {
            return angular.isDefined(root.google) && angular.isDefined(root.google.maps);
        },
        uninstallScript: function (url) {
            var matches = document.getElementsByTagName('script');
            for (var i = matches.length; i >= 0; i--) {
                var match = matches[i];
                if (match && match.getAttribute('src') != null && match.getAttribute('src').indexOf(url) != -1) {
                    match.parentNode.removeChild(match)
                }
            }
        },
        destroySubsystem: function () {
            //gm.originalConsole.log('Destroying subsystem');
            if (gm.searchesTimer != null) {
                clearInterval(gm.searchesTimer);
                gm.searchesTimer = null;
            }
            gm.deleteSearch();
            gm.searches = [];
            gm.checkSearch = 0;
            gm.uninstallFakeConsole();
            root.google.maps.event.clearInstanceListeners(root);
            root.google.maps.event.clearInstanceListeners(document);
            delete root.google;
            if (gm.domain) {
                gm.uninstallScript(gm.domain);
                gm.domain = null;
            }
            gm.status = gm.subsystemUninitiated;
            gm.killswitch = true;
            gm.version = null;
            gm.apiKey = null;
            gm.coordinateSystem = null;
            gm.forceHttps = false;
            gm.language = null;
        },
        ticks: function () {
            return (new Date().getTime());
        },
        createSubsystem: function (version, apiKey, forceHttps, coordinateSystem, language) {
            //gm.originalConsole.log('Creating subsystem');
            root['terratypeGoogleMapsV3Callback'] = function () {
                if (gm.status == gm.subsystemInit || gm.status == gm.subsystemReadGoogleJs) {
                    gm.status = gm.subsystemCheckGoogleJs;
                };
            }
            var start = gm.ticks() + gm.timeout;
            var single = 0;
            var wait = setInterval(function () {
                //gm.originalConsole.warn('Waiting for previous subsystem to die');
                if (gm.ticks() > start) {
                    clearInterval(wait);
                    event.broadcast('gmapkilled');
                    gm.destroySubsystem();
                } else if (gm.status == gm.subsystemCompleted || gm.status == gm.subsystemUninitiated || gm.status == gm.subsystemInit) {
                    //gm.originalConsole.warn('Creating new subsystem');
                    clearInterval(wait);
                    if (!version) {
                        version = '3';  //  Stable release
                    }
                    gm.version = version;
                    gm.forceHttps = forceHttps;
                    var https = '';
                    if (forceHttps) {
                        https = 'https:';
                    }
                    if (coordinateSystem == Gcj02) {
                        //  maps.google.cn only handles http
                        https = 'http:';
                    }
                    gm.coordinateSystem = coordinateSystem;

                    gm.domain = https + ((coordinateSystem == Gcj02) ? '//maps.google.cn/' : '//maps.googleapis.com/');
                    gm.status = gm.subsystemInit;
                    gm.killswitch = false;

                    gm.apiKey = apiKey;
                    var key = '';
                    if (apiKey) {
                        key = '&key=' + apiKey;
                    }

                    gm.language = language;
                    var lan = '';
                    if (language) {
                        lan = '&language=' + language;
                    }

                    start = gm.ticks() + gm.timeout;
                    var timer = setInterval(function () {
                        if (gm.killswitch) {
                            clearInterval(timer);
                        } else {
                            //gm.originalConsole.warn('Subsystem status ' + gm.status);
                            switch (gm.status)
                            {
                                case gm.subsystemInit:
                                    LazyLoad.js(gm.domain + 'maps/api/js?v=' + version + '&libraries=places&callback=terratypeGoogleMapsV3Callback' + key + lan, function () {
                                        //if (gm.status == gm.subsystemInit || gm.status == gm.subsystemReadGoogleJs) {
                                        //    gm.status = gm.subsystemCheckGoogleJs;
                                        //};
                                    });
                                    start = gm.ticks() + gm.timeout;
                                    gm.status = gm.subsystemReadGoogleJs;
                                    break;

                                case gm.subsystemReadGoogleJs:
                                    if (gm.ticks() > start) {
                                        clearInterval(timer);
                                        event.broadcast('gmaperror');
                                        gm.destroySubsystem();
                                    }
                                    break;

                                case gm.subsystemCheckGoogleJs:
                                    if (gm.isGoogleMapsLoaded()) {
                                        gm.installFakeConsole();
                                        gm.status = gm.subsystemLoadedGoogleJs;
                                        event.broadcast('gmaprefresh');
                                    } else if (gm.ticks() > start) {
                                        clearInterval(timer);
                                        event.broadcast('gmaperror');
                                        gm.destroySubsystem();
                                    }
                                    break;

                                case gm.subsystemLoadedGoogleJs:
                                    gm.status = gm.subsystemCooloff;
                                    start = gm.ticks() + gm.timeout;
                                    break;

                                case gm.subsystemCooloff:
                                case gm.subsystemCompleted:
                                    single = event.broadcastSingle('gmaprefresh', single);
                                    if (single == null) {
                                        clearInterval(timer);
                                        gm.destroySubsystem();
                                    } else if (gm.status == gm.subsystemCooloff && gm.ticks() > start) {
                                        gm.status = gm.subsystemCompleted;
                                        gm.uninstallFakeConsole();
                                    }
                                    break;
                            }
                        }
                    }, gm.poll);
                } else {
                    gm.killswitch = true;
                }
            }, gm.poll)
        },
        configIconUrl: function (url) {
            if (typeof (url) === 'undefined' || url == null) {
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
        getAnchorHorizontal: function (text, width) {
            if (typeof text == 'string') {
                switch (text.charAt(0)) {
                    case 'l':
                    case 'L':
                        return 0;

                    case 'c':
                    case 'C':
                    case 'm':
                    case 'M':
                        return width / 2;

                    case 'r':
                    case 'R':
                        return width - 1;
                }
            }
            return Number(text);
        },
        getAnchorVertical: function (text, height) {
            if (typeof text == 'string') {
                switch (text.charAt(0)) {
                    case 't':
                    case 'T':
                        return 0;

                    case 'c':
                    case 'C':
                    case 'm':
                    case 'M':
                        return height / 2;

                    case 'b':
                    case 'B':
                        return height - 1;
                }
            }
            return Number(text);
        },
        icon: function (icon) {
            if (icon.url && icon.size.width && icon.size.height) {
                return {
                    url: gm.configIconUrl(icon.url),
                    scaledSize: new root.google.maps.Size(icon.size.width, icon.size.height),
                    anchor: new root.google.maps.Point(
                        gm.getAnchorHorizontal(icon.anchor.horizontal, icon.size.width),
                        gm.getAnchorVertical(icon.anchor.vertical, icon.size.height)),
                    shadow: icon.shadowImage        /* This has been deprecated */
                }
            } else {
                return { url: 'https://mt.google.com/vt/icon/name=icons/spotlight/spotlight-poi.png' };
            }
        },
        mapTypeIds: function (basic, satellite, terrain) {
            var mapTypeIds = [];
            //  The order they are pushed sets the order of the buttons

            if (basic) {
                mapTypeIds.push('roadmap');
            }
            if (satellite) {
                mapTypeIds.push('satellite');
            }
            if (terrain) {
                mapTypeIds.push('terrain');
            }

            if (mapTypeIds.length == 0) {
                mapTypeIds.push('roadmap');
            }

            return mapTypeIds;
        },

        style: function (name, showRoads, showLandmarks, showLabels) {
            var styles = [];
            
            switch (name)
            {
                case 'silver':             //  Silver
                    styles = [
                        { "elementType": "geometry", "stylers": [{ "color": "#f5f5f5" }] },
                        { "elementType": "labels.text.fill", "stylers": [{ "color": "#616161" }] },
                        { "elementType": "labels.text.stroke", "stylers": [{ "color": "#f5f5f5" }] },
                        { "featureType": "administrative.land_parcel", "elementType": "labels.text.fill", "stylers": [{ "color": "#bdbdbd" }] },
                        { "featureType": "poi", "elementType": "geometry", "stylers": [{ "color": "#eeeeee" }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#757575" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "color": "#e5e5e5" }] },
                        { "featureType": "poi.park", "elementType": "labels.text.fill", "stylers": [{ "color": "#9e9e9e" }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "color": "#ffffff" }] },
                        { "featureType": "road.arterial", "elementType": "labels.text.fill", "stylers": [{ "color": "#757575" }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#dadada" }] },
                        { "featureType": "road.highway", "elementType": "labels.text.fill", "stylers": [{ "color": "#616161" }] },
                        { "featureType": "road.local", "elementType": "labels.text.fill", "stylers": [{ "color": "#9e9e9e" }] },
                        { "featureType": "transit.line", "elementType": "geometry", "stylers": [{ "color": "#e5e5e5" }] },
                        { "featureType": "transit.station", "elementType": "geometry", "stylers": [{ "color": "#eeeeee" }] },
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#c9c9c9" }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "color": "#9e9e9e" }] }
                    ];
                    break;

                case 'retro':             //  Retro
                    styles = [
                        { "elementType": "geometry", "stylers": [{ "color": "#ebe3cd" }] },
                        { "elementType": "labels.text.fill", "stylers": [{ "color": "#523735" }] },
                        { "elementType": "labels.text.stroke", "stylers": [{ "color": "#f5f1e6" }] },
                        { "featureType": "administrative", "elementType": "geometry.stroke", "stylers": [{ "color": "#c9b2a6" }] },
                        { "featureType": "administrative.land_parcel", "elementType": "geometry.stroke", "stylers": [{ "color": "#dcd2be" }] },
                        { "featureType": "administrative.land_parcel", "elementType": "labels.text.fill", "stylers": [{ "color": "#ae9e90" }] },
                        { "featureType": "landscape.natural", "elementType": "geometry", "stylers": [{ "color": "#dfd2ae" }] },
                        { "featureType": "poi", "elementType": "geometry", "stylers": [{ "color": "#dfd2ae" }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#93817c" }] },
                        { "featureType": "poi.park", "elementType": "geometry.fill", "stylers": [{ "color": "#a5b076" }] },
                        { "featureType": "poi.park", "elementType": "labels.text.fill", "stylers": [{ "color": "#447530" }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "color": "#f5f1e6" }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "color": "#fdfcf8" }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#f8c967" }] },
                        { "featureType": "road.highway", "elementType": "geometry.stroke", "stylers": [{ "color": "#e9bc62" }] },
                        { "featureType": "road.highway.controlled_access", "elementType": "geometry", "stylers": [{ "color": "#e98d58" }] },
                        { "featureType": "road.highway.controlled_access", "elementType": "geometry.stroke", "stylers": [{ "color": "#db8555" }] },
                        { "featureType": "road.local", "elementType": "labels.text.fill", "stylers": [{ "color": "#806b63" }] },
                        { "featureType": "transit.line", "elementType": "geometry", "stylers": [{ "color": "#dfd2ae" }] },
                        { "featureType": "transit.line", "elementType": "labels.text.fill", "stylers": [{ "color": "#8f7d77" }] },
                        { "featureType": "transit.line", "elementType": "labels.text.stroke", "stylers": [{ "color": "#ebe3cd" }] },
                        { "featureType": "transit.station", "elementType": "geometry", "stylers": [{ "color": "#dfd2ae" }] },
                        { "featureType": "water", "elementType": "geometry.fill", "stylers": [{ "color": "#b9d3c2" }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "color": "#92998d" }] }
                    ];
                    break;

                case 'dark':             //  Dark
                    styles = [
                        { "elementType": "geometry", "stylers": [{ "color": "#212121" }] },
                        { "elementType": "labels.icon", "stylers": [{ "visibility": "off" }] },
                        { "elementType": "labels.text.fill", "stylers": [{ "color": "#757575" }] },
                        { "elementType": "labels.text.stroke", "stylers": [{ "color": "#212121" }] },
                        { "featureType": "administrative", "elementType": "geometry", "stylers": [{ "color": "#757575" }] },
                        { "featureType": "administrative.country", "elementType": "labels.text.fill", "stylers": [{ "color": "#9e9e9e" }] },
                        { "featureType": "administrative.land_parcel", "stylers": [{ "visibility": "off" }] },
                        { "featureType": "administrative.locality", "elementType": "labels.text.fill", "stylers": [{ "color": "#bdbdbd" }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#757575" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "color": "#181818" }] },
                        { "featureType": "poi.park", "elementType": "labels.text.fill", "stylers": [{ "color": "#616161" }] },
                        { "featureType": "poi.park", "elementType": "labels.text.stroke", "stylers": [{ "color": "#1b1b1b" }] },
                        { "featureType": "road", "elementType": "geometry.fill", "stylers": [{ "color": "#2c2c2c" }] },
                        { "featureType": "road", "elementType": "labels.text.fill", "stylers": [{ "color": "#8a8a8a" }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "color": "#373737" }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#3c3c3c" }] },
                        { "featureType": "road.highway.controlled_access", "elementType": "geometry", "stylers": [{ "color": "#4e4e4e" }] },
                        { "featureType": "road.local", "elementType": "labels.text.fill", "stylers": [{ "color": "#616161" }] },
                        { "featureType": "transit", "elementType": "labels.text.fill", "stylers": [{ "color": "#757575" }] },
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#000000" }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "color": "#3d3d3d" }] }
                    ];
                    break;

                case 'night':             //  Night
                    styles = [
                        { "elementType": "geometry", "stylers": [{ "color": "#242f3e" }] },
                        { "elementType": "labels.text.fill", "stylers": [{ "color": "#746855" }] },
                        { "elementType": "labels.text.stroke", "stylers": [{ "color": "#242f3e" }] },
                        { "featureType": "administrative.locality", "elementType": "labels.text.fill", "stylers": [{ "color": "#d59563" }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#d59563" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "color": "#263c3f" }] },
                        { "featureType": "poi.park", "elementType": "labels.text.fill", "stylers": [{ "color": "#6b9a76" }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "color": "#38414e" }] },
                        { "featureType": "road", "elementType": "geometry.stroke", "stylers": [{ "color": "#212a37" }] },
                        { "featureType": "road", "elementType": "labels.text.fill", "stylers": [{ "color": "#9ca5b3" }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#746855" }] },
                        { "featureType": "road.highway", "elementType": "geometry.stroke", "stylers": [{ "color": "#1f2835" }] },
                        { "featureType": "road.highway", "elementType": "labels.text.fill", "stylers": [{ "color": "#f3d19c" }] },
                        { "featureType": "transit", "elementType": "geometry", "stylers": [{ "color": "#2f3948" }] },
                        { "featureType": "transit.station", "elementType": "labels.text.fill", "stylers": [{ "color": "#d59563" }] },
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#17263c" }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "color": "#515c6d" }] },
                        { "featureType": "water", "elementType": "labels.text.stroke", "stylers": [{ "color": "#17263c" }] }
                    ];
                    break;

                case 'desert':             //  Desert
                    styles = [
                        { "featureType": "administrative", "elementType": "all", "stylers": [{ "visibility": "on" }, { "lightness": 33 }] },
                        { "featureType": "landscape", "elementType": "all", "stylers": [{ "color": "#f2e5d4" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "color": "#c5dac6" }] },
                        { "featureType": "poi.park", "elementType": "labels", "stylers": [{ "visibility": "on" }, { "lightness": 20 }] },
                        { "featureType": "road", "elementType": "all", "stylers": [{ "lightness": 20 }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#c5c6c6" }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "color": "#e4d7c6" }] },
                        { "featureType": "road.local", "elementType": "geometry", "stylers": [{ "color": "#fbfaf7" }] },
                        { "featureType": "water", "elementType": "all", "stylers": [{ "color": "#acbcc9" }] }
                    ];
                    break;

                case 'blush':             //  Blush
                    styles = [
                        { "stylers": [{ "hue": "#dd0d0d" }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "lightness": 100 }, { "visibility": "simplified" }] }
                    ];
                    break;

                case 'unsaturatedbrowns':             //  Unsaturated Browns
                    styles = [
                        { "elementType": "geometry", "stylers": [{ "hue": "#ff4400" }, { "saturation": -68 }, { "lightness": -4 }, { "gamma": 0.72 }] },
                        { "featureType": "road", "elementType": "labels.icon" }, { "featureType": "landscape.man_made", "elementType": "geometry", "stylers": [{ "hue": "#0077ff" }, { "gamma": 3.1 }] },
                        { "featureType": "water", "stylers": [{ "hue": "#00ccff" }, { "gamma": 0.44 }, { "saturation": -33 }] },
                        { "featureType": "poi.park", "stylers": [{ "hue": "#44ff00" }, { "saturation": -23 }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "hue": "#007fff" }, { "gamma": 0.77 }, { "saturation": 65 }, { "lightness": 99 }] },
                        { "featureType": "water", "elementType": "labels.text.stroke", "stylers": [{ "gamma": 0.11 }, { "weight": 5.6 }, { "saturation": 99 }, { "hue": "#0091ff" }, { "lightness": -86 }] },
                        { "featureType": "transit.line", "elementType": "geometry", "stylers": [{ "lightness": -48 }, { "hue": "#ff5e00" }, { "gamma": 1.2 }, { "saturation": -23 }] },
                        { "featureType": "transit", "elementType": "labels.text.stroke", "stylers": [{ "saturation": -64 }, { "hue": "#ff9100" }, { "lightness": 16 }, { "gamma": 0.47 }, { "weight": 2.7 }] }
                    ];
                    break;

                case 'lightdream':             //  Light Dream
                    styles = [
                        { "featureType": "landscape", "stylers": [{ "hue": "#FFBB00" }, { "saturation": 43.4 }, { "lightness": 37.6 }, { "gamma": 1 }] },
                        { "featureType": "road.highway", "stylers": [{ "hue": "#FFC200" }, { "saturation": -61.8 }, { "lightness": 45.6 }, { "gamma": 1 }] },
                        { "featureType": "road.arterial", "stylers": [{ "hue": "#FF0300" }, { "saturation": -100 }, { "lightness": 51.2 }, { "gamma": 1 }] },
                        { "featureType": "road.local", "stylers": [{ "hue": "#FF0300" }, { "saturation": -100 }, { "lightness": 52 }, { "gamma": 1 }] },
                        { "featureType": "water", "stylers": [{ "hue": "#0078FF" }, { "saturation": -13.2 }, { "lightness": 2.4 }, { "gamma": 1 }] },
                        { "featureType": "poi", "stylers": [{ "hue": "#00FF6A" }, { "saturation": -1.1 }, { "lightness": 11.2 }, { "gamma": 1 }] }
                    ];
                    break;

                case 'paledawn':             //  Pale Dawn
                    styles = [
                        { "featureType": "administrative", "elementType": "all", "stylers": [{ "lightness": 33 }] },
                        { "featureType": "landscape", "elementType": "all", "stylers": [{ "color": "#f2e5d4" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "color": "#c5dac6" }] },
                        { "featureType": "poi.park", "elementType": "labels", "stylers": [{ "lightness": 20 }] },
                        { "featureType": "road", "elementType": "all", "stylers": [{ "lightness": 20 }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#c5c6c6" }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "color": "#e4d7c6" }] },
                        { "featureType": "road.local", "elementType": "geometry", "stylers": [{ "color": "#fbfaf7" }] },
                        { "featureType": "water", "elementType": "all", "stylers": [{ "color": "#acbcc9" }] }
                    ];
                    break;

                case 'crisp':            //  Crisp
                    styles = [
                        { "featureType": "administrative.country", "elementType": "geometry", "stylers": [{ "visibility": "simplified" }, { "hue": "#ff0000" }] }
                    ];
                    break;

                case 'mapbox':            //  MapBox
                    styles = [
                        { "featureType": "water", "stylers": [{ "saturation": 43 }, { "lightness": -11 }, { "hue": "#0088ff" }] },
                        { "featureType": "road", "elementType": "geometry.fill", "stylers": [{ "hue": "#ff0000" }, { "saturation": -100 }, { "lightness": 99 }] },
                        { "featureType": "road", "elementType": "geometry.stroke", "stylers": [{ "color": "#808080" }, { "lightness": 54 }] },
                        { "featureType": "landscape.man_made", "elementType": "geometry.fill", "stylers": [{ "color": "#ece2d9" }] },
                        { "featureType": "poi.park", "elementType": "geometry.fill", "stylers": [{ "color": "#ccdca1" }] },
                        { "featureType": "road", "elementType": "labels.text.fill", "stylers": [{ "color": "#767676" }] },
                        { "featureType": "road", "elementType": "labels.text.stroke", "stylers": [{ "color": "#ffffff" }] },
                        { "featureType": "landscape.natural", "elementType": "geometry.fill", "stylers": [{ "color": "#b8cb93" }] },
                        { "featureType": "poi.sports_complex", "stylers": [{ "visibility": "on" }] }
                    ];
                    break;

                case 'shiftworker':            //  Shift Worker
                    styles = [
                        { "stylers": [{ "saturation": -100 }, { "gamma": 1 }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "visibility": "simplified" }] },
                        { "featureType": "water", "stylers": [{ "visibility": "on" }, { "saturation": 50 }, { "gamma": 0 }, { "hue": "#50a5d1" }] },
                        { "featureType": "administrative.neighborhood", "elementType": "labels.text.fill", "stylers": [{ "color": "#333333" }] },
                        { "featureType": "road.local", "elementType": "labels.text", "stylers": [{ "weight": 0.5 }, { "color": "#333333" }] },
                        { "featureType": "transit.station", "elementType": "labels.icon", "stylers": [{ "gamma": 1 }, { "saturation": 50 }] }
                    ];
                    break;

                case 'mutedblue':            //  Muted Blue
                    styles = [
                        { "featureType": "all", "stylers": [{ "saturation": 0 }, { "hue": "#e7ecf0" }] },
                        { "featureType": "road", "stylers": [{ "saturation": -70 }] },
                        { "featureType": "water", "stylers": [{ "visibility": "simplified" }, { "saturation": -60 }] }
                    ];
                    break;

                case 'avocado':            //  Avocado
                    styles = [
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#aee2e0" }] },
                        { "featureType": "landscape", "elementType": "geometry.fill", "stylers": [{ "color": "#abce83" }] },
                        { "featureType": "poi", "elementType": "geometry.fill", "stylers": [{ "color": "#769E72" }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#7B8758" }] },
                        { "featureType": "poi", "elementType": "labels.text.stroke", "stylers": [{ "color": "#EBF4A4" }] },
                        { "featureType": "poi.park", "elementType": "geometry", "stylers": [{ "visibility": "simplified" }, { "color": "#8dab68" }] },
                        { "featureType": "road", "elementType": "geometry.fill", "stylers": [{ "visibility": "simplified" }] },
                        { "featureType": "road", "elementType": "labels.text.fill", "stylers": [{ "color": "#5B5B3F" }] },
                        { "featureType": "road", "elementType": "labels.text.stroke", "stylers": [{ "color": "#ABCE83" }] },
                        { "featureType": "road", "elementType": "labels.icon", "stylers": [{ "visibility": "off" }] },
                        { "featureType": "road.local", "elementType": "geometry", "stylers": [{ "color": "#A4C67D" }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "color": "#9BBF72" }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "color": "#EBF4A4" }] },
                        { "featureType": "administrative", "elementType": "geometry.stroke", "stylers": [{ "color": "#87ae79" }] },
                        { "featureType": "administrative", "elementType": "geometry.fill", "stylers": [{ "color": "#7f2200" }, { "visibility": "off" }] },
                        { "featureType": "administrative", "elementType": "labels.text.stroke", "stylers": [{ "color": "#ffffff" }, { "weight": 4.1 }] },
                        { "featureType": "administrative", "elementType": "labels.text.fill", "stylers": [{ "color": "#495421" }] },
                    ];
                    break;

                case 'colbalt':            //  Colbalt
                    styles = [
                        { "featureType": "all", "elementType": "all", "stylers": [{ "invert_lightness": true }, { "saturation": 10 }, { "lightness": 30 }, { "gamma": 0.5 }, { "hue": "#435158" }] }
                    ];
                    break;

                case 'ice':            //  Ice
                    styles = [
                        { "stylers": [{ "hue": "#2c3e50" }, { "saturation": 250 }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "lightness": 50 }, { "visibility": "simplified" }] },
                    ];
                    break;

                case 'brightandbubbly':            //  Bright & Bubbly
                    styles = [
                        { "featureType": "water", "stylers": [{ "color": "#19a0d8" }] },
                        { "featureType": "administrative", "elementType": "labels.text.stroke", "stylers": [{ "color": "#ffffff" }, { "weight": 6 }] },
                        { "featureType": "administrative", "elementType": "labels.text.fill", "stylers": [{ "color": "#e85113" }] },
                        { "featureType": "road.highway", "elementType": "geometry.stroke", "stylers": [{ "color": "#efe9e4" }, { "lightness": -40 }] },
                        { "featureType": "road.arterial", "elementType": "geometry.stroke", "stylers": [{ "color": "#efe9e4" }, { "lightness": -20 }] },
                        { "featureType": "road", "elementType": "labels.text.stroke", "stylers": [{ "lightness": 100 }] },
                        { "featureType": "road", "elementType": "labels.text.fill", "stylers": [{ "lightness": -100 }] },
                        { "featureType": "road.highway", "elementType": "labels.icon" },
                        { "featureType": "landscape", "stylers": [{ "lightness": 20 }, { "color": "#efe9e4" }] },
                        { "featureType": "water", "elementType": "labels.text.stroke", "stylers": [{ "lightness": 100 }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "lightness": -100 }] },
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "hue": "#11ff00" }] },
                        { "featureType": "poi", "elementType": "labels.text.stroke", "stylers": [{ "lightness": 100 }] },
                        { "featureType": "poi", "elementType": "labels.icon", "stylers": [{ "hue": "#4cff00" }, { "saturation": 58 }] },
                        { "featureType": "poi", "elementType": "geometry", "stylers": [{ "visibility": "on" }, { "color": "#f0e4d3" }] },
                        { "featureType": "road.highway", "elementType": "geometry.fill", "stylers": [{ "color": "#efe9e4" }, { "lightness": -25 }] },
                        { "featureType": "road.arterial", "elementType": "geometry.fill", "stylers": [{ "color": "#efe9e4" }, { "lightness": -10 }] },
                        { "featureType": "poi", "elementType": "labels", "stylers": [{ "visibility": "simplified" }] }
                    ];
                    break;

                case 'hopper':            //  Hopper
                    styles = [
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "hue": "#165c64" }, { "saturation": 34 }, { "lightness": -69 }] },
                        { "featureType": "landscape", "elementType": "geometry", "stylers": [{ "hue": "#b7caaa" }, { "saturation": -14 }, { "lightness": -18 }] },
                        { "featureType": "landscape.man_made", "elementType": "all", "stylers": [{ "hue": "#cbdac1" }, { "saturation": -6 }, { "lightness": -9 }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "hue": "#8d9b83" }, { "saturation": -89 }, { "lightness": -12 }] },
                        { "featureType": "road.highway", "elementType": "geometry", "stylers": [{ "hue": "#d4dad0" }, { "saturation": -88 }, { "lightness": 54 }] },
                        { "featureType": "road.arterial", "elementType": "geometry", "stylers": [{ "hue": "#bdc5b6" }, { "saturation": -89 }, { "lightness": -3 }] },
                        { "featureType": "road.local", "elementType": "geometry", "stylers": [{ "hue": "#bdc5b6" }, { "saturation": -89 }, { "lightness": -26 }] },
                        { "featureType": "poi", "elementType": "geometry", "stylers": [{ "hue": "#c17118" }, { "saturation": 61 }, { "lightness": -45 }] },
                        { "featureType": "poi.park", "elementType": "all", "stylers": [{ "hue": "#8ba975" }, { "saturation": -46 }, { "lightness": -28 }] },
                        { "featureType": "transit", "elementType": "geometry", "stylers": [{ "hue": "#a43218" }, { "saturation": 74 }, { "lightness": -51 }] },
                        { "featureType": "administrative.province", "elementType": "all", "stylers": [{ "hue": "#ffffff" }, { "saturation": 0 }, { "lightness": 100 }] },
                        { "featureType": "administrative.neighborhood", "elementType": "all", "stylers": [{ "hue": "#ffffff" }, { "saturation": 0 }, { "lightness": 100 }] },
                        { "featureType": "administrative.locality", "elementType": "labels", "stylers": [{ "color": "#b7caaa" }, { "weight": 0.1 }] },
                        { "featureType": "administrative.land_parcel", "elementType": "all", "stylers": [{ "hue": "#ffffff" }, { "saturation": 0 }, { "lightness": 100 }] },
                        { "featureType": "administrative", "elementType": "all", "stylers": [{ "hue": "#3a3935" }, { "saturation": 5 }, { "lightness": -57 }] },
                        { "featureType": "poi.medical", "elementType": "geometry", "stylers": [{ "hue": "#cba923" }, { "saturation": 50 }, { "lightness": -46 }] }
                    ];
                    break;

                case 'lost':            //  Lost
                    styles = [
                        { "elementType": "labels", "stylers": [{ "color": "#52270b" }, { "weight": 0.1 }] },
                        { "featureType": "landscape", "stylers": [{ "color": "#f9ddc5" }, { "lightness": -7 }] },
                        { "featureType": "road", "stylers": [{ "color": "#813033" }, { "lightness": 43 }] },
                        { "featureType": "poi.business", "stylers": [{ "color": "#645c20" }, { "lightness": 38 }] },
                        { "featureType": "water", "stylers": [{ "color": "#1994bf" }, { "saturation": -69 }, { "gamma": 0.99 }, { "lightness": 43 }] },
                        { "featureType": "road.local", "elementType": "geometry.fill", "stylers": [{ "color": "#f19f53" }, { "weight": 1.3 }, { "lightness": 16 }] },
                        { "featureType": "poi.business" }, { "featureType": "poi.park", "stylers": [{ "color": "#645c20" }, { "lightness": 39 }] },
                        { "featureType": "poi.school", "stylers": [{ "color": "#a95521" }, { "lightness": 35 }] },
                        { "featureType": "poi.medical", "elementType": "geometry.fill", "stylers": [{ "color": "#813033" }, { "lightness": 38 }] },
                        { "featureType": "poi.sports_complex", "stylers": [{ "color": "#9e5916" }, { "lightness": 32 }] },
                        { "featureType": "poi.government", "stylers": [{ "color": "#9e5916" }, { "lightness": 46 }] },
                        { "featureType": "transit.line", "stylers": [{ "color": "#813033" }, { "lightness": 22 }] },
                        { "featureType": "transit", "stylers": [{ "lightness": 38 }] },
                        { "featureType": "road.local", "elementType": "geometry.stroke", "stylers": [{ "color": "#f19f53" }, { "lightness": -10 }] }
                    ];
                    break;

                case 'redalert':            //  Red Alert
                    styles = [
                        { "featureType": "water", "elementType": "geometry", "stylers": [{ "color": "#ffdfa6" }] },
                        { "featureType": "landscape", "elementType": "geometry", "stylers": [{ "color": "#b52127" }] },
                        { "featureType": "poi", "elementType": "geometry", "stylers": [{ "color": "#c5531b" }] },
                        { "featureType": "road.highway", "elementType": "geometry.fill", "stylers": [{ "color": "#74001b" }, { "lightness": -10 }] },
                        { "featureType": "road.highway", "elementType": "geometry.stroke", "stylers": [{ "color": "#da3c3c" }] },
                        { "featureType": "road.arterial", "elementType": "geometry.fill", "stylers": [{ "color": "#74001b" }] },
                        { "featureType": "road.arterial", "elementType": "geometry.stroke", "stylers": [{ "color": "#da3c3c" }] },
                        { "featureType": "road.local", "elementType": "geometry.fill", "stylers": [{ "color": "#990c19" }] },
                        { "elementType": "labels.text.fill", "stylers": [{ "color": "#ffffff" }] },
                        { "elementType": "labels.text.stroke", "stylers": [{ "color": "#74001b" }, { "lightness": -8 }] },
                        { "featureType": "transit", "elementType": "geometry", "stylers": [{ "color": "#6a0d10" }] },
                        { "featureType": "administrative", "elementType": "geometry", "stylers": [{ "color": "#ffdfa6" }, { "weight": 0.4 }] },
                    ];
                    break;

                case 'olddrymud':            //  Old Dry Mud
                    styles = [
                        { "featureType": "landscape", "stylers": [{ "hue": "#FFAD00" }, { "saturation": 50.2 }, { "lightness": -34.8 }, { "gamma": 1 }] },
                        { "featureType": "road.highway", "stylers": [{ "hue": "#FFAD00" }, { "saturation": -19.8 }, { "lightness": -1.8 }, { "gamma": 1 }] },
                        { "featureType": "road.arterial", "stylers": [{ "hue": "#FFAD00" }, { "saturation": 72.4 }, { "lightness": -32.6 }, { "gamma": 1 }] },
                        { "featureType": "road.local", "stylers": [{ "hue": "#FFAD00" }, { "saturation": 74.4 }, { "lightness": -18 }, { "gamma": 1 }] },
                        { "featureType": "water", "stylers": [{ "hue": "#00FFA6" }, { "saturation": -63.2 }, { "lightness": 38 }, { "gamma": 1 }] },
                        { "featureType": "poi", "stylers": [{ "hue": "#FFC300" }, { "saturation": 54.2 }, { "lightness": -14.4 }, { "gamma": 1 }] }
                    ];
                    break;

                case 'flat':            //  Flat
                    styles = [
                        { "featureType": "poi", "elementType": "labels.text.fill", "stylers": [{ "color": "#747474" }, { "lightness": "23" }] },
                        { "featureType": "poi.attraction", "elementType": "geometry.fill", "stylers": [{ "color": "#f38eb0" }] },
                        { "featureType": "poi.government", "elementType": "geometry.fill", "stylers": [{ "color": "#ced7db" }] },
                        { "featureType": "poi.medical", "elementType": "geometry.fill", "stylers": [{ "color": "#ffa5a8" }] },
                        { "featureType": "poi.park", "elementType": "geometry.fill", "stylers": [{ "color": "#c7e5c8" }] },
                        { "featureType": "poi.place_of_worship", "elementType": "geometry.fill", "stylers": [{ "color": "#d6cbc7" }] },
                        { "featureType": "poi.school", "elementType": "geometry.fill", "stylers": [{ "color": "#c4c9e8" }] },
                        { "featureType": "poi.sports_complex", "elementType": "geometry.fill", "stylers": [{ "color": "#b1eaf1" }] },
                        { "featureType": "road", "elementType": "geometry", "stylers": [{ "lightness": "100" }] },
                        { "featureType": "road", "elementType": "labels", "stylers": [{ "lightness": "100" }] },
                        { "featureType": "road.highway", "elementType": "geometry.fill", "stylers": [{ "color": "#ffd4a5" }] },
                        { "featureType": "road.arterial", "elementType": "geometry.fill", "stylers": [{ "color": "#ffe9d2" }] },
                        { "featureType": "road.local", "elementType": "all", "stylers": [{ "visibility": "simplified" }] },
                        { "featureType": "road.local", "elementType": "geometry.fill", "stylers": [{ "weight": "3.00" }] },
                        { "featureType": "road.local", "elementType": "geometry.stroke", "stylers": [{ "weight": "0.30" }] },
                        { "featureType": "road.local", "elementType": "labels.text", "stylers": [{ "visibility": "on" }] },
                        { "featureType": "road.local", "elementType": "labels.text.fill", "stylers": [{ "color": "#747474" }, { "lightness": "36" }] },
                        { "featureType": "road.local", "elementType": "labels.text.stroke", "stylers": [{ "color": "#e9e5dc" }, { "lightness": "30" }] },
                        { "featureType": "transit.line", "elementType": "geometry", "stylers": [{ "lightness": "100" }] },
                        { "featureType": "water", "elementType": "all", "stylers": [{ "color": "#d2e7f7" }] }
                    ];
                    break;

                case 'hotel':            //  Hotel
                    styles = [
                        { "featureType": "landscape.man_made", "elementType": "geometry.fill", "stylers": [{ "lightness": "-5" }] },
                        { "featureType": "landscape.man_made", "elementType": "labels.text.fill", "stylers": [{ "saturation": "21" }] },
                        { "featureType": "landscape.natural", "elementType": "geometry.fill", "stylers": [{ "saturation": "1" }, { "color": "#eae2d3" }, { "lightness": "20" }] },
                        { "featureType": "road.highway", "elementType": "labels.icon", "stylers": [{ "saturation": "39" }, { "lightness": "7" }, { "gamma": "1.06" }, { "hue": "#00b8ff" }, { "weight": "1.44" }] },
                        { "featureType": "road.arterial", "elementType": "geometry.stroke", "stylers": [{ "lightness": "100" }, { "weight": "1.16" }, { "color": "#e0e0e0" }] },
                        { "featureType": "road.arterial", "elementType": "labels.icon", "stylers": [{ "saturation": "-16" }, { "lightness": "28" }, { "gamma": "0.87" }] },
                        { "featureType": "water", "elementType": "geometry.fill", "stylers": [{ "saturation": "-75" }, { "lightness": "-15" }, { "gamma": "1.35" }, { "weight": "1.45" }, { "hue": "#00dcff" }] },
                        { "featureType": "water", "elementType": "labels.text.fill", "stylers": [{ "color": "#626262" }] }, { "featureType": "water", "elementType": "labels.text.stroke", "stylers": [{ "saturation": "19" }, { "weight": "1.84" }] }
                    ];
                    break;
            }

            function setVisibilityOff(s, f, e) {
                angular.forEach(s, function (value) {
                    if ((!f && value.featureType == f) && (!e && value.elementType == e)) {
                        value.styles = {
                            "visibility": "off"
                        }
                        return;
                    }
                });
                var o = {};
                if (f) {
                    o.featureType = f;
                }
                if (e) {
                    o.elementType = e;
                }
                o.stylers = [{
                    "visibility": "off"
                }];
                s.push(o);
                return s;
            }

            if (!showRoads) {
                styles = setVisibilityOff(styles, 'road', 'all');
            }
            if (!showLandmarks) {
                styles = setVisibilityOff(styles, 'administrative', 'geometry');
                styles = setVisibilityOff(styles, 'poi');
                styles = setVisibilityOff(styles, 'transit');
            }

            if (!showLabels) {
                styles = setVisibilityOff(styles, null, 'labels');
                styles = setVisibilityOff(styles, 'administrative');
                styles = setVisibilityOff(styles, 'administrative.land_parcel');
                styles = setVisibilityOff(styles, 'administrative.neighborhood');
            }
            return styles;
        },
        searchCountries: function (countries) {
            if (!countries || !(countries instanceof Array) || countries.length == 0) {
                return null;
            }
            if (countries.length == 1) {
                return countries[0];
            }
            return countries;
        },
        round: function (num, decimals) {
            var sign = num >= 0 ? 1 : -1;
            var pow = Math.pow(10, decimals);
            return parseFloat((Math.round((num * pow) + (sign * 0.001)) / pow).toFixed(decimals));
        },
        searches: [],
        searchesTimer: null,
        checkSearch: 0,
        createSearch: function (id, source, destination, options, gmap, done) {
            gm.searches.push({ status: 0, id: id, source: source, destination: destination, options: options, done: done });
            if (gm.searchesTimer == null) {
                gm.searchesTimer = setInterval(function () {
                    if (gm.checkSearch == 0) {
                        gm.checkSearch = 1;
                        if (root.google.maps.places) {
                            var service = new root.google.maps.places.PlacesService(gmap);
                            service.textSearch({ query: 'paris, france' }, function (results, status) {
                                if (root.google) {
                                    gm.checkSearch = (status == 'OK') ? 2 : -1;
                                }
                            });
                            return;
                        } else {
                            gm.checkSearch = -1;
                        }
                    } else if (gm.checkSearch == 1) {
                        return;
                    }

                    var kill = true;
                    for (var i = 0; i != gm.searches.length; i++) {
                        var s = gm.searches[i];
                        if (s.status == 0) {
                            if (gm.checkSearch == -1) {
                                s.status = 3;
                                s.done(null);
                                return;
                            }
                            s.autocomplete = new root.google.maps.places.Autocomplete(s.source, s.options);
                            s.status = 1;
                            return;
                        }
                        if (s.status == 1) {
                            var m = document.body.childNodes;
                            for (p = 0; p != m.length; p++) {
                                if (m[p].nodeType == 1 && m[p].className && m[p].className.indexOf('pac-container') != -1 && !m[p].hasAttribute('data-terratype-id')) {
                                    m[p].className += ' terratype_' + id + '_googlemapv3_lookup_results';
                                    m[p].setAttribute('data-terratype-id', s.id);
                                    s.destination.appendChild(m[p]);
                                    s.status = 2;
                                    s.done(s.autocomplete);
                                    return;
                                }
                            }
                            kill = false;
                        }
                    }
                    if (kill) {
                        clearInterval(gm.searchesTimer);
                        gm.searchesTimer = null;
                    }
                }, gm.poll);
            }
        },
        deleteSearch: function (id) {
            //for (var i = 0; i != gm.searches.length; i++) {
            //    var s = gm.searches[i];
            //    if ((!id || s.id == id) && s.status == 2) {
            //        var m = document.body.childNodes;
            //        for (p = m.length - 1; p >= 0; p--) {
            //            if (m[p].nodeType == 1 && m[p].className && m[p].className.indexOf('pac-container') != -1 && m[p].getAttribute('data-terratype-id') === id) {
            //                document.removeChild(m[p]);
            //            }
            //        }
            //        s.status = 3;
            //    }
            //}
        },
        mergeJson: function (aa, bb) {        //  Does not merge arrays
            var mi = function (c) {
                var t = {};
                for (var k in c) {
                    if (typeof c[k] === 'object' && c[k].constructor.name !== 'Array') {
                        t[k] = mi(c[k]);
                    } else {
                        t[k] = c[k];
                    }
                }
                return t;
            }
            var mo = function (a, b) {
                var r = (a) ? mi(a) : {};
                if (b) {
                    for (var k in b) {
                        if (r[k] && typeof r[k] === 'object' && r[k].constructor.name !== 'Array') {
                            r[k] = mo(r[k], b[k]);
                        } else {
                            r[k] = b[k];
                        }
                    }
                }
                return r;
            }
            return mo(aa, bb);
        },
    }

    var provider = {
        identifier: identifier,
        datumWait: 330,
        css: [],
        js: [],
        boot: function (id, urlProvider, store, config, vm, updateView, translate, done) {
            var scope = {
                events: [],
                datumChangeWait: null,
                defaultConfig: {
                    position: {
                        datum: "55.4063207,10.3870147"
                    },
                    zoom: 12,
                    provider: {
                        id: identifier, 
                        version: 3,
                        forceHttps: true,
                        language: '',
                        predefineStyling: 'retro',
                        showRoads: true,
                        showLandmarks: true,
                        showLabels: true,
                        variety: {
                            basic: true,
                            satellite: false,
                            terrain: false,
                            selector: {
                                type: 1,     // Horizontal Bar
                                position: 0  // Default
                            }
                        },
                        streetView: {
                            enable: false,
                            position: 0
                        },
                        fullscreen: {
                            enable: false,
                            position: 0
                        },
                        scale: {
                            enable: false,
                            position: 0
                        },
                        zoomControl: {
                            enable: true,
                            position: 0,
                        },
                        panControl: {
                            enable: false
                        },
                        draggable: true
                    },
                    search: {
                        enable: 0,
                        limit: {
                            countries: []
                        }
                    }
                },
                initValues: function () {
                    if (!store().position.datum) {
                        store().position.datum = scope.defaultConfig.position.datum;
                    }
                    vm().position.datum = scope.parse.call(scope, store().position.datum);

                    if (!store().zoom) {
                        store().zoom = scope.defaultConfig.zoom;
                    }
                    config().provider = gm.mergeJson(scope.defaultConfig.provider, config().provider);
                    config().search = gm.mergeJson(scope.defaultConfig.search, config().search);
                },
                init: function (done) {
                    //event.cancel(id);
                    if (store().position) {
                        if (typeof store().position.datum === 'string') {
                            vm().position.datum = scope.parse.call(scope, store().position.datum);
                        }
                    }
                    if (vm().isPreview == false && config().provider && config().provider.version && store().position && store().position.id && vm().position.precision) {
                        scope.loadMap.call(scope);
                    }

                    done({
                        httpCalls: {
                            'apiKey': {
                                when: 'config.provider.id=' + identifier,
                                field: 'config.provider.apiKey',
                                values: []
                            }
                        },
                        files: {
                            logo: urlProvider(identifier, 'images/Logo.png'),
                            mapExample: urlProvider(identifier, 'images/Example.png'),
                            views: {
                                config: {
                                    definition: urlProvider(identifier, 'views/config.definition.html', true),
                                    appearance: urlProvider(identifier, 'views/config.appearance.html', true),
                                    search: urlProvider(identifier, 'views/config.search.html', true)
                                },
                                editor: {
                                    appearance: urlProvider(identifier, 'views/editor.appearance.html', true)
                                },
                                grid: {
                                    appearance: urlProvider(identifier, 'views/grid.appearance.html', true)
                                }
                            }
                        },
                        setProvider: function () {
                            if (vm().provider.id != identifier) {
                                scope.destroy();
                            }
                        },
                        setCoordinateSystem: function () {
                            if (store().position && store().position.id != gm.coordinateSystem) {
                                scope.reloadMap.call(scope);
                            }
                        },
                        setIcon: function () {
                            if (scope.gmarker) {
                                scope.gmarker.setIcon(gm.icon.call(gm, config().icon));
                            }
                        },
                        forceHttpsChange: function () {
                            if (config().provider.forceHttps != gm.forceHttps) {
                                scope.reloadMap.call(scope);
                            }
                        },
                        languageChange: function () {
                            if (config().provider.language != gm.language) {
                                scope.reloadMap.call(scope);
                            }
                        },
                        versionChange: function () {
                            if (config().provider.version != gm.version) {
                                scope.reloadMap.call(scope);
                            }
                        },
                        styleChange: function () {
                            config().provider.styles = gm.style.call(gm, config().provider.predefineStyling, config().provider.showRoads,
                                    config().provider.showLandmarks, config().provider.showLabels);
                            if (scope.gmap) {
                                scope.gmap.setOptions({
                                    styles: config().provider.styles
                                });
                            }
                        },
                        datumChange: function (text) {
                            vm().datumChangeText = text;
                            if (scope.datumChangeWait) {
                                clearTimeout(scope.datumChangeWait);
                            }
                            scope.datumChangeWait = setTimeout(function () {
                                scope.datumChangeWait = null;
                                var p = scope.parse.call(scope, vm().datumChangeText);
                                if (typeof p !== 'boolean') {
                                    vm().position.datum = p;
                                    scope.setDatum.call(scope);
                                    scope.setMarker.call(scope);
                                    return;
                                }
                                vm().position.datumStyle = { 'color': 'red' };
                            }, provider.datumWait);
                        },
                        optionChange: function () {
                            if (scope.gmap) {
                                var mapTypeIds = gm.mapTypeIds.call(gm, config().provider.variety.basic, config().provider.variety.satellite, config().provider.variety.terrain);
                                scope.gmap.setOptions({
                                    mapTypeId: mapTypeIds[0],
                                    mapTypeControl: (mapTypeIds.length > 1),
                                    mapTypeControlOptions: {
                                        style: config().provider.variety.selector.type,
                                        mapTypeIds: mapTypeIds,
                                        position: config().provider.variety.selector.position
                                    },
                                    fullScreenControl: config().provider.fullscreen.enable,
                                    fullscreenControlOptions: config().provider.fullscreen.position,
                                    scaleControl: config().provider.scale.enable,
                                    scaleControlOptions: {
                                        position: config().provider.scale.position
                                    },
                                    streetViewControl: config().provider.streetView.enable,
                                    streetViewControlOptions: {
                                        position: config().provider.streetView.position
                                    },
                                    zoomControl: config().provider.zoomControl.enable,
                                    zoomControlOptions: {
                                        position: config().provider.zoomControl.position
                                    }
                                });
                            }
                        },
                        searchChange: function () {
                            if (typeof config().search.enable == 'string') {
                                config().search.enable = parseInt(config().search.enable);
                            }
                            if (config().search.enable != 0) {
                                if (scope.gautocomplete) {
                                    scope.deleteSearch.call(scope);
                                    setTimeout(function () {
                                        scope.createSearch.call(scope);
                                    }, 1);
                                } else {
                                    scope.createSearch.call(scope);
                                }
                            } else {
                                scope.deleteSearch.call(scope);
                            }
                        },
                        searchCountryChange: function () {
                            if (scope.gautocomplete) {
                                scope.gautocomplete.setComponentRestrictions({ "country": gm.searchCountries(config().search.limit.countries) });
                            }
                        },
                        reload: function () {
                            scope.reloadMap.call(scope);
                        },
                        addEvent: function (id, func, s) {
                            scope.events.push({ id: id, func: func, scope: s });
                        },
                        labelChange: scope.labelChange,
                        destroy: scope.destroy
                    });
                },
                labelChange: function () {
                    if (scope.gmap) {
                        if (scope.ginfo) {
                            delete scope.ginfo;
                            scope.ginfo = null;
                        }
                        if (store().label && typeof store().label.content == 'string' && store().label.content.trim() != '') {
                            scope.ginfo = new root.google.maps.InfoWindow({
                                content: store().label.content
                            });
                        }
                    }
                },
                destroy: function () {
                    event.cancel(id);
                    if (scope.loadMapWait) {
                        clearTimeout(scope.loadMapWait);
                        scope.loadMapWait = null;
                    }
                    if (scope.superWaiter) {
                        clearInterval(scope.superWaiter);
                        scope.superWaiter = null;
                    }
                    if (root.google && root.google.maps && root.google.maps.event) {
                        angular.forEach(scope.gevents, function (gevent) {
                            root.google.maps.event.removeListener(gevent);
                        });
                    }
                    delete scope.gevents;
                    delete scope.gmap;
                    delete scope.gmarker;
                    scope.deleteSearch.call(scope);
                },
                reloadMap: function () {
                    scope.destroy();
                    if (scope.div) {
                        var div = document.getElementById(scope.div);
                        var counter = 100;      //  Put in place incase of horrible errors

                        var timer = setInterval(function () {
                            if (--counter < 0) {
                                clearInterval(timer);
                                scope.loadMap.call(scope);
                            }
                            try
                            {
                                var child = div.firstChild;
                                if (child) {
                                    div.removeChild(child);
                                } else {
                                    counter = 0;
                                }
                            }
                            catch (oh) {
                                counter = 0;
                            }
                        }, 1);
                    } else {
                        scope.loadMap.call(scope);
                    }
                },
                parse: function (text) {
                    if (typeof text !== 'string') {
                        return false;
                    }
                    var args = text.trim().split(',');
                    if (args.length < 2) {
                        return false;
                    }
                    var lat = parseFloat(args[0].substring(0, 10));
                    if (isNaN(lat) || lat > 90 || lat < -90) {
                        return false;
                    }
                    var lng = parseFloat(args[1].substring(0, 10));
                    if (isNaN(lng) || lng > 180 || lng < -180) {
                        return false;
                    }
                    return {
                        latitude: lat,
                        longitude: lng
                    };
                },
                isElementInViewport: function (el) {
                    var rect = el.getBoundingClientRect();
                    return (
                        (rect.top <= (window.innerHeight || document.documentElement.clientHeight)) && ((rect.top + rect.height) >= 0) &&
                        (rect.left <= (window.innerWidth || document.documentElement.clientWidth)) && ((rect.left + rect.width) >= 0)
                    );
                },
                toString: function (datum, precision) {
                    function encodelatlng(latlng) {
                        return Number(latlng).toFixed(precision).replace(/\.?0+$/, '');
                    }
                    return encodelatlng(datum.latitude) + ',' + encodelatlng(datum.longitude);
                },
                setDatum: function () {
                    var datum = scope.toString.call(scope, vm().position.datum, vm().position.precision);
                    if (typeof datum !== 'boolean') {
                        store().position.datum = datum;
                        vm().position.datumText = datum;
                        vm().position.datumStyle = {};
                    } else {
                        vm().position.datumStyle = { 'color': 'red' };
                    }
                },
                setMarker: function (quick) {
                    if (scope.gmap && scope.gmarker) {
                        var latlng = {
                            lat: vm().position.datum.latitude,
                            lng: vm().position.datum.longitude
                        };
                        //  Gmaps sommetimes complains about perfectly valid coordinates, when this happens we just move on
                        scope.gmarker.setPosition(latlng);
                        if (quick) {
                            scope.gmap.setCenter(latlng);
                        } else {
                            scope.gmap.panTo(latlng);
                        }
                    }
                },
                loadMapWait: null,
                div: null,
                divoldsize: 0,
                visible: false,
                divwait: 0,
                superWaiter: null,
                loadMap: function () {
                    if (scope.loadMapWait == null) {
                        scope.loadMapWait = setTimeout(function () {
                            //gm.originalConsole.warn(id + ': Loading map');
                            scope.initValues();
                            scope.loadMapWait = null;
                            vm().status = {
                                loading: true,
                                reload: true
                            };
                            vm().showMap = false;
                            scope.gmap = null;
                            scope.gmarker = null;
                            scope.gautocomplete = null;
                            scope.gevents = [],
                            scope.div = null;
                            scope.divoldsize = 0;
                            scope.divwait = gm.timeout / gm.poll;
                            event.register(id, 'gmaperror', scope, this, function (s) {
                                //gm.originalConsole.warn(id + ': Map error');
                                vm().status = {
                                    failed: true,
                                    reload: true
                                };
                                event.cancel(id);
                                clearInterval(scope.superWaiter);
                                scope.superWaiter = null;
                                updateView();
                            });
                            event.register(id, 'gmapkilled', scope, this, function (s) {
                                //gm.originalConsole.warn(id + ': Map killed');
                                vm().status = {
                                    reload: true
                                };
                                event.cancel(id);
                                clearInterval(scope.superWaiter);
                                scope.superWaiter = null;
                                updateView();
                            });
                            event.register(id, 'gmaprefresh', scope, this, function (s) {
                                //gm.originalConsole.warn(id + ': Map refresh(). div=' + scope.div + ', gmap=' + scope.gmap);
                                if (!root.google) {
                                    scope.reloadMap.call(scope);
                                } else if (scope.div == null) {
                                    vm().status = {
                                        success: true,
                                        reload: true
                                    };
                                    vm().provider.version = String(root.google.maps.version);
                                    vm().provider.versionMajor = parseInt(String(root.google.maps.version).substring(2, 4));
                                    if (typeof config().search.enable == 'string') {
                                        config().search.enable = parseInt(config().search.enable);
                                    }

                                    //  Check that we have loaded with the right setting for us
                                    if (gm.apiKey != config().provider.apiKey ||
                                        gm.coordinateSystem != store().position.id ||
                                        gm.forceHttps != config().provider.forceHttps ||
                                        gm.language != config().provider.language) {
                                        vm().status = {
                                            duplicate: true,
                                            reload: true
                                        };
                                        event.cancel(id);
                                        updateView();
                                        return;
                                    }
                                    scope.ignoreEvents = 0;
                                    scope.div = 'terratype_' + id + '_googlemapv3_map';
                                    vm().showMap = true;
                                    updateView();
                                } else {
                                    var element = document.getElementById(scope.div);
                                    if (element == null) {
                                        if (scope.gmap == null && scope.divwait != 0) {
                                            scope.divwait--;
                                        } else {
                                            //gm.originalConsole.log(id + ' ' + scope.div + ' not present');
                                            scope.destroy.call(scope);
                                        }
                                    } else if (scope.gmap == null) {
                                        scope.gevents = [];
                                        var latlng = {
                                            lat: vm().position.datum.latitude,
                                            lng: vm().position.datum.longitude
                                        };
                                        var mapTypeIds = gm.mapTypeIds.call(gm, config().provider.variety.basic, config().provider.variety.satellite, config().provider.variety.terrain);
                                        config().provider.styles = gm.style.call(gm, config().provider.predefineStyling, config().provider.showRoads,
                                                config().provider.showLandmarks, config().provider.showLabels);
                                        scope.gmap = new root.google.maps.Map(element, {
                                            disableDefaultUI: false,
                                            scrollwheel: false,
                                            panControl: false,      //   Has been depricated
                                            center: latlng,
                                            zoom: store().zoom,
                                            draggable: config().draggable,
                                            fullScreenControl: config().provider.fullscreen.enable,
                                            fullscreenControlOptions: config().provider.fullscreen.position,
                                            styles: config().provider.styles,
                                            mapTypeId: mapTypeIds[0],
                                            mapTypeControl: (mapTypeIds.length > 1),
                                            mapTypeControlOptions: {
                                                style: config().provider.variety.selector.type,
                                                mapTypeIds: mapTypeIds,
                                                position: config().provider.variety.selector.position
                                            },
                                            scaleControl: config().provider.scale.enable,
                                            scaleControlOptions: {
                                                position: config().provider.scale.position
                                            },
                                            streetViewControl: config().provider.streetView.enable,
                                            streetViewControlOptions: {
                                                position: config().provider.streetView.position
                                            },
                                            zoomControl: config().provider.zoomControl.enable,
                                            zoomControlOptions: {
                                                position: config().provider.zoomControl.position
                                            }
                                        });
                                        scope.gevents.push(root.google.maps.event.addListener(scope.gmap, 'zoom_changed', function () {
                                            scope.eventZoom.call(scope);
                                        }));
                                        root.google.maps.event.addListenerOnce(scope.gmap, 'tilesloaded', function () {
                                            scope.eventRefresh.call(scope);
                                        });
                                        scope.gevents.push(root.google.maps.event.addListener(scope.gmap, 'resize', function () {
                                            scope.eventCheckRefresh.call(scope);
                                        }));
                                        scope.gmarker = new root.google.maps.Marker({
                                            map: scope.gmap,
                                            position: latlng,
                                            id: 'terratype_' + id + '_marker',
                                            draggable: true,
                                            icon: gm.icon.call(gm, config().icon)
                                        })
                                        scope.ginfo = null;
                                        if (store().label && typeof store().label.content == 'string' && store().label.content.trim() != '') {
                                            scope.ginfo = new root.google.maps.InfoWindow({
                                                content: store().label.content
                                            });
                                        }
                                        scope.gevents.push(scope.gmarker.addListener('click', function () {
                                            if (scope.ignoreEvents > 0) {
                                                return;
                                            }
                                            if (scope.callEvent('icon-click')) {
                                                if (scope.ginfo) {
                                                    scope.ginfo.open(scope.gmap, scope.gmarker);
                                                }
                                            }
                                        }));
                                        scope.gevents.push(root.google.maps.event.addListener(scope.gmap, 'click', function () {
                                            if (scope.ignoreEvents > 0) {
                                                return;
                                            }
                                            if (scope.ginfo) {
                                                scope.ginfo.close();
                                            }
                                            scope.callEvent('map-click');
                                        }));

                                        scope.gevents.push(root.google.maps.event.addListener(scope.gmarker, 'dragend', function (marker) {
                                            scope.eventDrag.call(scope, marker);
                                        }));

                                        if (config().search.enable != 0) {
                                            scope.createSearch.call(scope);
                                        }
                                        scope.setDatum.call(scope);

                                        updateView();
                                    } else {
                                        var newValue = element.parentElement.offsetTop + element.parentElement.offsetWidth;
                                        var newSize = element.clientHeight * element.clientWidth;
                                        var show = vm().showMap;
                                        var visible = show && scope.isElementInViewport(element);
                                        if (newValue != 0 && show == false) {
                                            vm().showMap = true;
                                            updateView();
                                            setTimeout(function () {
                                                if (document.getElementById(scope.div).hasChildNodes() == false) {
                                                    scope.reloadMap.call(scope);
                                                } else {
                                                    scope.eventRefresh.call(scope);
                                                }
                                            }, 1);
                                        } else if (newValue == 0 && show == true) {
                                            vm().showMap = false;
                                            scope.visible = false;
                                        }
                                        else if (visible == true && scope.divoldsize != 0 && newSize != 0 && scope.divoldsize != newSize) {
                                            scope.eventRefresh.call(scope);
                                            scope.visible = true;
                                        } else if (visible == true && scope.visible == false) {
                                            scope.eventRefresh.call(scope);
                                            scope.visible = true;
                                        } else if (visible == false && scope.visible == true) {
                                            scope.visible = false;
                                        }
                                        scope.divoldsize = newSize;
                                    }
                                }
                            });

                            if (gm.status == gm.subsystemUninitiated) {
                                gm.createSubsystem(config().provider.version, config().provider.apiKey, config().provider.forceHttps,
                                    store().position.id, config().provider.language);
                            }

                            //  Check that the subsystem is working
                            count = 0;
                            scope.superWaiter = setInterval(function () {
                                function go() {
                                    //  Error with subsystem, it isn't loading, only thing we can do is try again
                                    if (count > 5) {
                                        vm().status = {
                                            error: true,
                                            reload: true
                                        };
                                        clearInterval(scope.superWaiter);
                                        scope.superWaiter = null;
                                        updateView();
                                    }

                                    gm.createSubsystem(config().provider.version, config().provider.apiKey, config().provider.forceHttps,
                                        store().position.id, config().provider.language);
                                    count++;
                                }

                                if (gm.status != gm.subsystemCooloff && gm.status != gm.subsystemCompleted) {
                                    go();
                                } else if (scope.div == null || scope.gmap == null || document.getElementById(scope.div) == null || document.getElementById(scope.div).hasChildNodes() == false) {
                                    gm.destroySubsystem();
                                    setTimeout(go, 1);
                                } else {
                                    vm().status = {
                                        success: true
                                    };
                                    clearInterval(scope.superWaiter);
                                    scope.superWaiter = null;
                                    updateView();
                                }
                            }, gm.timeout);
                        }, gm.poll);
                    }
                },
                eventZoom: function () {
                    if (scope.ignoreEvents > 0) {
                        return;
                    }
                    //gm.originalConsole.warn(id + ': eventZoom()');
                    store().zoom = scope.gmap.getZoom();
                },
                eventRefresh: function () {
                    if (scope.gmap == null || scope.ignoreEvents > 0) {
                        return;
                    }
                    //gm.originalConsole.warn(id + ': eventRefresh()');
                    scope.ignoreEvents++;
                    scope.gmap.setZoom(store().zoom);
                    scope.setMarker.call(scope, true);
                    root.google.maps.event.trigger(scope.gmap, 'resize');
                    scope.ignoreEvents--;
                },
                eventCheckRefresh: function () {
                    if (scope.gmap.getBounds() && !scope.gmap.getBounds().contains(scope.gmarker.getPosition())) {
                        scope.eventRefresh.call(scope);
                    }
                },
                eventDrag: function (marker) {
                    if (scope.ignoreEvents > 0) {
                        return;
                    }
                    //gm.originalConsole.warn(id + ': eventDrag()');
                    scope.ignoreEvents++;
                    vm().position.datum = {
                        latitude: gm.round(marker.latLng.lat(), vm().position.precision),
                        longitude: gm.round(marker.latLng.lng(), vm().position.precision)
                    };
                    scope.setMarker.call(scope);
                    scope.setDatum.call(scope);
                    updateView();
                    scope.ignoreEvents--;
                },
                eventLookup: function (place) {
                    if (scope.ignoreEvents > 0 || !place.geometry) {
                        return;
                    }
                    //gm.originalConsole.warn(id + ': eventDrag()');
                    scope.ignoreEvents++;
                    store().lookup = place.formatted_address;
                    vm().position.datum = {
                        latitude: gm.round(place.geometry.location.lat(), vm().position.precision),
                        longitude: gm.round(place.geometry.location.lng(), vm().position.precision)
                    };
                    scope.setMarker.call(scope);
                    scope.setDatum.call(scope);
                    updateView();
                    scope.ignoreEvents--;
                },
                searchListerners: [],
                createSearch: function () {
                    var lookup = document.getElementById('terratype_' + id + '_googlemapv3_lookup');
                    var results = document.getElementById('terratype_' + id + '_googlemapv3_lookup_results');
                    if (!lookup || !results) {
                        return;
                    }

                    gm.createSearch(id, lookup, results, {
                        autocomplete: config().search.enable == 2
                    }, scope.gmap, function (handler) {
                        if (handler == null) {
                            vm().status.searchFailed = true;
                        } else {
                            scope.gautocomplete = handler;
                            if (config().search && config().search.limit &&
                                config().search.limit.countries && config().search.limit.countries.length != 0) {
                                scope.gautocomplete.setComponentRestrictions({ "country": gm.searchCountries(config().search.limit.countries) });
                            }
                            scope.searchListerners.push(root.google.maps.event.addListener(scope.gautocomplete, 'place_changed', function () {
                                scope.eventLookup.call(scope, scope.gautocomplete.getPlace());
                            }));
                            scope.searchListerners.push(root.google.maps.event.addListener(scope.gautocomplete, 'places_changed', function () {
                                var places = scope.gautocomplete.getPlaces();
                                if (places && places.length > 0) {
                                    scope.eventLookup.call(scope, places[0]);
                                }
                            }));

                            $(lookup).on('keypress keydown', function (e) {
                                $(lookup).css('color', '');
                                if (e.which == 13) {
                                    var service = new root.google.maps.places.PlacesService(scope.gmap);
                                    service.textSearch({ query: lookup.value }, function (results, status) {
                                        if (results && status == 'OK' && results.length > 0) {
                                            scope.eventLookup.call(scope, results[0]);
                                        } else {
                                            $(lookup).css('color', '#ff0000');
                                        }
                                    });
                                    return e.preventDefault();
                                }
                            });
                        }
                    });
                },
                deleteSearch: function () {
                    angular.forEach(scope.searchListerners, function (value, index) {
                        root.google.maps.event.removeListener(value);
                    });
                    scope.searchListerners = [];
                    if (scope.gautocomplete) {
                        root.google.maps.event.clearInstanceListeners(scope.gautocomplete);
                        scope.gautocomplete = null;
                    }
                    $('#terratype_' + id + '_googlemapv3_lookup').off('keypress keydown');
                    gm.deleteSearch(id);
                },
                callEvent: function (id) {
                    for (var i = 0; i != scope.events.length; i++) {
                        if (scope.events[i].id == id) {
                            scope.events[i].func.call(scope.events[i].scope);
                        }
                    }

                }
            }
            scope.init(done);
        }
    }

    root.terratype.providers[identifier] = provider;
}(window));
