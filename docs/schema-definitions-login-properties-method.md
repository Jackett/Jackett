# Method Schema

```txt
Cardigann#/definitions/Login/properties/method
```

post: send inputs as an HTTP POST. get: same, using HTTP GET. form: fetch path, extract the HTML form (handles CSRF tokens and captchas), then POST it. cookie: use the configured cookie. oneurl: legacy, added for the removed beyond-hd-oneurl indexer; no definition uses it.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## method Type

`string` ([Method](schema-definitions-login-properties-method.md))

## method Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value      | Explanation |
| :--------- | :---------- |
| `"form"`   |             |
| `"post"`   |             |
| `"cookie"` |             |
| `"get"`    |             |
| `"oneurl"` |             |
