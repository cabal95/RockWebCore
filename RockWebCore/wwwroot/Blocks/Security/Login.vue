<template>
<div>
    <fieldset>
        <legend>Login</legend>

        <div class="row">
            <div class="col-sm-12">
                {{ PromptMessage }}
                {{ InvalidPersonTokenText }}

                <rock.textbox ref="username" title="Username" v-model:value="Username"></rock.textbox>

                <rock.textbox ref="password" title="Password" type="password" v-model:value="Password"></rock.textbox>

                <div class="checkbox">
                    <label title="">
                        <input type="checkbox" v-model:checked="KeepLoggedIn" />
                        <span class="label-text">Keep me logged in</span>
                    </label>
                </div>

                <a class="btn btn-primary" v-bind:class="{'disabled': ButtonDisabled}" @click="onClick">
                    <span v-if="ButtonDisabled"><i class="fa fa-sync fa-spin"></i> Logging In...</span>
                    <span v-if="!ButtonDisabled">Log In</span>
                </a>

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
    import { RockTextBox, ValidationManager } from 'Scripts/RockControls';
    declare var template: string;

    export default function (id, options) {
        var vm = new Vue({
            el: '#' + id,
            template: template,
            data: options,
            components: {
                'rock.textbox': RockTextBox
            },
            methods: {
                onClick: function () {
                    let validation = new ValidationManager();
                    let success = validation.validateControl(this);
                    if (!success) {
                        var messages = validation.validationMessages.map(function (msg) {
                            return `<li>${msg}</li>`;
                        }).join('');

                        this.Message = `<p>Please correct the following:</p><ul>${messages}</ul>`;

                        return;
                    }

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

                            if (res.ok !== true) {
                                _this.ButtonDisabled = false;
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
