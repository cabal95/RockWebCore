import * as Vue from 'Scripts/vue';

export default class App {
    constructor(id, options) {
        var rockTextBox = {
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

        var vm = new Vue({
            el: '#' + id,
            data: options,
            components: {
                rockTextBox: rockTextBox
            },
            methods: {
                onClick: function () {
                    this.ButtonDisabled = true;

                    var data = {
                        Username: this.Username,
                        Password: this.Password
                    };

                    var options = {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(data)
                    };

                    var _this = this;
                    fetch('/api/Auth/Login', options)
                        .then(function (res) {
                            console.log('done');
                            _this.ButtonDisabled = false;

                            if (res.ok !== true) {
                                _this.Message = _this.NoAccountText;
                            }
                            else {
                                window.location = _this.RedirectUrl;
                            }
                        });
                }
            }
        });
    }
}
