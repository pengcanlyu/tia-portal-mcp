# pengcanlyu v4 scoped verification, 2026-10-14

Base: upstream v4.0.0, commit `7301fb8`. Package: `4.0.0-pengcanlyu.1`.
Integration preserves the fork's OPC UA generator/filter/fingerprint work,
channel inspection, local file content input, offline SAT-folder export and
optional-publishing credential guards. Retired v2 token/batch interfaces were
not resurrected. The previous fork main is an ancestor of the integration.

## Offline qualification

- Real V21 assembly build and compile-only reference-stub build succeeded.
- Full automated suite: **6019 passed, 0 failed**.
- Final identity-attribute guard and related PLC/contract regression: **125 passed**.
- Local package verification: one canonical net10.0 worker subtree, required
  worker files present, no Siemens implementation DLLs and no worker runtimeconfig.
- Installed host/worker doctor passed assembly/runtime/group checks. An unbound
  writable host correctly reported binding warnings.

## Live engineering qualification

Only a disposable offline copy was edited. No PLC download, online connection,
RUN/STOP, live process write, server deployment or Windows UAC automation occurred.
The installed MCP stdio host was exercised through the MCP client SDK.

- Discovery returned 18 writable-mode tools and exact open-project binding worked.
- Save-as produced the disposable copy. Hardware hierarchy, I/O channels,
  project tree, PLC source and OPC UA interface reads succeeded.
- G1 SM1231 V2.0 exposed channel identity/address/width, **not** analog range or
  filter settings in its self-description. Unsupported writes must remain denied.
- A temporary CPU 1512C-1 PN, article `6ES7512-1CM03-0AB0`, firmware V4.1,
  exposed integrated analog `OperatingType`, `OperatingRange`, `Smoothing` and
  diagnostics. Preview, `Smoothing` 0 -> 1, readback and restoration 1 -> 0
  succeeded. A stale-hash write was rejected as `state_changed`.
- Selected-channel reads returned one channel with original collection index;
  selected-channel writes and restoration succeeded.
- A filtered two-variable custom OPC UA interface was previewed/generated,
  enabled, exported with catalog, and deleted. The original interface remained.
- A temporary SCL FC was created. Hash-pinned local-file source update changed
  VERSION 0.1 -> 0.2 and source readback confirmed it.
- Existing G1 PLC software compiled with **0 errors, 0 warnings**.
- Offline folder export produced `SIMATIC.S7S`, **80 files**, 1,910,169 bytes,
  **0 errors, 0 warnings**. This is a test artifact, not a production release.
- Worker whitelist path/timestamp/hash and the restricted dialog helper policy
  were refreshed. No user UAC intervention was required during this run.

## Limits

- No physical 1512C or SM electrical acceptance was performed.
- 7NF00 parameter support was checked against the installed Siemens V21 manual;
  **the standalone 7NF module was not live-edited**. Creating an SM directly as a
  root network device is invalid; the failed disposable-fixture attempt was
  inspected, not replayed. A real SM must be plugged into a compatible station.
- Multiuser extension operations are intentionally unavailable; upstream
  Multiuser behavior relies on upstream qualification and automated regressions.
- Large OPC UA models, all hardware types/ranges and actual SAT distribution
  are not qualified by this test. Inspect API metadata and test a copy first.
- Detailed receipts, PLC source, project files and local paths are private and
  are not committed to this public repository.
