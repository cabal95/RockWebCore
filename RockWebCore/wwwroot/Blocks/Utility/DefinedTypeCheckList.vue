<template>
    <div>
        <h3>{{ Title }}</h3>
        <div v-html="Description"></div>

        <item v-for="item in Items" v-bind:key="item.Id" v-bind:item="item" v-bind:hide-selected="true" v-on:set-state="onSetState"></item>
    </div>
</template>
<script lang="ts">
    import { CheckListItem } from './check-list-item.component';
    import Vue from 'Scripts/vue';
    declare var template: string;

    export default function (id, options) {
        return new Vue({
            template: template,
            el: '#' + id,
            data: options,
            components: {
                'item': CheckListItem
            },
            methods: {
                onSetState: function (item) {
                    var data = {
                        Parameters: {
                            id: item.Id,
                            state: item.Selected
                        }
                    };

                    var options = {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(data)
                    };

                    fetch('/api/BlockAction/' + this.BlockId + '/SetSelected', options)
                        .then(function (res) {
                            if (res.ok !== true) {
                                item.Selected = !data.Parameters.state;
                            }
                        });
                }
            }
        });
    }
</script>
