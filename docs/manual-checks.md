# Manual checks before a deploy

The specs drive the app through the User port, and run every real adapter over fake Fittings
(decision 63), so the real Fittings and the Web App's views are checked by hand: no automated test
drives a browser (decision 45). Run this list in **Chrome (or Edge)** and **Firefox** before
pushing a release to `main`. When only a Fitting has changed, its own section is enough. If a fake
Fitting turns out to behave differently from the real one, tighten the contract in its Seam's doc
comments, and fix the fake.

Run the app with `dotnet run --project Motif.Sketch.App` (http://localhost:5173), or check the
published site after the workflow deploys it.

## First run and editing

- [ ] A fresh browser profile opens a blank project: the system is selected, its card is open with the name field focused, and the welcome card shows. Dismiss it, reload: it doesn't come back.
- [ ] Add one of each element type. Each is placed, linked to the system and selected. Primary actors go left; the rest go right, top to bottom.
- [ ] Drag an element: it moves live, then stays put (pinned). Auto-layout leaves it; All releases it.
- [ ] Double-click an element: its name field takes focus. F2 does the same. Delete deletes it (not the system).
- [ ] Ctrl+Z / Ctrl+Y undo and redo across both views. In a text field they undo the text instead.
- [ ] Mouse wheel zooms around the pointer; dragging open space pans; Fit fits.

## Sketch

- [ ] Start sketch: the User port has an adapter per primary actor, plus Specs (pale slate).
- [ ] Drag a Not placed item onto a port (new adapter), onto an adapter (merged; it lists both sources), and onto the hexagon's left and right halves (new driving / driven port).
- [ ] Drag an adapter onto another port, onto another adapter (reorder), and back onto Not placed.
- [ ] Rename a context element whose adapter still has its name: the adapter follows. Delete one: the adapter keeps its other sources and shows the orange flag.
- [ ] Minimise Not placed to its badge and back.
- [ ] Alt+1 / Alt+2 / Alt+3 and the thumbnail strips switch layouts; the split divider drags.

## Project Files (`JsInteropProjectFiles`, `projects.js`)

- [ ] Chrome: Save asks for a file the first time, then writes to the same file, and says "Saved to …". Reload, Save again: the browser asks once for permission, then writes to the same file.
- [ ] Chrome: refuse that permission. A toast says the browser wasn't allowed to write, and the work stays unsaved.
- [ ] Firefox: Save downloads a `.motifsketch.json` and says "Downloaded …"; Open uses the file picker.
- [ ] Closing either picker changes nothing.
- [ ] Open a broken file (e.g. any other JSON): a friendly list of problems, and the current work is untouched.

## Browser Storage (`JsInteropBrowserStorage`, `autosave.js`)

- [ ] The title shows a dot and the tab title a • while there are unsaved changes; closing the tab warns.
- [ ] Reload mid-edit: nothing is lost, and the unsaved marker is still there.
- [ ] Chrome: save to a file, edit, reload, Save: it writes to the same file after asking once for permission (the file handle came back from autosave).
- [ ] Open a second tab: it's read-only with a banner. "Use this tab instead" moves editing to it, and the first tab goes read-only with the latest work saved.

## Glossary Files (`JsInteropGlossaryFiles`, `glossaries.js`)

- [ ] Export → Glossary…, Add to a glossary…: pick a copy of `motif/glossary.md`. With nothing new it says so and the file is unchanged; after adding a storage element, the new term appears in alphabetical place, marked proposed, and `git diff` shows only added lines. New glossary.md writes a whole glossary.
- [ ] The dialog's note says the glossary is saved back to the same file in Chrome, and downloads in Firefox, where both buttons download.
- [ ] Closing the picker writes nothing.

## GitHub (the browser's `HttpClientHandler`)

- [ ] Open `#repo=motif-dotnet/Motif.Sketch`: the repo's sketch, read-only, labelled with the repo and with a banner. The tool switcher says each tool opens that repo, and Motif Glossary opens its glossary. `#repo=someone/nothing-here`: a friendly error, then your own work.

## Adapters that are their own Fittings

SVG Files (`diagramexports.js`), Markdown File (`solutionstubs.js`) and Browser Clipboard
(`clipboard.js`) have no rules of their own, so no spec runs them (decision 63).

- [ ] Export both SVGs and the plan. The SVGs open in a browser and match the screen in light mode (no orange selection, no centre line). In dark mode the screen is dark but the exports are light.
- [ ] The Export menu's dot and notes appear when items aren't placed; a port named "Domain" blocks the plan with a reason.
- [ ] Copy link, open it in another browser (or a private window): read-only, both views, Export works, Make a copy turns it into ordinary unsaved work and clears the link from the address bar.
- [ ] If the browser refuses clipboard access, the link appears in a dialog to copy by hand.

## Sharing

- [ ] Damage a link (cut its end off): a friendly error, then your own work.

## Devices and look

- [ ] Tablet (or touch emulation): drag, pinch to zoom, tap and hold to rename.
- [ ] Phone width (under 600px): read-only with a note; snapshot links read well.
- [ ] Phone width with no autosaved work (or only the system): opens the example read-only, with the example banner.
- [ ] `#example` on a computer: the example read-only, "Example · read-only", Make a copy to edit works.
- [ ] Empty work on a computer (fresh, or after New): the welcome card shows until dismissed; once dismissed it never returns.
- [ ] Theme menu: System / Light / Dark, remembered after a reload, and shared with Motif Glossary on the same site.
- [ ] The mark and name open the tool switcher: Motif Sketch is marked as this tool, and Motif Glossary opens the Glossary.
- [ ] Open example shows Motif Sketch's own design.
