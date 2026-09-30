# ResponseBlock Schema

```txt
Cardigann#/definitions/ResponseBlock
```

Response type for non-HTML search results. Omit for HTML, which is the default.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## ResponseBlock Type

`object` ([ResponseBlock](schema-definitions-responseblock.md))

# ResponseBlock Properties

| Property                              | Type     | Required | Nullable       | Defined by                                                                                                                                                           |
| :------------------------------------ | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [type](#type)                         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock-properties-type.md "Cardigann#/definitions/ResponseBlock/properties/type")                           |
| [noResultsMessage](#noresultsmessage) | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock-properties-no-results-message.md "Cardigann#/definitions/ResponseBlock/properties/noResultsMessage") |

## type

json or xml.

`type`

* is required

* Type: `string` ([Type](schema-definitions-responseblock-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock-properties-type.md "Cardigann#/definitions/ResponseBlock/properties/type")

### type Type

`string` ([Type](schema-definitions-responseblock-properties-type.md))

### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value    | Explanation |
| :------- | :---------- |
| `"json"` |             |
| `"xml"`  |             |

## noResultsMessage

If the response contains this text, or is empty and this is an empty string, return zero results instead of an error. For sites that do not return an empty object or a zero count.

`noResultsMessage`

* is optional

* Type: `string` ([No results message](schema-definitions-responseblock-properties-no-results-message.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock-properties-no-results-message.md "Cardigann#/definitions/ResponseBlock/properties/noResultsMessage")

### noResultsMessage Type

`string` ([No results message](schema-definitions-responseblock-properties-no-results-message.md))
