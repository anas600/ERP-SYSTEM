// Polyfills for jsdom — Sprint 61 (FE unit tests)
// Runs BEFORE the test framework is loaded (configured in `setupFiles`).
// Jest-DOM matchers are loaded separately via `setupFilesAfterEach`.
const { TextEncoder, TextDecoder } = require('util');

if (typeof global.TextEncoder === 'undefined') {
  global.TextEncoder = TextEncoder;
}
if (typeof global.TextDecoder === 'undefined') {
  global.TextDecoder = TextDecoder;
}
if (typeof global.structuredClone === 'undefined') {
  global.structuredClone = (v) => JSON.parse(JSON.stringify(v));
}
if (typeof global.URL.createObjectURL !== 'function') {
  const counter = { n: 0 };
  global.URL.createObjectURL = (file) => {
    if (!file) return 'blob:null';
    const id = ++counter.n;
    return `blob:mock/${id}-${encodeURIComponent(file.name ?? 'file')}`;
  };
  global.URL.revokeObjectURL = (_url) => {
    /* no-op for tests */
  };
}
