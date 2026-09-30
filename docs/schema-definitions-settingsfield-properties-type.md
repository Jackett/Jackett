# Type Schema

```txt
Cardigann#/definitions/SettingsField/properties/type
```

Input type. password masks the input. The info\_\* types show a predefined info box (category_8000, cookie, FlareSolverr, useragent) and need no label.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## type Type

`string` ([Type](schema-definitions-settingsfield-properties-type.md))

## type Constraints

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
