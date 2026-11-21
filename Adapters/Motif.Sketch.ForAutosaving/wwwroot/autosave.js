// The Browser Storage Fitting: the work in IndexedDB, with the file handle beside it, and a lock its tabs share
// (Web Locks), asked for on a BroadcastChannel. One call to the browser each; the rules are in the C# adapter.

const DB = "motif-sketch", STORE = "work", KEY = "current", LOCK = "motif-sketch-autosave", ASK = "take-over";
const handles = (globalThis.motifFileHandles ??= new Map());
const channel = "BroadcastChannel" in window ? new BroadcastChannel("motif-sketch") : null;

let release = null;      // resolves the held lock's promise, letting the lock go

export function listen(dotnet) {
  if (channel) channel.onmessage = e => e.data === ASK && dotnet.invokeMethodAsync("Asked");
}

export function canLock() {
  return "locks" in navigator;
}

// The lock is held for as long as the promise its callback returns is pending.
function hold() {
  return new Promise(r => (release = r));
}

export function tryLock() {
  return new Promise(resolve =>
    navigator.locks.request(LOCK, { ifAvailable: true }, lock => {
      resolve(lock !== null);
      return lock && hold();
    }));
}

export function lock() {
  return new Promise(resolve =>
    navigator.locks.request(LOCK, () => {
      resolve();
      return hold();
    }));
}

export function unlock() {
  release?.();
  release = null;
}

export function askForTheLock() {
  channel?.postMessage(ASK);
}

function open() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB, 1);
    request.onupgradeneeded = () => request.result.createObjectStore(STORE);
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

async function run(mode, action) {
  const db = await open();
  try {
    return await new Promise((resolve, reject) => {
      const tx = db.transaction(STORE, mode);
      const request = action(tx.objectStore(STORE));
      tx.oncomplete = () => resolve(request.result);
      tx.onerror = () => reject(tx.error);
    });
  } finally {
    db.close();
  }
}

// The handle itself is kept, not its id: ids last only as long as the page.
export async function read() {
  const work = await run("readonly", store => store.get(KEY));
  if (!work) return null;
  let fileHandle = null;
  if (work.handle) {
    fileHandle = `restored-${Date.now().toString(36)}`;
    handles.set(fileHandle, work.handle);
  }
  return { json: work.json, fileName: work.fileName ?? null, fileHandle, unsaved: !!work.unsaved };
}

export function write(work) {
  const handle = work.fileHandle ? handles.get(work.fileHandle) ?? null : null;
  return run("readwrite", store =>
    store.put({ json: work.json, fileName: work.fileName, handle, unsaved: work.unsaved }, KEY));
}
