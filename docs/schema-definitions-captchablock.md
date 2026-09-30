# CaptchaBlock Schema

```txt
Cardigann#/definitions/CaptchaBlock
```

Captcha handling for form logins. Google ReCaptcha and simplecaptcha are detected automatically and need no captcha block.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## CaptchaBlock Type

`object` ([CaptchaBlock](schema-definitions-captchablock.md))

# CaptchaBlock Properties

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                       |
| :-------------------- | :------- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------- |
| [type](#type)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-type.md "Cardigann#/definitions/CaptchaBlock/properties/type")         |
| [selector](#selector) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-selector.md "Cardigann#/definitions/CaptchaBlock/properties/selector") |
| [input](#input)       | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-input.md "Cardigann#/definitions/CaptchaBlock/properties/input")       |

## type

Captcha type: image or text.

`type`

* is required

* Type: `string` ([Type](schema-definitions-captchablock-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-type.md "Cardigann#/definitions/CaptchaBlock/properties/type")

### type Type

`string` ([Type](schema-definitions-captchablock-properties-type.md))

### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value     | Explanation |
| :-------- | :---------- |
| `"image"` |             |
| `"text"`  |             |

## selector

Selector for the captcha HTML element.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-captchablock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-selector.md "Cardigann#/definitions/CaptchaBlock/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-captchablock-properties-selector.md))

## input

Name of the form field that receives the captcha value.

`input`

* is required

* Type: `string` ([Input](schema-definitions-captchablock-properties-input.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-input.md "Cardigann#/definitions/CaptchaBlock/properties/input")

### input Type

`string` ([Input](schema-definitions-captchablock-properties-input.md))
