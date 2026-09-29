# Name Schema

```txt
Cardigann#/definitions/FilterBlock/properties/name
```

Filter name.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../out/schema.json "open original schema") |

## name Type

`string` ([Name](schema-definitions-filterblock-properties-name.md))

## name Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value             | Explanation |
| :---------------- | :---------- |
| `"querystring"`   |             |
| `"timeparse"`     |             |
| `"dateparse"`     |             |
| `"regexp"`        |             |
| `"re_replace"`    |             |
| `"split"`         |             |
| `"replace"`       |             |
| `"trim"`          |             |
| `"prepend"`       |             |
| `"append"`        |             |
| `"tolower"`       |             |
| `"toupper"`       |             |
| `"urldecode"`     |             |
| `"urlencode"`     |             |
| `"htmldecode"`    |             |
| `"htmlencode"`    |             |
| `"timeago"`       |             |
| `"reltime"`       |             |
| `"fuzzytime"`     |             |
| `"validfilename"` |             |
| `"diacritics"`    |             |
| `"jsonjoinarray"` |             |
| `"hexdump"`       |             |
| `"strdump"`       |             |
| `"validate"`      |             |
