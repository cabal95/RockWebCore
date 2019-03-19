<div id="$$id$$" class="login-block">
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