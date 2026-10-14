# pengcanlyu v4 integration

This fork is based on upstream **v4.0.0 (7301fb8)**. It retains the upstream
documentation, explicit project binding, canonical response contracts, PLC and
network write pipeline, Multiuser support, HMI reads and compile diagnostics.
The package version is **4.0.0-pengcanlyu.1**. There are 8 tools in read-only and
18 in read-write/full. Do not install the upstream NuGet package and assume it
contains these fork extensions.

## Migration from the 2.7.7 fork

- Bind an already-open project with `bind_project`; `--project` is an assertion,
  not permission to open or switch projects implicitly.
- Replace the old read/write batches and safety-token flow with `plc_read`,
  `plc_write` and `network_read`/`network_write`. Use explicit `dryRun:true` first.
- Use `engineering_read` and `engineering_write` for the retained OPC UA and
  offline memory-card export features. These extensions are **standalone-only**;
  upstream Multiuser support does not imply extension support on local sessions.
- `engineering_write` defaults to **preview**, unlike upstream PLC/network
  writes. Actual execution requires `dryRun:false`. It uses the same pinned
  binding, validation, verification and audit pipeline. It never saves a project,
  connects to field hardware, downloads to a PLC, or changes RUN/STOP.
- Read-write lifecycle calls require MCP elicitation. Full mode removes only
  that server prompt, not Siemens access controls or the write guards.

## Hardware module and channel parameters

First get the hardware hierarchy with `network_read/read_hardware_config`.
Use exact name segments starting at the device's direct children, not a guessed
rack path. Inspect a module/submodule:

```json
{"operation":{"operation":"inspect_hardware_parameters","deviceName":"Station","itemPath":["AI module","AI submodule"],"channelIndex":0}}
```

This returns scalar attribute values, read/write metadata and a `stateHash`.
`channelIndex` is the returned collection index, not an I/O address or channel
number. Omit it to read all channels; large modules should be read channel by
channel. A selected-channel hash covers that channel and the module attributes,
not other channels. Use the same selector when writing:

```json
{"dryRun":true,"operation":{"operation":"set_hardware_parameters","deviceName":"Station","itemPath":["AI module","AI submodule"],"channelIndex":0,"expectedStateHash":"<64-hex hash from the read>","changes":{"Smoothing":1}}}
```

Only attributes reported as `ReadWrite`, readable and scalar are accepted. The
value must match the observed type; enum values must be defined. Unknown,
read-only, identity/placement, null/complex and stale-state inputs fail before mutation. Review the
preview, then submit explicitly with `dryRun:false`. Re-read the channel and
other affected channels afterward: Siemens parameter dependencies can change
values not listed in the request. There is no automatic multi-attribute rollback.
Partial failure returns the applied attribute names and before/after evidence.

The example value is **not a universal electrical configuration**. Parameter
names and values depend on exact article number, firmware and operating mode.
The installed V21 manual at `PublicAPI/V21/HW Parameter description/TIA Portal
Openness Hardware parameters.pdf` documents 7NF00 parameters such as
`OperatingType`, `OperatingRange`, `Smoothing` and
`InterferenceFrequencySuppression`. This is not proof that every older SM exposes
the same attributes. In particular, the tested G1 SM1231 V2.0 instance exposed
channel addresses/widths but not those analog settings through self-description.
Do not guess undocumented attributes or automate GUI/UAC as a fallback.

## Retained OPC UA and SAT preparation

`engineering_read` operations:

| Operation | Required selection |
| --- | --- |
| `list_opcua_interfaces` | Exact `plcName` |
| `inspect_opcua_variables` | Exact `plcName`; optional `includeVariables`, `maxVariables` |

`engineering_write` operations:

| Operation | Main fields |
| --- | --- |
| `generate_opcua_interface` | `plcName`, `interfaceName`, `interfaceUri`, `replaceExisting`, `enabled` |
| `set_opcua_interface_enabled` | `plcName`, `interfaceName`, `enabled` |
| `delete_opcua_interface` | `plcName`, `interfaceName` |
| `export_opcua_interface` | `plcName`, `interfaceName`, absolute XML `exportPath`, optional JSON `catalogPath` |
| `export_plc_to_folder` | `deviceName`, absolute **new** directory `exportPath` |

Generation/inspection retain `allowedSourcePathsPath` and its required
`allowedSourcePathsSha256` for filtering, optional output/catalog files and the
Siemens-derived generator with its source license. Model fingerprints ignore
only the generated root timestamp. The existing OPC UA service retains its own
best-effort compensating cleanup; this is not a general transactional guarantee.

`export_plc_to_folder` invokes the **offline directory overload** of the Siemens
DownloadProvider, checks for `SIMATIC.S7S`, files and zero export errors. It has
no online destination argument. The output still needs normal compilation,
target CPU/firmware verification and separate SAT deployment authorization.

## Local source files

`plc_write` block/type updates accept `contentFilePath` plus a 64-hex
`contentFileSha256` instead of inline `content`. The host verifies and reads a
local UTF-8 file once (1 byte to 8 MiB), then uses those immutable bytes for plan
and apply. UNC/device paths, alternate streams, mixed inline/file input and hash
mismatches are rejected. `expectedContentHash` still guards the current TIA
object. This does **not** bypass upstream's 60,000-character read/hash boundary
or the single-object/dependency restrictions.

## Installation and authorization

Use a side-by-side tool directory and retain the previous version for rollback.
The host requires .NET 10; the Openness worker remains .NET Framework 4.8. Use
the configured SDK/runtime path for host and child processes. Never redistribute
the installed Siemens implementation DLLs; `ref/` contains compile-only stubs.

After rebuilding/reinstalling the worker, refresh the Siemens whitelist using
the **installed worker EXE path, last-write UTC timestamp and SHA256**. Refresh
any restricted Openness-dialog helper policy to the same executable. Ordinary
TIA authorization is distinct from Windows UAC: UAC must be handled by the user.
Do not grant blanket access to unrelated executables or approve other dialogs.

## Primary references

- [Upstream v4.0.0](https://github.com/Czarnak/tia-portal-mcp/releases/tag/v4.0.0)
- [Siemens Openness code snippets](https://github.com/siemens/tia-portal-openness-code-snippets)
- [Siemens TIA AI extensions](https://github.com/siemens/tia-portal-ai-extensions)
- [Siemens channel access documentation](https://docs.tia.siemens.cloud/r/Rc0LxW522mLHDCX_sejL~A/S~HYtmj5j71~B4V9IoWa_A)
- Installed V21 hardware-parameter manual, 11/2025, 7NF00 V1.0/V1.1/V1.2 tables.

The Siemens repositories provide samples/guidance, not certification of this
fork. TIA engineering verification is not physical I/O commissioning.
