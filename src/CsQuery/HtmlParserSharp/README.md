HtmlParserSharp
===============

This is a manual C# port of the [Validator.nu HTML Parser](http://about.validator.nu/htmlparser/), a HTML5 parser originally written in Java and (compiled to C++ using the Google Web Toolkit) used by Mozilla's Gecko rendering engine. This port is current as of Version 1.4.

The code is DOM-agnostic and provides an interface via `TreeBuilder<T>` for creating a DOM from its output using any object model. Included in the code base is a `TreeBuilder` that produces a DOM using System.Xml.

Status
------

This port was created by Patrick Reisert based on Validator.nu 1.3. It was adopted by James Treworgy in September, 2012 to use in [CsQuery](https://github.com/jamietre/CsQuery). However, since a general-purpose HTML5 parser is extraordinarily useful, I've kept it as an independent project. It's included as a submodule in CsQuery to simplify distribution. It may become an external dependency at some point if development of the parser substantially diverges from CsQuery in the future.

