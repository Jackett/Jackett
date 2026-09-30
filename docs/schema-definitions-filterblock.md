# FilterBlock Schema

```txt
Cardigann#/definitions/FilterBlock
```

Filter applied to an extracted value. See the Filters section of the Definition-format wiki.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## FilterBlock Type

`object` ([FilterBlock](schema-definitions-filterblock.md))

all of

* [dateparse format check](schema-definitions-filterblock-allof-dateparse-format-check.md "check type definition")

# FilterBlock Properties

| Property      | Type     | Required | Nullable       | Defined by                                                                                                                                  |
| :------------ | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------ |
| [name](#name) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-filterblock-properties-name.md "Cardigann#/definitions/FilterBlock/properties/name")      |
| [args](#args) | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-filterblock-properties-arguments.md "Cardigann#/definitions/FilterBlock/properties/args") |

## name

Filter name.

`name`

* is required

* Type: `string` ([Name](schema-definitions-filterblock-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-filterblock-properties-name.md "Cardigann#/definitions/FilterBlock/properties/name")

### name Type

`string` ([Name](schema-definitions-filterblock-properties-name.md))

### name Constraints

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

## args

Filter arguments: a single value or a list, depending on the filter.

`args`

* is optional

* Type: merged type ([Arguments](schema-definitions-filterblock-properties-arguments.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-filterblock-properties-arguments.md "Cardigann#/definitions/FilterBlock/properties/args")

### args Type

merged type ([Arguments](schema-definitions-filterblock-properties-arguments.md))

one (and only one) of

* [Arguments as array (FilterBlock)](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-array-filterblock.md "check type definition")

* [Arguments as string](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-string.md "check type definition")

* [Arguments as integer](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-integer.md "check type definition")
