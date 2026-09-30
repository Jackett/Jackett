# Selectors Schema

```txt
Cardigann#/definitions/DownloadBlock/properties/selectors
```

If set, the search result download URL is fetched and parsed as HTML, and the first selector gives the actual download URL. Later selectors are fallbacks.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## selectors Type

`array` ([Selectors](schema-definitions-downloadblock-properties-selectors.md))
