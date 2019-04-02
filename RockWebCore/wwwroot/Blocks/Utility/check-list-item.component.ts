export const CheckListItem = {
    props: ['item', 'hideSelected'],
    template: `<div v-if="!hideSelected || !item.Selected">
    <div class="panel panel-default checklist-item">
        <header class="panel-heading clearfix">
            <input type="checkbox" v-bind:checked="item.Selected" v-on:input="onSelected($event)">
            <label v-on:click.stop.prevent="detailsVisible = !detailsVisible">
                <strong>{{ item.Value }}</strong>
            </label>
            <a class="btn btn-link btn-xs pull-right checklist-desc-toggle" v-on:click.stop.prevent="detailsVisible = !detailsVisible">
                <i class="fa fa-chevron-down"></i>
            </a>
        </header>
        <div class="checklist-description panel-body" v-if="detailsVisible" v-html="item.Description">
        </div>
    </div>
</div>
`,
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
