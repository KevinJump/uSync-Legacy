/// <binding ProjectOpened='default' />
const { watch, src, dest } = require('gulp');

const sources = [
    './Jumoo.uSync.BackOffice/App_Plugins',
    './Jumoo.uSync.Content/App_Plugins',
    './Jumoo.uSync.Complete/App_Plugins',
    './Jumoo.uSync.Snapshots/App_Plugins'
];

const destination = './Jumoo.uSync.Site/App_Plugins';

function copy(path, base) {
    return src(path, { base: base })
        .pipe(dest(destination));
}

function time() {
    return '[' + new Date().toISOString().slice(11, -5) + ']';
}

exports.default = function () {

    sources.forEach(function (source) {

        var searchPath = source + '/**/*';

        watch(searchPath, { ignoreInitial: false })
            .on('change', function (path, stats) {
                console.log(time(), path, 'changed');
                copy(path, source);
            })
            .on('add', function (path, stats) {
                console.log(time(), path, 'added');
                copy(path, source);
            });
    });
};


