# SearchPathBlock Schema

```txt
Cardigann#/definitions/SearchPathBlock
```

One search request. All matching paths are requested and their results combined.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## SearchPathBlock Type

`object` ([SearchPathBlock](schema-definitions-searchpathblock.md))

# SearchPathBlock Properties

| Property                          | Type      | Required | Nullable       | Defined by                                                                                                                                                                 |
| :-------------------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path)                     | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-path.md "Cardigann#/definitions/SearchPathBlock/properties/path")                             |
| [method](#method)                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-method.md "Cardigann#/definitions/SearchPathBlock/properties/method")                         |
| [followredirect](#followredirect) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-follow-redirect.md "Cardigann#/definitions/SearchPathBlock/properties/followredirect")        |
| [categories](#categories)         | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-categories-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/categories") |
| [inputs](#inputs)                 | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/inputs")         |
| [inheritinputs](#inheritinputs)   | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inherit-inputs.md "Cardigann#/definitions/SearchPathBlock/properties/inheritinputs")          |
| [queryseparator](#queryseparator) | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-query-separator.md "Cardigann#/definitions/SearchPathBlock/properties/queryseparator")        |
| [response](#response)             | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock.md "Cardigann#/definitions/SearchPathBlock/properties/response")                                           |

## path

Search URL path. Templates and conditionals are allowed.

`path`

* is required

* Type: `string` ([Path](schema-definitions-searchpathblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-path.md "Cardigann#/definitions/SearchPathBlock/properties/path")

### path Type

`string` ([Path](schema-definitions-searchpathblock-properties-path.md))

## method

HTTP method: get or post. Templates are allowed. Default: get.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-searchpathblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-method.md "Cardigann#/definitions/SearchPathBlock/properties/method")

### method Type

`string` ([Method](schema-definitions-searchpathblock-properties-method.md))

## followredirect

Follow redirects on search requests. If true, set a selector in login.test. Default: false.

`followredirect`

* is optional

* Type: `boolean` ([Follow redirect](schema-definitions-searchpathblock-properties-follow-redirect.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-follow-redirect.md "Cardigann#/definitions/SearchPathBlock/properties/followredirect")

### followredirect Type

`boolean` ([Follow redirect](schema-definitions-searchpathblock-properties-follow-redirect.md))

## categories

Only use this path if the search includes at least one of these tracker categories. A leading "!" inverts the match.

`categories`

* is optional

* Type: an array of merged types ([Tracker category](schema-definitions-searchpathblock-properties-categories-searchpathblock-tracker-category.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-categories-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/categories")

### categories Type

an array of merged types ([Tracker category](schema-definitions-searchpathblock-properties-categories-searchpathblock-tracker-category.md))

## inputs

Extra HTTP arguments for this path only.

`inputs`

* is optional

* Type: `object` ([Inputs (SearchPathBlock)](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/inputs")

### inputs Type

`object` ([Inputs (SearchPathBlock)](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md))

## inheritinputs

Use the search-level inputs as the base for this path's inputs. Default: true.

`inheritinputs`

* is optional

* Type: `boolean` ([Inherit inputs](schema-definitions-searchpathblock-properties-inherit-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inherit-inputs.md "Cardigann#/definitions/SearchPathBlock/properties/inheritinputs")

### inheritinputs Type

`boolean` ([Inherit inputs](schema-definitions-searchpathblock-properties-inherit-inputs.md))

## queryseparator

Query string separator. Default: &.

`queryseparator`

* is optional

* Type: `string` ([Query separator](schema-definitions-searchpathblock-properties-query-separator.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-query-separator.md "Cardigann#/definitions/SearchPathBlock/properties/queryseparator")

### queryseparator Type

`string` ([Query separator](schema-definitions-searchpathblock-properties-query-separator.md))

## response

Response type for non-HTML search results. Omit for HTML, which is the default.

`response`

* is optional

* Type: `object` ([ResponseBlock](schema-definitions-responseblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock.md "Cardigann#/definitions/SearchPathBlock/properties/response")

### response Type

`object` ([ResponseBlock](schema-definitions-responseblock.md))
