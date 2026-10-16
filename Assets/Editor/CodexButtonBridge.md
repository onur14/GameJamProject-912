# Local Unity button bridge

`CodexButtonBridge.cs` is editor-only. Unity loads it after script compilation.
It checks `Library/CodexBridge/request.json` every 250 ms and writes
`Library/CodexBridge/response.json`. Write requests to a temporary file, then
rename to `request.json` so the editor never reads a partial write. Send one
request at a time and match the response `id`.

Inspect the buttons in loaded scenes:

```json
{"id":"inspect-1","action":"inspect"}
```

Edit a button using its exact `path` from the inspection response:

```json
{"id":"edit-1","action":"edit","path":"Canvas/KabulEtme","setText":true,"text":"Yardım Et"}
```

Other optional flags: `setPosition` with `position: {"x":0,"y":0}`,
`setSize` with `size: {"x":200,"y":60}`, and `setColor` with
`color: {"r":1,"g":1,"b":1,"a":1}`. Size means RectTransform sizeDelta;
stretched anchors affect the final rendered size. Color edits the target
graphic's base color, which may be affected by the button transition state.

Edits support Undo, record prefab overrides, and mark the scene dirty.
They do not automatically save scenes or change click callbacks. Editing
is rejected in Play Mode. Prefab assets can still be edited through the
project files; this queue addresses loaded scene objects only.

If no response appears, focus Unity and use Assets > Refresh (Ctrl+R),
then resolve any compilation errors. Queue files stay inside ignored Library.
