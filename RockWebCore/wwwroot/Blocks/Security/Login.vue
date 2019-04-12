<template>
<form v-on:submit.prevent="login">
    <input type="submit" style="display: none;" />
    <fieldset>
        <legend>Login</legend>

        <div class="row">
            <div class="col-sm-12">
                <rock.validationsummary header-text="Please correct the following:" class="alert alert-warning"></rock.validationsummary>

                {{ PromptMessage }}
                {{ InvalidPersonTokenText }}

                <rock.textbox ref="username" title="Username" v-model:value="Username" :is-required="true"></rock.textbox>

                <rock.textbox ref="password" title="Password" type="password" v-model:value="Password" :is-required="true"></rock.textbox>

                <div class="checkbox">
                    <label title="">
                        <input type="checkbox" v-model:checked="KeepLoggedIn" />
                        <span class="label-text">Keep me logged in</span>
                    </label>
                </div>

                <a class="btn btn-primary" v-bind:class="{'disabled': ButtonDisabled}" @click.prevent="login">
                    <span v-if="ButtonDisabled"><i class="fa fa-sync fa-spin"></i> Logging In...</span>
                    <span v-if="!ButtonDisabled">Log In</span>
                </a>

                <a class="btn btn-action" v-bind:href="NewAccountLink" v-if="ShowNewAccount">{{ NewAccountText }}</a>

                <a class="btn btn-link">Forgot Account</a>

                <div class="alert alert-warning block-message margin-t-md" v-if="this.Message !== ''" v-html="this.Message"></div>
            </div>
        </div>
    </fieldset>
</form>
</template>
<script lang="ts">
    import Vue from 'Scripts/vue';
    import { RockTextBox, BlockValidator, RockValidationSummary } from 'Scripts/RockControls';
    import axios from 'Scripts/axios';
    declare var template: string;

    export default function (id, options) {
        var vm = new Vue({
            el: '#' + id,
            template: template,
            data: options,
            components: {
                'rock.textbox': RockTextBox,
                'rock.validationsummary': RockValidationSummary
            },
            methods: {
                login: function () {
                    let validation = new BlockValidator(this);
                    if (!validation.validate()) {
                        return;
                    }

                    this.ButtonDisabled = true;

                    let _this = this;
                    axios.post('/api/Auth/Login', {
                        Username: this.Username,
                        Password: this.Password
                    })
                        .then(function () {
                            window.location = _this.RedirectUrl;
                        })
                        .catch(function (error) {
                            _this.ButtonDisabled = false;
                            _this.Message = _this.NoAccountText;
                        });
                }
            }
        });
    }
</script>
