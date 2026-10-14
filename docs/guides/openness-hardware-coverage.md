# Openness engineering coverage

Fork version: **4.0.0-pengcanlyu.2**, based on upstream v4.0.0.
This is a bounded engineering surface, not an unrestricted wrapper around every
Siemens API. Exact device/firmware capabilities must be discovered before use.

## Workflow coverage

| Engineering need | MCP route | Boundary |
| --- | --- | --- |
| Catalog, station creation, hardware hierarchy | `network_read` / `network_write` | Resolve installed catalog identifiers; no guessed article/firmware strings |
| SM placement | `engineering_read`: `inspect_hardware_layout`, `check_hardware_module`; `engineering_write`: `plug_hardware_module` | Exact parent and vacant slot, `CanPlugNew`, state hash, readback; add only |
| Module/channel settings | `inspect_hardware_capabilities`, `inspect_hardware_parameters`, `set_hardware_parameters` | Only exposed scalar ReadWrite parameters; explicit per-channel selection |
| Process I/O addresses | Parameter read/write with `addressIndex` | Modeled `Address.StartAddress`, Input/Output only, same-controller overlap check |
| Hardware compilation | `engineering_write`: `compile_hardware` | Exact CPU DeviceItem, hardware-only; bounded diagnostics, no save/download |
| PLC code, DB/UDT, tags and cross references | `plc_read`, `plc_write`, `read_cross_references`, `compile_check` | Upstream operation contracts and source-format restrictions remain |
| PROFINET network and I/O systems | `network_read`, `network_write` | Qualified identities and upstream guards; inspect supported operations first |
| OPC UA interfaces | Engineering OPC UA operations | Standalone project, optional source allowlist pinned by SHA256 |
| Versionable sources and offline SAT preparation | Existing PLC export; `export_plc_to_folder` | Export does not authorize deployment; target/firmware/license validation still required |
| Project save, archive, lifecycle | Existing project tools | Explicit binding, dirty-project and access-mode guards |
| HMI | Upstream `hmi_read` | WinCC Unified read-only coverage, not HMI editing |

Not added: arbitrary object invocation/reflection, undocumented parameter writes,
bulk credential/security-policy changes, F-program engineering, technology-object
editors, library-wide destructive changes, or online PLC state changes. These
need specific requirements and their own validation, not a blanket fallback tool.
Siemens samples/AI skills do not make missing device APIs available.

## Discover, then edit

`inspect_hardware_capabilities` returns metadata and CLR enum names without
reading dynamic values. Optional `attributeNames` (up to 32 exact unique names)
reports missing parameters explicitly instead of probing undocumented names.
This metadata-only response has **no write hash**. Integer-coded parameter values
still require the manual for the exact module/firmware and active operating mode.

Read-only does not necessarily mean permanently unsupported: on the tested
7NF00 V1.2, `ParameterSettings=1` (FromTemplate) made range/type/smoothing
read-only. After an explicit, authorized switch to `ParameterSettings=0`
(Manual), re-inspection exposed those parameters as ReadWrite. The editor never
changes that mode implicitly and rejects a combined mode-plus-currently-read-only
parameter request. Change mode separately, re-read, then plan the dependent edit.
The installed 7NF00 V1.1 parameter table documents these values; V1.2 behavior was
verified in the local engineering fixture, not assumed from a universal mapping.

```json
{"operation":{"operation":"inspect_hardware_capabilities","deviceName":"Station","itemPath":["AI module","AI submodule"],"channelIndex":0,"attributeNames":["OperatingRange","Smoothing"]}}
```

Then read values with `inspect_hardware_parameters`, retaining the same selectors.
Use that returned `stateHash` as `expectedStateHash` for a preview and an explicitly
authorized actual write. A selected-channel hash does not cover other channels.
Re-read dependent channels/module settings after a change. No automatic retry or
transactional rollback is promised for multi-attribute writes.

Modeled address properties are a deliberate exception to dynamic-metadata-only
editing: `StartAddress` is implemented via its documented typed getter/setter.
`addressIndex` and `channelIndex` are mutually exclusive observed collection
indexes, not address numbers. Only `{"StartAddress":100}` is permitted on an
address target. Length and I/O direction cannot be changed through this editor.
`startAddress` is a byte offset, while `lengthBits` is the raw Openness bit length;
`lengthBytes` is rounded up for cabinet/address planning. In the V21 fixtures,
both G1 AI8 and 7NF AI8 reported 128 bits for their 16-byte input range. Collision
checks convert byte starts to bits rather than treating 128 as 128 bytes.
The existing `network_read` I/O-map controller resolver uses the same units;
its former byte-length assumption and documentation were corrected as well.
Without an address controller, collision verification cannot be completed and
the write is refused. All assigned controllers and their registered ranges in the
same Input/Output address space are checked. Hardware compile is still required.

## Add an SM

1. Inspect the exact station/container hierarchy. `itemPath:[]` means the station
   root; a nonempty path identifies the actual rack/container. Do not assume a
   particular CPU family's hierarchy. `layout.items` are physical slot members
   selected by `DeviceItem.Container`; `layout.children` are composition children
   for path navigation. These differ on S7-1500: the CPU/SM can be root composition
   siblings of the rail while physically plugged into it. Returned item paths
   always follow the composition hierarchy, not the physical slot relationship.
2. Query the installed catalog for the exact module type/firmware identifier.
3. Call `check_hardware_module` with `moduleTypeIdentifier`, `moduleName`, and
   `positionNumber`. Check `canPlug` and reasons. Retain its layout `stateHash`.
4. Preview `plug_hardware_module`, then authorize `dryRun:false` with the same
   fields/hash. A stale layout, occupied name/slot, or `CanPlugNew:false` fails
   before placement. Siemens can still reject a placement after a true precheck.
5. Inspect actual child items and channels, assign electrical settings/addresses,
   then call `compile_hardware` on the CPU itemPath. Run software `compile_check`
   separately. Save only after reviewing both reports.

Placement returns before/after layouts. It does not move, replace or delete an
existing module. An apply exception is not permission to retry: inspect first.
Compile success verifies engineering consistency, not wiring, input impedance,
measurement accuracy, load capability or a commissioned physical machine.

## Sources

- [Siemens official Openness snippets](https://github.com/siemens/tia-portal-openness-code-snippets)
- [Siemens TIA AI extensions](https://github.com/siemens/tia-portal-ai-extensions)
- [V21 module placement](https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/functions-on-device-items/creating-and-plugging-a-device-item)
- [V21 address API](https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/functions-on-device-items/accessing-addresses)
- [V21 address controller](https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/functions-on-device-items/accessing-address-controller)
- [V21 compilation](https://docs.tia.siemens.cloud/r/en-us/v21/tia-portal-openness-api-for-automation-of-engineering-workflows/tia-portal-openness-api/functions-for-projects-and-project-data/compiling-a-project)
- Installed V21 `Siemens.Engineering.Base.xml`, `Step7.xml`, and the hardware
  parameter manual under `PublicAPI/V21/HW Parameter description`.
