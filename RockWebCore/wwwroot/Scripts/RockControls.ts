import Vue from 'Scripts/vue';
import Component from 'Scripts/vue-class-component';
import { Prop } from 'Scripts/vue-property-decorator';

@Component({})
class Parent extends Vue {
    testMethod() {
        console.log('Hello');
    }
}

@Component({ template: '<button @click.prevent="clicked">Click</button>'})
export class RockTestBox extends Parent {
    clicked() {
        console.log('Clicked!');
        this.testMethod();
    }
}


export interface IValidator {
    validationGroup: string;
    validationMessages: Array<string>;
    validate(): boolean;
}

export class ValidationManager {
    validationMessages: Array<string> = new Array<string>();

    validateControl(control: Vue, validationGroup = ''): boolean {
        this.validationMessages = new Array<string>();

        return this.validateRecursiveControl(control, validationGroup);
    }

    private validateRecursiveControl(control: Vue, validationGroup: string): boolean {
        let success: boolean = true;

        if (control.validationGroup === validationGroup && typeof control.validationMessages !== 'undefined' && typeof control.validate === 'function') {
            if (!control.validate()) {
                success = false;
                this.validationMessages = this.validationMessages.concat(control.validationMessages);
            }
        }

        for (var child of control.$children) {
            if (!this.validateRecursiveControl(child, validationGroup)) {
                success = false;
            }
        }

        return success;
    }
}


@Component({
    template: ''
})
export class RequiredFieldValidator extends Vue implements IValidator {
    @Prop(Function) readonly valueProvider!: Function
    @Prop({ default: '' }) readonly validationGroup!: string
    @Prop(String) readonly title!: string

    validationMessages: Array<string> = new Array<string>()

    validate(): boolean {
        this.validationMessages = new Array<string>();

        if (this.valueProvider && this.valueProvider() == '') {
            this.validationMessages.push(`<code>${this.title}</code> value is required`);
            return false;
        }

        return true;
    }
}

/**
 * A styled Text Box.
 */
@Component({
    template: `
<div class="form-group rock-text-box">
    <label class="control-label">{{ title }}</label>
    <div class="control-wrapper">
        <required-field-validator :title="title" :value-provider="getValue" :validation-group="validationGroup"></required-field-validator>
        <input v-bind:type="type" class="form-control" v-bind:value="value" @input="$emit(\'input\', $event.target.value)">
    </div>
</div>`,
    components: {
        requiredFieldValidator: RequiredFieldValidator
    }
})
export class RockTextBox extends Vue {
    @Prop(String) readonly title: String;
    @Prop({ default: 'text' }) readonly type!: String;
    @Prop(String) readonly value!: String;
    @Prop(String) readonly validationGroup!: string;

    /**
     * Get the current value of the text box.
     * @return {String} The text value
     */
    getValue() {
        return this.value;
    }
}
