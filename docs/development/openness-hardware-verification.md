# Hardware coverage verification, 2026-10-14

Candidate: **4.0.0-pengcanlyu.2**, upstream v4.0.0 plus the earlier fork merge.
Installed host and worker were exercised through MCP stdio against TIA V21.
All edits were confined to a disposable local engineering copy. No field PLC
connection, download, mode change or process-value write was performed.

## Runtime checks

| Check | Result |
| --- | --- |
| Exact binding and original-project preservation | Original reopened; unmodified, same last-modified timestamp; OPC UA interface still enabled |
| G1 SM1231 metadata | Addresses/channels present; requested analog range/smoothing absent; no invented fallback |
| 1512C / 7NF00 V1.2 placement | Exact catalog lookup and rail CanPlugNew succeeded; insert/readback preserved existing modules; stale hash rejected |
| Composition versus physical placement | CPU and SM are rail members but station composition children; paths and slot checks now use the correct respective relationships |
| Parameter dependency | 7NF FromTemplate mode exposed analog settings read-only; explicit Manual mode exposed them ReadWrite |
| Analog edit and restore | Channel 0 range 11 -> 9 -> 11 and smoothing 0 -> 1 -> 0, then FromTemplate restored; original selected-channel hash recovered |
| Address edit and restore | Start byte 126 -> 1000 -> 126, original selected-address hash recovered |
| Collision and unit boundary | CPU input overlap rejected; adjacent AI8 ranges 126/142 accepted, 141 rejected; each reports 128 bits / 16 bytes |
| Hardware compilation success path | G1 fixture: 0 errors, 1 warning |
| Hardware compilation error path | Newly created 1512C: 2 missing configuration-password/certificate errors, 1 display-password warning; correctly reported unsuccessful |
| Bounded capability discovery | Metadata-only response, missing-attribute reasons, no writable state hash |

The 1512C fixture is **not** a fully compiled/commissioned target application.
Its security credentials were deliberately not invented or bypassed. The new
compile tool proves access and truthful diagnostics, not migration acceptance.
Successful writes also do not establish physical sensor/load compatibility.

Installed worker SHA256:
`d214bfaaecdb567fae2b058b606208f74e5268012c96d825acd386ca295f08e3`.
Whitelist and restricted authorization-helper identity were refreshed to this
exact installed binary. Windows UAC was not automated.

## Automated checks

- Real installed Siemens references and compile-only stub builds passed.
- The final hardware-focused test selection passed 30 tests, including exact
  selectors, stale/invalid hashes, access-mode classification, separate physical
  containers, bounded discovery, and mixed byte-offset/bit-length collisions.
- The final local full suite passed **6,040 tests**, with zero failures or skips.
  CI outcomes are recorded with the pull request; intermediate runs are not
  evidence for later candidate revisions.
- `ref/` remains generated compile-only declarations, not Siemens implementation
  DLLs. The declaration source-hash verifier is retained.

Runtime receipts and the disposable engineering project are local-only; they
contain project details and are intentionally not published in the repository.
The original v1 report remains historical evidence for the previous package.
