# CategoryMapping Schema

```txt
Cardigann#/definitions/CategoryMapping
```

Maps one tracker category to a Newznab/Torznab category.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## CategoryMapping Type

`object` ([CategoryMapping](schema-definitions-categorymapping.md))

# CategoryMapping Properties

| Property            | Type      | Required | Nullable       | Defined by                                                                                                                                                 |
| :------------------ | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [id](#id)           | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-id.md "Cardigann#/definitions/CategoryMapping/properties/id")                 |
| [cat](#cat)         | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-indexercategories.md "Cardigann#/definitions/CategoryMapping/properties/cat") |
| [desc](#desc)       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-description.md "Cardigann#/definitions/CategoryMapping/properties/desc")      |
| [default](#default) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-default.md "Cardigann#/definitions/CategoryMapping/properties/default")       |

## id

Tracker-specific category ID. Can be a number or a string.

`id`

* is required

* Type: merged type ([ID](schema-definitions-categorymapping-properties-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-id.md "Cardigann#/definitions/CategoryMapping/properties/id")

### id Type

merged type ([ID](schema-definitions-categorymapping-properties-id.md))

one (and only one) of

* [ID as integer](schema-definitions-categorymapping-properties-id-oneof-id-as-integer.md "check type definition")

* [ID as string](schema-definitions-categorymapping-properties-id-oneof-id-as-string.md "check type definition")

## cat

Predefined Newznab/Torznab category name. See the Jackett-Categories wiki page.

`cat`

* is required

* Type: `string` ([IndexerCategories](schema-definitions-categorymapping-properties-indexercategories.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-indexercategories.md "Cardigann#/definitions/CategoryMapping/properties/cat")

### cat Type

`string` ([IndexerCategories](schema-definitions-categorymapping-properties-indexercategories.md))

### cat Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value                    | Explanation |
| :----------------------- | :---------- |
| `"Console"`              |             |
| `"Console/NDS"`          |             |
| `"Console/PSP"`          |             |
| `"Console/Wii"`          |             |
| `"Console/XBox"`         |             |
| `"Console/XBox 360"`     |             |
| `"Console/Wiiware"`      |             |
| `"Console/XBox 360 DLC"` |             |
| `"Console/PS3"`          |             |
| `"Console/Other"`        |             |
| `"Console/3DS"`          |             |
| `"Console/PS Vita"`      |             |
| `"Console/WiiU"`         |             |
| `"Console/XBox One"`     |             |
| `"Console/PS4"`          |             |
| `"Movies"`               |             |
| `"Movies/Foreign"`       |             |
| `"Movies/Other"`         |             |
| `"Movies/SD"`            |             |
| `"Movies/HD"`            |             |
| `"Movies/UHD"`           |             |
| `"Movies/BluRay"`        |             |
| `"Movies/3D"`            |             |
| `"Movies/DVD"`           |             |
| `"Movies/WEB-DL"`        |             |
| `"Audio"`                |             |
| `"Audio/MP3"`            |             |
| `"Audio/Video"`          |             |
| `"Audio/Audiobook"`      |             |
| `"Audio/Lossless"`       |             |
| `"Audio/Other"`          |             |
| `"Audio/Foreign"`        |             |
| `"PC"`                   |             |
| `"PC/0day"`              |             |
| `"PC/ISO"`               |             |
| `"PC/Mac"`               |             |
| `"PC/Mobile-Other"`      |             |
| `"PC/Games"`             |             |
| `"PC/Mobile-iOS"`        |             |
| `"PC/Mobile-Android"`    |             |
| `"TV"`                   |             |
| `"TV/WEB-DL"`            |             |
| `"TV/Foreign"`           |             |
| `"TV/SD"`                |             |
| `"TV/HD"`                |             |
| `"TV/UHD"`               |             |
| `"TV/Other"`             |             |
| `"TV/Sport"`             |             |
| `"TV/Anime"`             |             |
| `"TV/Documentary"`       |             |
| `"XXX"`                  |             |
| `"XXX/DVD"`              |             |
| `"XXX/WMV"`              |             |
| `"XXX/XviD"`             |             |
| `"XXX/x264"`             |             |
| `"XXX/UHD"`              |             |
| `"XXX/Pack"`             |             |
| `"XXX/ImageSet"`         |             |
| `"XXX/Other"`            |             |
| `"XXX/SD"`               |             |
| `"XXX/WEB-DL"`           |             |
| `"Books"`                |             |
| `"Books/Mags"`           |             |
| `"Books/EBook"`          |             |
| `"Books/Comics"`         |             |
| `"Books/Technical"`      |             |
| `"Books/Other"`          |             |
| `"Books/Foreign"`        |             |
| `"Other"`                |             |
| `"Other/Misc"`           |             |
| `"Other/Hashed"`         |             |

## desc

Tracker category name. If set, it is used for a 1:1 mapping between tracker and Newznab categories.

`desc`

* is optional

* Type: `string` ([Description](schema-definitions-categorymapping-properties-description.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-description.md "Cardigann#/definitions/CategoryMapping/properties/desc")

### desc Type

`string` ([Description](schema-definitions-categorymapping-properties-description.md))

## default

Use this category when the search query has no categories. Default: false.

`default`

* is optional

* Type: `boolean` ([Default](schema-definitions-categorymapping-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-default.md "Cardigann#/definitions/CategoryMapping/properties/default")

### default Type

`boolean` ([Default](schema-definitions-categorymapping-properties-default.md))
