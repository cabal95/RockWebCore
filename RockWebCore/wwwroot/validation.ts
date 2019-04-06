export interface IValidator {
    validationGroup: string;
    validationMessages: Array<string>;
    validate(): boolean;
}

export class ValidatorGroup implements IValidator {
    validationGroup: string;
    validators: Array<IValidator>;
    validationMessages: Array<string>;

    constructor(validationGroup: string) {
        this.validationGroup = validationGroup;
        this.validators = new Array<IValidator>();
    }

    validate(): boolean {
        let isValid: boolean = true;
        this.validationMessages = new Array<string>();

        for (let c of this.validators) {
            if (c.validate() === false) {
                isValid = false;
                this.validationMessages = this.validationMessages.concat(c.validationMessages);
            }
        }

        return isValid;
    }

    addValidator(validator: IValidator): void {
        this.validators.push(validator);
    }

    addValidatorsForControl(control: IHasValidators) {
        let validators: Array<IValidator> = control.getValidators();
        for (let validator of validators) {
            if (validator.validationGroup === this.validationGroup) {
                this.addValidator(validator);
            }
        }
    }
}

export class TextBoxRequiredValidator implements IValidator {
    control: TextBox;
    validationGroup: string;
    validationMessages: Array<string>;

    constructor(control: TextBox, validationGroup: string) {
        this.control = control;
        this.validationGroup = validationGroup;
    }

    validate(): boolean {
        let isValid: boolean = true;

        this.validationMessages = new Array<string>();

        if (this.control.getValue() == '') {
            this.validationMessages.push(this.control.title + ' value is required.');
            isValid = false;
        }

        return isValid;
    }
}

export interface IControl {

}

export interface IHasValidators {
    getValidators(): Array<IValidator>;
}

export class TextBox implements IHasValidators, IControl {
    validationGroup: string;
    title: string;
    value: string;

    getValidators(): Array<IValidator> {
        return new Array<IValidator>(new TextBoxRequiredValidator(this, this.validationGroup));
    }

    getValue(): string {
        return this.value;
    }
}

export class ControlGroup implements IHasValidators {
    controls: Array<IControl>;

    constructor() {
        this.controls = new Array<IControl>();
    }

    getValidators(): Array<IValidator> {
        let validators: Array<IValidator> = new Array<IValidator>();

        for (let c of this.controls) {
            let cany: any = c;
            if (typeof cany.getValidators === 'function') {
                validators = validators.concat(cany.getValidators());
            }
        }

        return validators;
    }
}
