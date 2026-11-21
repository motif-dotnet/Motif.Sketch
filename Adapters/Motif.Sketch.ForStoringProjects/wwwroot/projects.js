// The Project Files Fitting: project files through the File System Access API where the browser has it, else
// upload and download. One call to the browser each; the rules are in the C# adapter.

// File handles live here, keyed by opaque ids, so Browser Storage can keep them by id too.
// This map is the one browser contract shared between adapters.
const handles = (globalThis.motifFileHandles ??= new Map());
let counter = 0;
// Chrome refuses picker extensions over 16 characters, and ".motifsketch.json" is 17. ".json" covers it:
// the suggested name still ends in ".motifsketch.json", and the open picker still lists those files.
const types = [{ description: "Motif Sketch project", accept: { "application/json": [".json"] } }];
const readwrite = { mode: "readwrite" };

function keep(handle) {
  const id = `file-${Date.now().toString(36)}-${counter++}`;
  handles.set(id, handle);
  return id;
}

// The user closing a picker is declining, not a failure.
async function declinable(pick) {
  try {
    return await pick();
  } catch (e) {
    if (e.name === "AbortError") return null;
    throw e;
  }
}

export function canWriteInPlace() {
  return "showSaveFilePicker" in window;
}

// Returns { name, text, handle } or null.
export async function open() {
  if ("showOpenFilePicker" in window) {
    const picked = await declinable(() => window.showOpenFilePicker({ types }));
    if (!picked) return null;
    const file = await picked[0].getFile();
    return { name: file.name, text: await file.text(), handle: keep(picked[0]) };
  }
  return await new Promise((resolve, reject) => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".json,.motifsketch.json,application/json";
    input.addEventListener("change", async () => {
      const file = input.files?.[0];
      if (!file) return resolve(null);
      try { resolve({ name: file.name, text: await file.text(), handle: null }); } catch (e) { reject(e); }
    });
    input.addEventListener("cancel", () => resolve(null));
    input.click();
  });
}

// Returns { name, handle } or null.
export async function pickWhereToSave(suggestedName) {
  const handle = await declinable(() => window.showSaveFilePicker({ suggestedName, types }));
  return handle && { name: handle.name, handle: keep(handle) };
}

export async function mayWrite(id) {
  return (await handles.get(id).queryPermission(readwrite)) === "granted";
}

export async function askToWrite(id) {
  return (await handles.get(id).requestPermission(readwrite)) === "granted";
}

export async function write(id, text) {
  const handle = handles.get(id);
  const writable = await handle.createWritable();
  await writable.write(text);
  await writable.close();
  return handle.name;
}

export function download(text, name) {
  const url = URL.createObjectURL(new Blob([text], { type: "application/json" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}
