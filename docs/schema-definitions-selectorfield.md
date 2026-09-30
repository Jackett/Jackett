# SelectorField Schema

```txt
Cardigann#/definitions/SelectorField
```

Selector applied to the download page to get the download URL, hash or title.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## SelectorField Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

# SelectorField Properties

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                                             |
| :-------------------------------------- | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [selector](#selector)                   | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-selector.md "Cardigann#/definitions/SelectorField/properties/selector")                     |
| [attribute](#attribute)                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-attribute.md "Cardigann#/definitions/SelectorField/properties/attribute")                   |
| [usebeforeresponse](#usebeforeresponse) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-use-before-response.md "Cardigann#/definitions/SelectorField/properties/usebeforeresponse") |
| [filters](#filters)                     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-filters-selectorfield.md "Cardigann#/definitions/SelectorField/properties/filters")         |

## selector

Selector for the value.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-selectorfield-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-selector.md "Cardigann#/definitions/SelectorField/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-selectorfield-properties-selector.md))

## attribute

Take the value of this attribute (for example, href) instead of the element text.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-selectorfield-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-attribute.md "Cardigann#/definitions/SelectorField/properties/attribute")

### attribute Type

`string` ([Attribute](schema-definitions-selectorfield-properties-attribute.md))

## usebeforeresponse

Apply the selector to the page returned by the before request instead of the search result download link. Default: false.

`usebeforeresponse`

* is optional

* Type: `boolean` ([Use before response](schema-definitions-selectorfield-properties-use-before-response.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-use-before-response.md "Cardigann#/definitions/SelectorField/properties/usebeforeresponse")

### usebeforeresponse Type

`boolean` ([Use before response](schema-definitions-selectorfield-properties-use-before-response.md))

## filters

Filters applied to the extracted value, in order.

`filters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-filters-selectorfield.md "Cardigann#/definitions/SelectorField/properties/filters")

### filters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))
