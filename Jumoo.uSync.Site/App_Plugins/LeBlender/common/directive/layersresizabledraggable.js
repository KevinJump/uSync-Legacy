angular.module("umbraco").
    directive('LeBlenderdraggable', function () {
        return {
            restrict: 'A',
            scope: {
                layer: '=',
                handlerClick: '&ngClick',
                handlerMouseOver: '&ngMouseover',
                handlerMouseLeave: '&ngMouseleave',
                condition: '=',
                aspectratio: '=',
                resize: '=',
                parentwidth: '=',
                parentheight: '=',
            },
            link: function (scope, element, attrs) {
                scope.$watch(function () {
                    return scope.layer;
                }, function (modelValue) {

                    var setPosition = function(position) {
                        scope.layer.dataX = position.left;
                        scope.layer.dataY = position.top;
                        scope.layer.dataXPer = (100 / scope.parentwidth) * scope.layer.dataX;
                        scope.layer.dataYPer = (100 / scope.parentheight) * scope.layer.dataY;
                    }

                    var setSize = function (size) {
                        scope.layer.width = size.width;
                        scope.layer.height = size.height;
                        scope.layer.widthPer = (100 / scope.parentwidth) * size.width;
                        scope.layer.heightPer = (100 / scope.parentheight) * size.height;
                    }

                    element.draggable({
                        snap: false,
                        revert: false,
                        scroll: false,
                        cursor: "move",
                        distance: 10,
                        cancel: ".text",
                        stop: function (event, ui) {
                            setPosition(ui.position);
                        }
                    })

                    if (scope.resize) {
                        element.resizable({
                            aspectRatio: scope.aspectratio,
                            stop: function (event, ui) {
                                setPosition(ui.position);
                                setSize(ui.size);
                            }
                        });
                    }

                    element.css({ 'top': scope.layer.dataY, 'left': scope.layer.dataX, 'width': scope.layer.width + "px", 'height': scope.layer.height + "px" });
                });

            }
        };
    });