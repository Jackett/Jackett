# Login Schema

```txt
Cardigann#/definitions/Login
```

How Jackett logs in to the tracker. Omit for sites that need no login.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## Login Type

`object` ([Login](schema-definitions-login.md))

# Login Properties

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                             |
| :-------------------------------------- | :-------- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------- |
| [method](#method)                       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-method.md "Cardigann#/definitions/Login/properties/method")                         |
| [cookies](#cookies)                     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-cookies.md "Cardigann#/definitions/Login/properties/cookies")                       |
| [path](#path)                           | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-path.md "Cardigann#/definitions/Login/properties/path")                             |
| [submitpath](#submitpath)               | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-submit-path.md "Cardigann#/definitions/Login/properties/submitpath")                |
| [form](#form)                           | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-form.md "Cardigann#/definitions/Login/properties/form")                             |
| [captcha](#captcha)                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock.md "Cardigann#/definitions/Login/properties/captcha")                                   |
| [inputs](#inputs)                       | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-inputs-login.md "Cardigann#/definitions/Login/properties/inputs")                   |
| [selectors](#selectors)                 | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-selectors.md "Cardigann#/definitions/Login/properties/selectors")                   |
| [selectorinputs](#selectorinputs)       | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-selector-inputs.md "Cardigann#/definitions/Login/properties/selectorinputs")        |
| [getselectorinputs](#getselectorinputs) | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-get-selector-inputs.md "Cardigann#/definitions/Login/properties/getselectorinputs") |
| [error](#error)                         | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-error-login.md "Cardigann#/definitions/Login/properties/error")                     |
| [test](#test)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock.md "Cardigann#/definitions/Login/properties/test")                                     |
| [headers](#headers)                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-headers-login.md "Cardigann#/definitions/Login/properties/headers")                 |

## method

post: send inputs as an HTTP POST. get: same, using HTTP GET. form: fetch path, extract the HTML form (handles CSRF tokens and captchas), then POST it. cookie: use the configured cookie. oneurl: legacy, added for the removed beyond-hd-oneurl indexer; no definition uses it.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-login-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-method.md "Cardigann#/definitions/Login/properties/method")

### method Type

`string` ([Method](schema-definitions-login-properties-method.md))

### method Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value      | Explanation |
| :--------- | :---------- |
| `"form"`   |             |
| `"post"`   |             |
| `"cookie"` |             |
| `"get"`    |             |
| `"oneurl"` |             |

## cookies

Cookies sent with the post login request.

`cookies`

* is optional

* Type: `string[]` ([Cookie](schema-definitions-login-properties-cookies-cookie.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-cookies.md "Cardigann#/definitions/Login/properties/cookies")

### cookies Type

`string[]` ([Cookie](schema-definitions-login-properties-cookies-cookie.md))

## path

Login target. For post/get it is the request URL. For form it is the page containing the login form.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-login-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-path.md "Cardigann#/definitions/Login/properties/path")

### path Type

`string` ([Path](schema-definitions-login-properties-path.md))

## submitpath

Form login only. URL to POST to, when it differs from the form's action attribute.

`submitpath`

* is optional

* Type: `string` ([Submit path](schema-definitions-login-properties-submit-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-submit-path.md "Cardigann#/definitions/Login/properties/submitpath")

### submitpath Type

`string` ([Submit path](schema-definitions-login-properties-submit-path.md))

## form

Form login only. Selector for the HTML form element. Default: form.

`form`

* is optional

* Type: `string` ([Form](schema-definitions-login-properties-form.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-form.md "Cardigann#/definitions/Login/properties/form")

### form Type

`string` ([Form](schema-definitions-login-properties-form.md))

## captcha

Captcha handling for form logins. Google ReCaptcha and simplecaptcha are detected automatically and need no captcha block.

`captcha`

* is optional

* Type: `object` ([CaptchaBlock](schema-definitions-captchablock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock.md "Cardigann#/definitions/Login/properties/captcha")

### captcha Type

`object` ([CaptchaBlock](schema-definitions-captchablock.md))

## inputs

Login parameters, e.g. username: "{{ .Config.username }}". Fixed values are allowed.

`inputs`

* is optional

* Type: `object` ([Inputs (Login)](schema-definitions-login-properties-inputs-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-inputs-login.md "Cardigann#/definitions/Login/properties/inputs")

### inputs Type

`object` ([Inputs (Login)](schema-definitions-login-properties-inputs-login.md))

## selectors

Form login only. If true, the keys in inputs are treated as CSS selectors. Only needed for dynamic input names (very rare).

`selectors`

* is optional

* Type: `boolean` ([Selectors](schema-definitions-login-properties-selectors.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-selectors.md "Cardigann#/definitions/Login/properties/selectors")

### selectors Type

`boolean` ([Selectors](schema-definitions-login-properties-selectors.md))

## selectorinputs

Form login only. Input values taken from the login page with selectors, e.g. a CSRF token hidden in JavaScript.

`selectorinputs`

* is optional

* Type: `object` ([Selector inputs](schema-definitions-login-properties-selector-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-selector-inputs.md "Cardigann#/definitions/Login/properties/selectorinputs")

### selectorinputs Type

`object` ([Selector inputs](schema-definitions-login-properties-selector-inputs.md))

## getselectorinputs

Form login only. Like selectorinputs, but sent in the query string instead of the POST body.

`getselectorinputs`

* is optional

* Type: `object` ([GET selector inputs](schema-definitions-login-properties-get-selector-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-get-selector-inputs.md "Cardigann#/definitions/Login/properties/getselectorinputs")

### getselectorinputs Type

`object` ([GET selector inputs](schema-definitions-login-properties-get-selector-inputs.md))

## error

Selectors checked on the login response. If one matches, the login failed and the matched text is shown as the error.

`error`

* is optional

* Type: `object[]` ([ErrorBlock](schema-definitions-errorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-error-login.md "Cardigann#/definitions/Login/properties/error")

### error Type

`object[]` ([ErrorBlock](schema-definitions-errorblock.md))

## test

Page requested after login to confirm the session is valid. A redirect, or no selector match, means the login failed. Also used during searches to detect an expired session and re-login.

`test`

* is optional

* Type: `object` ([PageTestBlock](schema-definitions-pagetestblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock.md "Cardigann#/definitions/Login/properties/test")

### test Type

`object` ([PageTestBlock](schema-definitions-pagetestblock.md))

## headers

Extra HTTP headers sent with login requests. If omitted, search headers are used.

`headers`

* is optional

* Type: `object` ([Headers (Login)](schema-definitions-login-properties-headers-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-headers-login.md "Cardigann#/definitions/Login/properties/headers")

### headers Type

`object` ([Headers (Login)](schema-definitions-login-properties-headers-login.md))
