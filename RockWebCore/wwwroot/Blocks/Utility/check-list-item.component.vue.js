(function () {
    return {
        props: ['item', 'hideSelected'],
        template: $$template$$,
        data: function () {
            return {
                detailsVisible: false
            };
        },
        methods: {
            onSelected: function (event) {
                this.item.Selected = event.target.checked;
                this.$emit('set-state', this.item);
            }
        }
    };
})();
