# Selector Schema

```txt
Cardigann#/definitions/SelectorBlock/properties/selector
```

CSS selector for HTML, or path for JSON/XML. :has(), :not() and :contains() are supported. In JSON, $ refers to the root object and a .. prefix reads from the row instead of the rows attribute subset.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## selector Type

`string` ([Selector](schema-definitions-selectorblock-properties-selector.md))
