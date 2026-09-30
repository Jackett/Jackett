# SettingsField Schema

```txt
Cardigann#/definitions/SettingsField
```

One config option shown on the indexer config page. Its value is available to templates as .Config.<name>.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## SettingsField Type

`object` ([SettingsField](schema-definitions-settingsfield.md))

# SettingsField Properties

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                         |
| :-------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------- |
| [name](#name)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-name.md "Cardigann#/definitions/SettingsField/properties/name")         |
| [label](#label)       | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-label.md "Cardigann#/definitions/SettingsField/properties/label")       |
| [type](#type)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-type.md "Cardigann#/definitions/SettingsField/properties/type")         |
| [default](#default)   | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-default.md "Cardigann#/definitions/SettingsField/properties/default")   |
| [options](#options)   | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-options.md "Cardigann#/definitions/SettingsField/properties/options")   |
| [defaults](#defaults) | `array`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-defaults.md "Cardigann#/definitions/SettingsField/properties/defaults") |

## name

Internal variable name. Referenced in templates as .Config.<name>.

`name`

* is required

* Type: `string` ([Name](schema-definitions-settingsfield-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-name.md "Cardigann#/definitions/SettingsField/properties/name")

### name Type

`string` ([Name](schema-definitions-settingsfield-properties-name.md))

## label

Display name shown on the config page.

`label`

* is optional

* Type: `string` ([Label](schema-definitions-settingsfield-properties-label.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-label.md "Cardigann#/definitions/SettingsField/properties/label")

### label Type

`string` ([Label](schema-definitions-settingsfield-properties-label.md))

## type

Input type. password masks the input. The info\_\* types show a predefined info box (category_8000, cookie, FlareSolverr, useragent) and need no label.

`type`

* is required

* Type: `string` ([Type](schema-definitions-settingsfield-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-type.md "Cardigann#/definitions/SettingsField/properties/type")

### type Type

`string` ([Type](schema-definitions-settingsfield-properties-type.md))

### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value                  | Explanation |
| :--------------------- | :---------- |
| `"info"`               |             |
| `"text"`               |             |
| `"password"`           |             |
| `"checkbox"`           |             |
| `"select"`             |             |
| `"multi-select"`       |             |
| `"info_category_8000"` |             |
| `"info_cookie"`        |             |
| `"info_flaresolverr"`  |             |
| `"info_useragent"`     |             |

## default

Default value. For checkbox, true ticks it (unticked by default). For info, the text to display. For select, the default option key.

`default`

* is optional

* Type: merged type ([Default](schema-definitions-settingsfield-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-default.md "Cardigann#/definitions/SettingsField/properties/default")

### default Type

merged type ([Default](schema-definitions-settingsfield-properties-default.md))

one (and only one) of

* [Default as string](schema-definitions-settingsfield-properties-default-oneof-default-as-string.md "check type definition")

* [Default as integer](schema-definitions-settingsfield-properties-default-oneof-default-as-integer.md "check type definition")

* [Default as boolean](schema-definitions-settingsfield-properties-default-oneof-default-as-boolean.md "check type definition")

## options

Select options as key: display-name pairs.

`options`

* is optional

* Type: `object` ([Options](schema-definitions-settingsfield-properties-options.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-options.md "Cardigann#/definitions/SettingsField/properties/options")

### options Type

`object` ([Options](schema-definitions-settingsfield-properties-options.md))

## defaults

Default selected option keys for a multi-select.

`defaults`

* is optional

* Type: `string[]` ([Option key](schema-definitions-settingsfield-properties-defaults-option-key.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-defaults.md "Cardigann#/definitions/SettingsField/properties/defaults")

### defaults Type

`string[]` ([Option key](schema-definitions-settingsfield-properties-defaults-option-key.md))
