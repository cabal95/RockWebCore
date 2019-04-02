import { CheckListItem } from './check-list-item.component';
import * as Vue from 'Scripts/vue';

export default class App {
    constructor(id, options) {
        var vm = new Vue({
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
}