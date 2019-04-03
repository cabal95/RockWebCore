import * as Vue from 'Scripts/vue';

export default class App {
    constructor(id, options) {
        var vm = new Vue({
            el: '#' + id,
            data: {
                visibleTab: 'Version',
                showRoutes: false,
                showCacheStatistics: false,
                options: options
            },
            methods: {
                dumpDiagnostics: function () {
                },
                clearCache: function () {
                },
                restartRock: function () {
                    bootbox.alert("The Rock application will be restarted. You will need to reload this page to continue.");
                }
            }
        });
    }
}
