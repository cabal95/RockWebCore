<template>
<div>
    <fieldset>
        <legend>Login</legend>

        <div class="row">
            <div class="col-sm-12">
                {{ PromptMessage }}
                {{ InvalidPersonTokenText }}

                <rock-text-box title="Username" v-model:value="Username"></rock-text-box>

                <rock-text-box title="Password" type="password" v-model:value="Password"></rock-text-box>

                <div class="checkbox">
                    <label title="">
                        <input type="checkbox" v-model:checked="KeepLoggedIn" />
                        <span class="label-text">Keep me logged in</span>
                    </label>
                </div>

                <a class="btn btn-primary" v-bind:class="{'disabled': ButtonDisabled}" @click="onClick">Log In</a>

                <a class="btn btn-action" v-bind:href="NewAccountLink" v-if="ShowNewAccount">{{ NewAccountText }}</a>

                <a class="btn btn-link">Forgot Account</a>

                <div class="alert alert-warning block-message margin-t-md" v-if="this.Message !== ''" v-html="this.Message"></div>
            </div>
        </div>
    </fieldset>
</div>
</template>
<script lang="ts">
    import Vue from 'Scripts/vue';
    import { RockTextBox } from 'Scripts/RockControls';
    declare var template: string;

    export default function (id, options) {
        var vm = new Vue({
            el: '#' + id,
            template: template,
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
</script>
