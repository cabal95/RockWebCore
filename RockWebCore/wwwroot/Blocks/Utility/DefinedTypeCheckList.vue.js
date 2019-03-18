import itemComponent from 'wwwroot/Blocks/Utility/check-list-item.component.vue'

var vm = new Vue({
    el: '#$$id$$',
    data: $$data$$,
    components: {
        'item': itemComponent
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
