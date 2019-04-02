import * as Vue from 'Scripts/vue';
import { RockTextBox } from 'Scripts/RockControls';

export default class App {
    constructor(id, options) {
        var vm = new Vue({
            el: '#' + id,
            data: options,
            components: {
                rockTextBox: RockTextBox
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
