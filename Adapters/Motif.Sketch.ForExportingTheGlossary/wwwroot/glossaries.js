// The Glossary Files Fitting: the project's glossary (motif/glossary.md) through the File System Access API where the browser has it, else
// upload and download. One call to the browser each; the rules are in the C# adapter.

// Handles stay in this module: unlike a project file's, a glossary's handle isn't autosaved, so it lasts only
// until the page closes.
const handles = new Map();
let counter = 0;
const types = [{ description: "Markdown glossary", accept: { "text/markdown": [".md"] } }];
const readwrite = { mode: "readwrite" };

function keep(handle) {
  const id = `glossary-${counter++}`;
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
    input.accept = ".md,.markdown,text/markdown";
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
  const url = URL.createObjectURL(new Blob([text], { type: "text/markdown" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}
