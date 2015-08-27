declare module 'aurelia-validation/validation/utilities' {
	export class Utilities {
	    constructor();
	    static getValue(val: any): any;
	    static isEmptyValue(val: any): boolean;
	}

}
declare module 'aurelia-validation/validation/validation-locale' {
	export class ValidationLocale {
	    static Repository: any;
	    defaults: any;
	    currentLocale: any;
	    constructor(defaults: any, data: any);
	    getValueFor(identifier: any, category: any): any;
	    setting(settingIdentifier: any): any;
	    translate(translationIdentifier: any, newValue: any, threshold: any): any;
	}

}
declare module 'aurelia-validation/validation/validate-custom-attribute-view-strategy' {
	export class ValidateCustomAttributeViewStrategyBase {
	    bindingPathAttributes: any;
	    constructor();
	    getValidationProperty(validation: any, element: any): any;
	    prepareElement(validationProperty: any, element: any): void;
	    updateElement(validationProperty: any, element: any): void;
	}
	export class TWBootstrapViewStrategy extends ValidateCustomAttributeViewStrategyBase {
	    appendMessageToInput: any;
	    appendMessageToLabel: any;
	    helpBlockClass: any;
	    constructor(appendMessageToInput: any, appendMessageToLabel: any, helpBlockClass: any);
	    searchFormGroup(currentElement: any, currentDepth: any): any;
	    findLabels(formGroup: any, inputId: any): any[];
	    findLabelsRecursively(currentElement: any, inputId: any, currentLabels: any, currentDepth: any): void;
	    appendMessageToElement(element: any, validationProperty: any): void;
	    appendUIVisuals(validationProperty: any, currentElement: any): void;
	    prepareElement(validationProperty: any, element: any): void;
	    updateElement(validationProperty: any, element: any): void;
	}
	export class ValidateCustomAttributeViewStrategy {
	}

}
declare module 'aurelia-validation/validation/validation-config' {
	export class ValidationConfigDefaults {
	    static _defaults: any;
	    static defaults: any;
	}
	export class ValidationConfig {
	    static uniqueListenerId: any;
	    innerConfig: any;
	    values: any;
	    changedHandlers: any;
	    constructor(innerConfig?: any);
	    getValue(identifier: any): any;
	    setValue(identifier: any, value: any): ValidationConfig;
	    onLocaleChanged(callback: any): any;
	    getDebounceTimeout(): any;
	    useDebounceTimeout(value: any): ValidationConfig;
	    getDependencies(): any;
	    computedFrom(dependencies: any): ValidationConfig;
	    useLocale(localeIdentifier: any): ValidationConfig;
	    locale(): any;
	    useViewStrategy(viewStrategy: any): ValidationConfig;
	    getViewStrategy(): any;
	}

}
declare module 'aurelia-validation/validation/validation-result' {
	export class ValidationResult {
	    isValid: any;
	    properties: any;
	    constructor();
	    addProperty(name: any): any;
	    checkValidity(): void;
	    clear(): void;
	}
	export class ValidationResultProperty {
	    group: any;
	    onValidateCallbacks: any;
	    isValid: any;
	    isDirty: any;
	    message: any;
	    failingRule: any;
	    latestValue: any;
	    constructor(group: any);
	    clear(): void;
	    onValidate(onValidateCallback: any): void;
	    notifyObserversOfChange(): void;
	    setValidity(validationResponse: any, shouldBeDirty: any): void;
	}

}
declare module 'aurelia-validation/validation/validation-rules' {
	export class ValidationRule {
	    onValidate: any;
	    threshold: any;
	    message: any;
	    errorMessage: any;
	    ruleName: any;
	    constructor(threshold: any, onValidate: any, message?: any);
	    withMessage(message: any): void;
	    explain(): any;
	    setResult(result: any, currentValue: any, locale: any): boolean;
	    /**
	     * Validation rules: return a promise that fulfills and resolves to true/false
	     */
	    validate(currentValue: any, locale: any): Promise<boolean>;
	}
	export class EmailValidationRule extends ValidationRule {
	    isFQDN: any;
	    emailUserUtf8Regex: any;
	    constructor();
	}
	export class MinimumLengthValidationRule extends ValidationRule {
	    constructor(minimumLength: any);
	}
	export class MaximumLengthValidationRule extends ValidationRule {
	    constructor(maximumLength: any);
	}
	export class BetweenLengthValidationRule extends ValidationRule {
	    constructor(minimumLength: any, maximumLength: any);
	}
	export class CustomFunctionValidationRule extends ValidationRule {
	    constructor(customFunction: any, threshold: any);
	}
	export class NumericValidationRule extends ValidationRule {
	    constructor();
	}
	export class RegexValidationRule extends ValidationRule {
	    constructor(regex: any);
	}
	export class ContainsOnlyValidationRule extends RegexValidationRule {
	    constructor(regex: any);
	}
	export class MinimumValueValidationRule extends ValidationRule {
	    constructor(minimumValue: any);
	}
	export class MinimumInclusiveValueValidationRule extends ValidationRule {
	    constructor(minimumValue: any);
	}
	export class MaximumValueValidationRule extends ValidationRule {
	    constructor(maximumValue: any);
	}
	export class MaximumInclusiveValueValidationRule extends ValidationRule {
	    constructor(maximumValue: any);
	}
	export class BetweenValueValidationRule extends ValidationRule {
	    constructor(minimumValue: any, maximumValue: any);
	}
	export class DigitValidationRule extends ValidationRule {
	    digitRegex: any;
	    constructor();
	}
	export class AlphaNumericValidationRule extends ValidationRule {
	    alphaNumericRegex: any;
	    constructor();
	}
	export class AlphaValidationRule extends ValidationRule {
	    alphaRegex: any;
	    constructor();
	}
	export class AlphaOrWhitespaceValidationRule extends ValidationRule {
	    alphaNumericRegex: any;
	    constructor();
	}
	export class AlphaNumericOrWhitespaceValidationRule extends ValidationRule {
	    alphaNumericRegex: any;
	    constructor();
	}
	export class MediumPasswordValidationRule extends ValidationRule {
	    constructor(minimumComplexityLevel: any);
	}
	export class StrongPasswordValidationRule extends MediumPasswordValidationRule {
	    constructor();
	}
	export class EqualityValidationRuleBase extends ValidationRule {
	    constructor(otherValue: any, equality: any, otherValueLabel?: any);
	}
	export class EqualityValidationRule extends EqualityValidationRuleBase {
	    constructor(otherValue: any);
	}
	export class EqualityWithOtherLabelValidationRule extends EqualityValidationRuleBase {
	    constructor(otherValue: any, otherLabel: any);
	}
	export class InEqualityValidationRule extends EqualityValidationRuleBase {
	    constructor(otherValue: any);
	}
	export class InEqualityWithOtherLabelValidationRule extends EqualityValidationRuleBase {
	    constructor(otherValue: any, otherLabel: any);
	}
	export class InCollectionValidationRule extends ValidationRule {
	    constructor(collection: any);
	}

}
declare module 'aurelia-validation/validation/validation-rules-collection' {
	export class ValidationRulesCollection {
	    isRequired: any;
	    validationRules: any;
	    validationCollections: any;
	    isRequiredMessage: any;
	    constructor();
	    /**
	     * Returns a promise that fulfils and resolves to simple result status object.
	     */
	    validate(newValue: any, locale: any): Promise<{
	        isValid: boolean;
	        message: any;
	        failingRule: string;
	        latestValue: any;
	    }>;
	    addValidationRule(validationRule: any): void;
	    addValidationRuleCollection(validationRulesCollection: any): void;
	    isNotEmpty(): void;
	    withMessage(message: any): void;
	}
	export class SwitchCaseValidationRulesCollection {
	    conditionExpression: any;
	    innerCollections: any;
	    defaultCollection: any;
	    caseLabel: any;
	    defaultCaseLabel: any;
	    constructor(conditionExpression: any);
	    case(caseLabel: any): void;
	    default(): void;
	    getCurrentCollection(caseLabel: any, createIfNotExists?: boolean): any;
	    validate(newValue: any, locale: any): any;
	    addValidationRule(validationRule: any): void;
	    addValidationRuleCollection(validationRulesCollection: any): void;
	    isNotEmpty(): void;
	    withMessage(message: any): void;
	}

}
declare module 'aurelia-validation/validation/path-observer' {
	export class PathObserver {
	    observerLocator: any;
	    path: any;
	    subject: any;
	    observers: any;
	    callbacks: any;
	    constructor(observerLocator: any, subject: any, path: any);
	    observeParts(propertyName?: any): void;
	    observePart(part: any): void;
	    getObserver(): any;
	    getValue(): any;
	    subscribe(callback: any): any;
	}

}
declare module 'aurelia-validation/validation/debouncer' {
	export class Debouncer {
	    currentFunction: any;
	    debounceTimeout: any;
	    constructor(debounceTimeout: any);
	    debounce(func: any): void;
	}

}
declare module 'aurelia-validation/validation/validation-property' {
	export class ValidationProperty {
	    propertyResult: any;
	    propertyName: any;
	    validationGroup: any;
	    collectionOfValidationRules: any;
	    config: any;
	    latestValue: any;
	    observer: any;
	    debouncer: any;
	    dependencyObservers: any;
	    constructor(observerLocator: any, propertyName: any, validationGroup: any, propertyResult: any, config: any);
	    addValidationRule(validationRule: any): void;
	    validateCurrentValue(forceDirty: any, forceExecution?: any): any;
	    clear(): void;
	    /**
	     * returns a promise that fulfils and resolves to true/false
	     */
	    validate(newValue: any, shouldBeDirty: any, forceExecution?: any): any;
	}

}
declare module 'aurelia-validation/validation/validation-group-builder' {
	export class ValidationGroupBuilder {
	    observerLocator: any;
	    validationRuleCollections: any;
	    validationGroup: any;
	    constructor(observerLocator: any, validationGroup: any);
	    ensure(propertyName: any, configurationCallback: any): any;
	    isNotEmpty(): any;
	    isGreaterThan(minimumValue: any): any;
	    isGreaterThanOrEqualTo(minimumValue: any): any;
	    isBetween(minimumValue: any, maximumValue: any): any;
	    isIn(collection: any): any;
	    isLessThan(maximumValue: any): any;
	    isLessThanOrEqualTo(maximumValue: any): any;
	    isEqualTo(otherValue: any, otherValueLabel: any): any;
	    isNotEqualTo(otherValue: any, otherValueLabel: any): any;
	    isEmail(): any;
	    hasMinLength(minimumValue: any): any;
	    hasMaxLength(maximumValue: any): any;
	    hasLengthBetween(minimumValue: any, maximumValue: any): any;
	    isNumber(): any;
	    containsOnlyDigits(): any;
	    containsOnlyAlpha(): any;
	    containsOnlyAlphaOrWhitespace(): any;
	    containsOnlyAlphanumerics(): any;
	    containsOnlyAlphanumericsOrWhitespace(): any;
	    isStrongPassword(minimumComplexityLevel: any): any;
	    containsOnly(regex: any): any;
	    matches(regex: any): any;
	    passes(customFunction: any, threshold: any): any;
	    passesRule(validationRule: any): any;
	    checkLast(): void;
	    withMessage(message: any): any;
	    if(conditionExpression: any): any;
	    else(): any;
	    endIf(): any;
	    switch(conditionExpression: any): any;
	    case(caseLabel: any): any;
	    default(): any;
	    endSwitch(): any;
	}

}
declare module 'aurelia-validation/validation/validation-group' {
	/**
	 * Encapsulates validation rules and their current validation state for a given subject
	 * @class ValidationGroup
	 * @constructor
	 */
	export class ValidationGroup {
	    result: any;
	    subject: any;
	    validationProperties: any;
	    config: any;
	    builder: any;
	    onValidateCallbacks: any;
	    onPropertyValidationCallbacks: any;
	    isValidating: any;
	    onDestroy: any;
	    /**
	     * Instantiates a new {ValidationGroup}
	     * @param subject The subject to evaluate
	     * @param observerLocator The observerLocator used to monitor changes on the subject
	     * @param config The configuration
	     */
	    constructor(subject: any, observerLocator: any, config: any);
	    destroy(): void;
	    clear(): void;
	    onBreezeEntity(): void;
	    /**
	     * Causes complete re-evaluation: gets the latest value, marks the property as 'dirty' (unless false is passed), runs validation rules asynchronously and updates this.result
	     * @returns {Promise} A promise that fulfils when valid, rejects when invalid.
	     */
	    validate(forceDirty?: boolean, forceExecution?: boolean): Promise<any>;
	    onValidate(validationFunction: any, validationFunctionFailedCallback?: any): ValidationGroup;
	    onPropertyValidate(validationFunction: any): ValidationGroup;
	    /**
	     * Adds a validation property for the specified path
	     * @param {String} bindingPath the path of the property/field, for example 'firstName' or 'address.muncipality.zipCode'
	     * @param configCallback a configuration callback
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    ensure(bindingPath: any, configCallback?: any): ValidationGroup;
	    /**
	     * Adds a validation rule that checks a value for being 'isNotEmpty', 'required'
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isNotEmpty(): any;
	    /**
	     * Adds a validation rule that checks a value for being greater than or equal to a threshold
	     * @param minimumValue the threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isGreaterThanOrEqualTo(minimumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being greater than a threshold
	     * @param minimumValue the threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isGreaterThan(minimumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being greater than or equal to a threshold, and less than or equal to another threshold
	     * @param minimumValue The minimum threshold
	     * @param maximumValue The isLessThanOrEqualTo threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isBetween(minimumValue: any, maximumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being less than a threshold
	     * @param maximumValue The threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isLessThanOrEqualTo(maximumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being less than or equal to a threshold
	     * @param maximumValue The threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isLessThan(maximumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being equal to a threshold
	     * @param otherValue The threshold
	     * @param otherValueLabel Optional: a label to use in the validation message
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isEqualTo(otherValue: any, otherValueLabel: any): any;
	    /**
	     * Adds a validation rule that checks a value for not being equal to a threshold
	     * @param otherValue The threshold
	     * @param otherValueLabel Optional: a label to use in the validation message
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isNotEqualTo(otherValue: any, otherValueLabel: any): any;
	    /**
	     * Adds a validation rule that checks a value for being a valid isEmail address
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isEmail(): any;
	    /**
	     * Adds a validation rule that checks a value for being equal to at least one other value in a particular collection
	     * @param collection The threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isIn(collection: any): any;
	    /**
	     * Adds a validation rule that checks a value for having a length greater than or equal to a specified threshold
	     * @param minimumValue The threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    hasMinLength(minimumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for having a length less than a specified threshold
	     * @param maximumValue The threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    hasMaxLength(maximumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for having a length greater than or equal to a specified threshold and less than another threshold
	     * @param minimumValue The min threshold
	     * @param maximumValue The max threshold
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    hasLengthBetween(minimumValue: any, maximumValue: any): any;
	    /**
	     * Adds a validation rule that checks a value for being numeric, this includes formatted numbers like '-3,600.25'
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isNumber(): any;
	    /**
	     * Adds a validation rule that checks a value for being strictly numeric, this excludes formatted numbers like '-3,600.25'
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    containsOnlyDigits(): any;
	    containsOnly(regex: any): any;
	    containsOnlyAlpha(): any;
	    containsOnlyAlphaOrWhitespace(): any;
	    containsOnlyLetters(): any;
	    containsOnlyLettersOrWhitespace(): any;
	    /**
	     * Adds a validation rule that checks a value for only containing alphanumerical characters
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    containsOnlyAlphanumerics(): any;
	    /**
	     * Adds a validation rule that checks a value for only containing alphanumerical characters or whitespace
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    containsOnlyAlphanumericsOrWhitespace(): any;
	    /**
	     * Adds a validation rule that checks a value for being a strong password. A strong password contains at least the specified of the following groups: lowercase characters, uppercase characters, digits and special characters.
	     * @param minimumComplexityLevel {Number} Optionally, specifiy the number of groups to match. Default is 4.
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    isStrongPassword(minimumComplexityLevel: any): any;
	    /**
	     * Adds a validation rule that checks a value for matching a particular regex
	     * @param regex the regex to match
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    matches(regex: any): any;
	    /**
	     * Adds a validation rule that checks a value for passing a custom function
	     * @param customFunction {Function} The custom function that needs to pass, that takes two arguments: newValue (the value currently being evaluated) and optionally: threshold, and returns true/false.
	     * @param threshold {Object} An optional threshold that will be passed to the customFunction
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    passes(customFunction: any, threshold?: any): any;
	    /**
	     * Adds the {ValidationRule}
	     * @param validationRule {ValudationRule} The rule that needs to pass
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    passesRule(validationRule: any): any;
	    /**
	     * Specifies that the next validation rules only need to be evaluated when the specified conditionExpression is true
	     * @param conditionExpression {Function} a function that returns true of false.
	     * @param threshold {Object} an optional treshold object that is passed to the conditionExpression
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    if(conditionExpression: any, threshold: any): any;
	    /**
	     * Specifies that the next validation rules only need to be evaluated when the previously specified conditionExpression is false.
	     * See: if(conditionExpression, threshold)
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    else(): any;
	    /**
	     * Specifies that the execution of next validation rules no longer depend on the the previously specified conditionExpression.
	     * See: if(conditionExpression, threshold)
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    endIf(): any;
	    /**
	     * Specifies that the next validation rules only need to be evaluated when they are preceded by a case that matches the conditionExpression
	     * @param conditionExpression {Function} a function that returns a case label to execute. This is optional, when omitted the case label will be matched using the underlying property's value
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    switch(conditionExpression: any): any;
	    /**
	     * Specifies that the next validation rules only need to be evaluated when the caseLabel matches the value returned by a preceding switch statement
	     * See: switch(conditionExpression)
	     * @param caseLabel {Object} the case label
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    case(caseLabel: any): any;
	    /**
	     * Specifies that the next validation rules only need to be evaluated when not other caseLabel matches the value returned by a preceding switch statement
	     * See: switch(conditionExpression)
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    default(): any;
	    /**
	     * Specifies that the execution of next validation rules no longer depend on the the previously specified conditionExpression.
	     * See: switch(conditionExpression)
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    endSwitch(): any;
	    /**
	     * Specifies that the execution of the previous validation rule should use the specified error message if it fails
	     * @param message either a static string or a function that takes two arguments: newValue (the value that has been evaluated) and threshold.
	     * @returns {ValidationGroup} returns this ValidationGroup, to enable fluent API
	     */
	    withMessage(message: any): any;
	}

}
declare module 'aurelia-validation/validation/validation' {
	import { ValidationGroup } from 'aurelia-validation/validation/validation-group';
	/**
	 * A lightweight validation plugin
	 * @class Validation
	 * @constructor
	 */
	export class Validation {
	    static defaults: any;
	    observerLocator: any;
	    config: any;
	    /**
	     * Instantiates a new {Validation}
	     * @param observerLocator the observerLocator used to observer properties
	     * @param validationConfig the configuration
	     */
	    constructor(observerLocator: any, validationConfig: any);
	    /**
	     * Returns a new validation group on the subject
	     * @param subject The subject to validate
	     * @returns {ValidationGroup} A ValidationGroup that encapsulates the validation rules and current validation state for this subject
	     */
	    on(subject: any, configCallback: any): ValidationGroup;
	    onBreezeEntity(breezeEntity: any, configCallback: any): ValidationGroup;
	}

}
declare module 'aurelia-validation/validation/validate-custom-attribute' {
	export class ValidateCustomAttribute {
	    element: any;
	    processedValidation: any;
	    viewStrategy: any;
	    value: any;
	    constructor(element: any);
	    valueChanged(newValue: any): void;
	    subscribeChangedHandlers(currentElement: any): void;
	    detached(): void;
	    attached(): void;
	}

}
declare module 'aurelia-validation/validation/decorators' {
	export function ensure(setupStep: any): (target: any, propertyName: any) => void;

}
declare module 'aurelia-validation/index' {
	export { Utilities } from 'aurelia-validation/validation/utilities';
	export { ValidationConfig } from 'aurelia-validation/validation/validation-config';
	export { ValidationLocale } from 'aurelia-validation/validation/validation-locale';
	export * from 'aurelia-validation/validation/validation-result';
	export * from 'aurelia-validation/validation/validation-rules';
	export { Validation } from 'aurelia-validation/validation/validation';
	export { ValidateCustomAttribute } from 'aurelia-validation/validation/validate-custom-attribute';
	export { ValidateCustomAttributeViewStrategy } from 'aurelia-validation/validation/validate-custom-attribute-view-strategy';
	export { ValidateCustomAttributeViewStrategyBase } from 'aurelia-validation/validation/validate-custom-attribute-view-strategy';
	export { ensure } from 'aurelia-validation/validation/decorators';
	export function configure(aurelia: any, configCallback: any): any;

}
declare module 'aurelia-validation' {
	export * from 'aurelia-validation/index';
}
