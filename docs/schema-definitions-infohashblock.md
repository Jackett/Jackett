# InfoHashBlock Schema

```txt
Cardigann#/definitions/InfoHashBlock
```

Builds a magnet URI from an infohash and title. Public and semi-private indexers only: private sites usually require their own tracker and have DHT disabled.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## InfoHashBlock Type

`object` ([InfoHashBlock](schema-definitions-infohashblock.md))

# InfoHashBlock Properties

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                                             |
| :-------------------------------------- | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [hash](#hash)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/hash")                                             |
| [title](#title)                         | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/title")                                            |
| [usebeforeresponse](#usebeforeresponse) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-infohashblock-properties-use-before-response.md "Cardigann#/definitions/InfoHashBlock/properties/usebeforeresponse") |

## hash

Selector applied to the download page to get the download URL, hash or title.

`hash`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/hash")

### hash Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

## title

Selector applied to the download page to get the download URL, hash or title.

`title`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/title")

### title Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

## usebeforeresponse

Take hash and title from the page returned by the before request instead of the search result download link. Default: false.

`usebeforeresponse`

* is optional

* Type: `boolean` ([Use before response](schema-definitions-infohashblock-properties-use-before-response.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-infohashblock-properties-use-before-response.md "Cardigann#/definitions/InfoHashBlock/properties/usebeforeresponse")

### usebeforeresponse Type

`boolean` ([Use before response](schema-definitions-infohashblock-properties-use-before-response.md))
