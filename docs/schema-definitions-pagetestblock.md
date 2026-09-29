# PageTestBlock Schema

```txt
Cardigann#/definitions/PageTestBlock
```

Page requested after login to confirm the session is valid. A redirect, or no selector match, means the login failed. Also used during searches to detect an expired session and re-login.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## PageTestBlock Type

`object` ([PageTestBlock](schema-definitions-pagetestblock.md))

# PageTestBlock Properties

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                         |
| :-------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock-properties-path.md "Cardigann#/definitions/PageTestBlock/properties/path")         |
| [selector](#selector) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock-properties-selector.md "Cardigann#/definitions/PageTestBlock/properties/selector") |

## path

Page to request, typically the home page or the search page.

`path`

* is required

* Type: `string` ([Path](schema-definitions-pagetestblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock-properties-path.md "Cardigann#/definitions/PageTestBlock/properties/path")

### path Type

`string` ([Path](schema-definitions-pagetestblock-properties-path.md))

## selector

Selector that must match on a logged-in page. Required if the site shows a login form instead of redirecting.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-pagetestblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock-properties-selector.md "Cardigann#/definitions/PageTestBlock/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-pagetestblock-properties-selector.md))
