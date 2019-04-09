import Vue from 'Scripts/vue';
import Component, { createDecorator } from 'Scripts/vue-class-component';
import { Prop } from 'Scripts/vue-property-decorator';


/**
 * Decorator of a computed property.
 * @param  property The name of the property to be computed.
 * @return ComputedPropertyDecorator | void
 */
function Computed(property) {
    return createDecorator(function (componentOptions, handler) {
        componentOptions.computed = componentOptions.computed || {};
        componentOptions.computed[property] = function () {
            return this[handler]();
        };
    });
}


/**
 * Defines the interface that a validator control must */
export interface IValidator {
    /**
     * The group of validation requirements this validator is tied to.
     */
    validationGroup: string;

    /**
     * The error messages, if any. Only populated after validate() is called.
     */
    validationMessages: Array<string>;

    /**
     * Validate the value.
     * @returns True if the value is valid, otherwise False.
     */
    validate(): boolean;
}

/**
 * Provides validation services for a Vue Block.
 */
export class BlockValidator {
    validationGroup: string;
    validationMessages: Array<string> = new Array<string>();
    block: Vue;

    /**
     * Initialize the block validator instance.
     * @param block The Vue block to be validated.
     * @param validationGroup The validation group to be validated.
     */
    constructor(block: Vue, validationGroup = '') {
        this.validationGroup = validationGroup;
        this.block = block;
    }

    /**
     * Validates the block and group that were specified at instance creation.
     * @returns True if the block fields are valid, False otherwise.
     */
    public validate(): boolean {
        this.validationMessages = new Array<string>();

        let success: boolean = this.validateRecursive(this.block);

        this.setValidationSummary(this.block);

        return success;
    }

    /**
     * Validate the specified control and all it's children recursively.
     * @param control The control to be validated.
     * @returns True if all controls are valid, otherwise False.
     */
    private validateRecursive(control: Vue): boolean {
        let success: boolean = true;

        if (control.validationGroup === this.validationGroup && typeof control.validationMessages !== 'undefined' && typeof control.validate === 'function') {
            if (!control.validate()) {
                success = false;
                this.validationMessages = this.validationMessages.concat(control.validationMessages);
            }
        }

        for (var child of control.$children) {
            if (!this.validateRecursive(child)) {
                success = false;
            }
        }

        return success;
    }

    /**
     * Find the first validation summary control that matches our validation group and set it's
     * validation messages to what we have.
     * @param control The control to recursively search.
     * @returns True if the validation messages were set, false otherwise.
     */
    private setValidationSummary(control: Vue): boolean {
        if (control.validationGroup === this.validationGroup && typeof control.headerText !== 'undefined' && typeof control.setValidationMessages === 'function') {
            control.setValidationMessages(this.validationMessages);
            return true;
        }

        for (var child of control.$children) {
            if (this.setValidationSummary(child)) {
                return true;
            }
        }

        return false;
    }
}

/**
 * Provides a simple interface for displaying validation messages.
 */
@Component({
    template: `<div v-if="messageText !== ''">
    <p v-if="headerText !== ''">{{ headerText }}</p>
    <ul v-html="messageText"></ul>
</div>`
})
export class RockValidationSummary extends Vue {
    @Prop({ default: '' }) readonly headerText!: string;
    @Prop({ default: '' }) readonly validationGroup!: string;
    messageText: string = '';

    public setValidationMessages(messages: Array<string>) {
        this.messageText = messages.map(function (v) { return `<li>${v}</li>`; }).join('');
    }
}


/**
 * A field validator that ensures a value has been provided.
 */
@Component({
    template: '<span v-if="false"></span>'
})
export class RequiredFieldValidator extends Vue implements IValidator {
    @Prop({ type: Boolean, default: true }) readonly isRequired!: boolean;
    /**
     * The function that provides the value from the control.
     */
    @Prop(Function) readonly valueProvider!: Function

    /**
     * The group of validation requirements this validator is tied to.
     */
    @Prop({ type: String, default: '' }) readonly validationGroup!: string

    /**
     * The title of the control being validated.
     */
    @Prop(String) readonly title!: string

    /**
     * The default value that must be different from the current value for
     * the control to be considered valid.
     */
    @Prop({ type: String, default: '' }) readonly defaultValue!: string

    @Prop(Object) readonly controlToValidate!: Vue

    /**
     * The error messages, if any. Only populated after validate() is called.
     */
    validationMessages: Array<string> = new Array<string>()

    /**
     * Validate the value.
     * @returns True if the value is valid, otherwise False.
     */
    validate(): boolean {
        this.validationMessages = new Array<string>();
        let valid: boolean = true;

        if (this.isRequired && this.valueProvider && this.valueProvider() == this.defaultValue) {
            this.validationMessages.push(`<code>${this.title}</code> value is required`);
            valid = false;
        }

        if (this.controlToValidate) {
            this.controlToValidate.hasError = !valid;
        }

        return valid;
    }
}

/**
 * Wraps a rock control with a nice title.
 */
@Component({
    template: `
<div :class="cssClass">
    <label class="control-label">
        {{ title }}
        <a v-if="help" class="help" href="#" tabindex="-1" data-toggle="tooltip" data-placement="auto" data-container="body" data-html="true" :title="help">
            <i class="fa fa-info-circle"></i>
        </a>
    </label>
    <div class="control-wrapper">
        <slot></slot>
    </div>
</div>`
})
export class RockControlWrapper extends Vue {
    @Prop(String) readonly title!: string;
    @Prop({ type: Boolean, default: false }) readonly isRequired!: boolean;
    @Prop(String) readonly help!: string;
    @Prop({ default: '' }) readonly formGroupCssClass!: string;
    @Prop({ default: false }) readonly hasError!: boolean;

    @Computed('cssClass')
    getCssClass() {
        let css: string = "form-group";

        if (this.formGroupCssClass !== '') {
            css += ' ' + this.formGroupCssClass;
        }

        if (this.hasError === true) {
            css += ' has-error';
        }

        if (this.isRequired === true) {
            css += ' required';
        }

        return css;
    }
}

/**
 * A styled Text Box.
 */
@Component({
    template: `
<div>
    <rock-control-wrapper :title="title" :is-required="isRequired" :has-error="hasError" :help="help">
        <required-field-validator :control-to-validate="this" :is-required="isRequired" :title="title" :value-provider="getValue" :validation-group="validationGroup"></required-field-validator>
        <input v-bind:type="type" class="form-control" v-bind:value="value" @input="$emit(\'input\', $event.target.value)">
    </rock-control-wrapper>
</div>`,
    components: {
        requiredFieldValidator: RequiredFieldValidator,
        rockControlWrapper: RockControlWrapper
    }
})
export class RockTextBox extends Vue {
    @Prop(String) readonly title: String;
    @Prop({ default: 'text' }) readonly type!: String;
    @Prop(String) readonly value!: String;
    @Prop(String) readonly validationGroup!: string;
    @Prop({ type: Boolean, default: false }) readonly isRequired!: boolean;
    @Prop(String) readonly help!: String;

    hasError: boolean = false;

    /**
     * Get the current value of the text box.
     * @return {String} The text value
     */
    getValue() {
        return this.value;
    }
}
