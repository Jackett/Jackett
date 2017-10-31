INTEGRATION with validator.nu

THE PROBLEM:

dom["<some html>"].AppendTo("#something");

The context of "<some html>" is not associated with dom, because it's a fragment. At the same time if we bound it to the DOM,
selectors won't work, because it will use the index.

We need a way to associate a context for new selectors. Select or [] should always operate against a root context.

### Todo (punch list)

Performance:
-Share a dictionary between styles and attributes; just set a bit flag and use an integer.
-Move indexing from CSSStyleDeclaration into DomElement (like attributes)
-Standarize the interface for CSSStyleDeclaration and AttributeCollection with the browser DOM

(done) nth-child could be alot more efficient by caching the index postions of all siblings when checking the 1st one.
(done) Cache equations AFTER parsing

