<template>
    <div>
        <ul class="nav nav-pills margin-b-md">
            <li v-bind:class="{active: visibleTab == 'Version' }"><a href="#" @click.prevent="visibleTab = 'Version'">Version Info</a></li>
            <li v-bind:class="{active: visibleTab == 'Diagnostics' }"><a href="#" @click.prevent="visibleTab = 'Diagnostics'">Diagnostics</a></li>
        </ul>

        <div class="tabContent">

            <div id="version-info" v-if="visibleTab == 'Version'">

                <p>
                    <strong>Rock Version: </strong>
                    <span v-html="options.RockVersion"></span>
                </p>

                <p>
                    <strong>Client Culture Setting: </strong>
                    {{ options.ClientCulture }}
                </p>

                <div class="actions margin-t-xl">
                    <a class="btn btn-primary" @click.prevent="clearCache()" title="Flushes all cached items from the Rock cache (e.g. Pages, BlockTypes, Blocks, Attributes, etc.">Clear Cache</a>
                    <a class="btn btn-link js-restart" Text="Restart Rock" @click.prevent="restartRock()" ToolTip="Restarts the Application.">Restart Rock</a>
                </div>

                <div v-if="this.versionMessage != ''" class="margin-t-md alert" v-bind:class="[versionMessageType]" v-html="this.versionMessage">

                </div>
            </div>

            <div id="diagnostics-tab" v-if="visibleTab == 'Diagnostics'">

                <h4>Details</h4>
                <p>
                    <strong>Database:</strong><br />
                    <span v-html="options.Database"></span>
                </p>

                <p>
                    <strong>System Date Time:</strong><br />
                    <span v-html="options.SystemDateTime"></span>
                </p>

                <p>
                    <strong>Rock Time:</strong><br />
                    <span v-html="options.RockTime"></span>
                </p>

                <p>
                    <strong>Process Start Time:</strong><br />
                    <span v-html="options.ProcessStartTime"></span>
                </p>

                <p>
                    <strong>Executing Location:</strong><br />
                    <span v-html="options.ExecLocation"></span>
                </p>

                <p>
                    <strong>Last Migration(s):</strong><br />
                    <span v-html="options.LastMigrations"></span>
                </p>

                <div>
                    <h4>Transaction Queue</h4>
                    <span v-html="options.TransactionQueue"></span>
                </div>

                <div>
                    <h4>Routes</h4>
                    <p><a id="show-routes" href="#" @click.prevent="showRoutes = !showRoutes">Show Routes</a></p>
                    <div id="routes" v-if="showRoutes">
                        <p>
                            <span v-html="options.Routes"></span>
                        </p>
                    </div>
                </div>


                <h4>Cache</h4>
                <div id="cache-details">
                    <span v-html="options.CacheOverview"></span>
                </div>

                <span v-html="options.FalseCacheHits"></span>

                <p>
                    <a href="#" @click.prevent="showCacheStatistics = !showCacheStatistics">Show Cache Statistics</a>
                </p>
                <div v-if="showCacheStatistics">
                    <p v-html="options.CacheObjects"></p>
                </div>

                <div>
                    <h4>Threads</h4>
                    <span v-html="options.Threads"></span>
                </div>

                <a class="btn btn-action margin-t-lg" @click="dumpDiagnostics()" title="Generates a diagnostics file for sharing with others.">
                    <i class="fa fa-download"></i> Download Diagnostics File
                </a>

            </div>
        </div>
    </div>
</template>
<script lang="ts">
    declare var bootbox;
    declare var template: String;
    import Vue from 'Scripts/vue';
    import axios from 'Scripts/axios';

    export default class App {
        constructor(id, options) {
            var vm = new Vue({
                el: '#' + id,
                template: template,
                data: {
                    visibleTab: 'Version',
                    versionMessage: '',
                    versionMessageType: 'alert-success',
                    showRoutes: false,
                    showCacheStatistics: false,
                    clearingCache: false,
                    options: options
                },
                methods: {
                    dumpDiagnostics: function () {
                    },
                    clearCache: function () {
                        this.clearingCache = true;

                        let _this = this;
                        axios.post(`/api/BlockAction/${this.options.BlockId}/ClearCache`, {})
                            .then(function (response) {
                                _this.clearingCache = false;
                                _this.versionMessageType = response.data.Error === true ? 'alert-warning' : 'alert-success';
                                _this.versionMessage = response.data.Messages.join('<br />');
                            })
                            .catch(function (error) {
                                _this.clearingCache = false;
                                _this.versionMessageType = 'error';
                                _this.versionMessage = error;
                            });
                        
                    },
                    restartRock: function () {
                        bootbox.alert("The Rock application will be restarted. You will need to reload this page to continue.");
                    }
                }
            });
        }
    }
</script>