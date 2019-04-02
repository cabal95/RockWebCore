export const RockTextBox = {
    props: {
        title: {
            type: String
        },
        type: {
            default: 'text',
            type: String
        },
        value: {
            type: String
        }
    },
    template: `
                <div class="form-group rock-text-box">
                    <label class="control-label">{{ title }}</label>
                    <div class="control-wrapper">
                        <input v-bind:type="type" class="form-control" v-bind:value="value" @input="$emit(\'input\', $event.target.value)">
                    </div>
                </div>`,
    data: function () {
        return {
        };
    },
    methods: {
    }
};
