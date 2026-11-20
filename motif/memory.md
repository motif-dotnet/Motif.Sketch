Rules for anyone working on the Motif.Sketch repo, on top of the [README](README.md), the [glossary](glossary.md) and [decisions.md](../decisions.md).

### Hard Rules
- The specs drive the app through its User port, so they can't see the page's layout. After a change to the app bar, a banner or anything else the layout depends on, check it in a browser at 375 px (phone), 637 px (between the phone and narrow breakpoints) and 1280 px, in each mode it shows: the Example (`#example`), a repo link (`#repo=owner/name`), a snapshot link, and the visitor's own work. The bar passes when its content is no wider than the bar (`scrollWidth <= clientWidth`) and nothing overlaps.
- `Suite/`, `wwwroot/css/motif.css`, `wwwroot/fonts/` and `scripts/sync-suite.sh` are the Motif web kit's copy. Change the kit in motif-dotnet/.github (`brand/web`), push it, then sync it here with `scripts/sync-suite.sh`. Never edit the copy.
