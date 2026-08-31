# Graph Report - src  (2026-08-31)

## Corpus Check
- 106 files · ~39,233 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1519 nodes · 2922 edges · 204 communities (54 shown, 48 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 161 edges (avg confidence: 0.82)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `4d045e38`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Local MCP Sandbox Testing
- WorkerRequest
- OpennessWorkerClient
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/NetworkDeviceCreator.cs
- BatchOperationRequest
- TiaMcpServer.Tests
- BlockAddress
- CompileChecker
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/CompileChecker.cs
- TiaPortalSession
- .List
- CatalogEntryInfo
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/CrossReferenceReader.cs
- ConfigureNetworkDeviceResultInfo
- WriteSafetyService
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/EquipmentCatalogSearcher.cs
- TagTableInfo
- OpcUaGenerationContext
- TagMutationResultInfo
- tia-portal-mcp/TiaMcpServer.Tests/CrossReferenceInfoTests.cs
- BuildXElement
- OpcUaInterfaceService
- tia-portal-mcp/TiaMcpServer.Contracts/BlockAddress.cs
- ProjectTreeNode
- tia-portal-mcp/TiaMcpServer.Tests/BlockAddressTests.cs
- HardwareConfigReader
- tia-portal-mcp/TiaMcpServer.Tests/HardwareConfigInfoTests.cs
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/AssemblyResolver.cs
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/BlockImporter.cs
- AddDeviceResultInfo
- ProjectSessionBinding
- TiaMcpServer.Tests
- tia-portal-mcp/TiaMcpServer.Tests/CatalogEntryInfoTests.cs
- tia-portal-mcp/TiaMcpServer.Tests/CompileCheckInfoTests.cs
- CrossReferenceReader
- TiaMcpServer.Contracts
- OpcUaGenerationResult
- tia-portal-mcp
- tia-portal-mcp/TiaMcpServer.Tests/AddDeviceResultInfoTests.cs
- tia-portal-mcp/TiaMcpServer.Tests/CatalogTypeIdentifierTests.cs
- tia-portal-mcp/TiaMcpServer.Tests/ConfigureNetworkDeviceResultInfoTests.cs
- .Generate
- AddNetworkDeviceToolTests.cs
- ConfigureNetworkDeviceToolTests.cs
- .FindAll
- DeviceItemInfo
- tia-portal-mcp/TiaMcpServer.Contracts/CatalogTypeIdentifier.cs
- HardwareConfigInfo
- OpcUaNodeCatalogEntry
- CompileCheckToolTests.cs
- SearchEquipmentCatalogToolTests.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CrossReferenceFilterNames.cs
- tia-portal-mcp/TiaMcpServer.OpennessWorker/Openness/BlockExporter.cs
- tia-portal-mcp/TiaMcpServer.Contracts/AddDeviceResultInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CatalogEntryInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CompileCheckReport.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CompileMessageInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/ConfigureNetworkDeviceResultInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CrossReferenceLocationInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CrossReferenceReport.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CrossReferenceSourceInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/CrossReferenceTargetInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/DeviceInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/DeviceItemInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/HardwareConfigInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/IoSystemInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/NetworkInterfaceInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/NodeInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/PlcCompileInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/PlcCrossReferenceInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/ProjectTreeNode.cs
- tia-portal-mcp/TiaMcpServer.Contracts/SubnetInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/TagInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/TagTableInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/UserConstantInfo.cs
- tia-portal-mcp/TiaMcpServer.Contracts/WorkerResponse.cs
- CrossReferenceInfoTests
- SubnetInfo
- NodeInfo
- OpcUaToolTests
- InterfaceTemplate
- Multi-process Architecture Rationale
- .NET 8
- .NET Framework 4.8
- SIEMENS_SOURCE_LICENSE.md
- BatchToolMetadataTests
- PlcProgramExportToolTests
- CrossReferenceLocationInfo
- .TryNormalize
- CrossReferenceSourceInfo
- CrossReferenceTargetInfo
- PlcCrossReferenceInfo
- CrossReferenceReport
- DeviceInfo
- UserConstants
- Requirements
- Run Locally
- Local Package Build
- MCP Client Configuration
- Install
- CrossReferenceFilterNames
- Build From Source

## God Nodes (most connected - your core abstractions)
1. `WorkerRequest` - 93 edges
2. `OpennessWorkerClient` - 74 edges
3. `TiaMcpServer.Contracts` - 58 edges
4. `BatchOperationRequest` - 56 edges
5. `WorkerResponse` - 47 edges
6. `Program` - 47 edges
7. `OpcUaGenerationContext` - 32 edges
8. `WriteSafetyService` - 30 edges
9. `TiaPortalSession` - 25 edges
10. `TiaMcpServer.Tests` - 22 edges

## Surprising Connections (you probably didn't know these)
- `OpennessWorkerClient` --references--> `ProjectSessionBinding`  [EXTRACTED]
  TiaMcpServer/Worker/OpennessWorkerClient.cs → TiaMcpServer.Contracts/ProjectSessionBinding.cs
- `CrossReferenceTargetInfo` --references--> `CrossReferenceLocationInfo`  [EXTRACTED]
  TiaMcpServer.Contracts/CrossReferenceTargetInfo.cs → TiaMcpServer.Contracts/CrossReferenceLocationInfo.cs
- `CrossReferenceReport` --references--> `PlcCrossReferenceInfo`  [EXTRACTED]
  TiaMcpServer.Contracts/CrossReferenceReport.cs → TiaMcpServer.Contracts/PlcCrossReferenceInfo.cs
- `CrossReferenceSourceInfo` --references--> `CrossReferenceTargetInfo`  [EXTRACTED]
  TiaMcpServer.Contracts/CrossReferenceSourceInfo.cs → TiaMcpServer.Contracts/CrossReferenceTargetInfo.cs
- `PlcCrossReferenceInfo` --references--> `CrossReferenceSourceInfo`  [EXTRACTED]
  TiaMcpServer.Contracts/PlcCrossReferenceInfo.cs → TiaMcpServer.Contracts/CrossReferenceSourceInfo.cs

## Import Cycles
- None detected.

## Communities (204 total, 48 thin omitted)

### Community 0 - "Local MCP Sandbox Testing"
Cohesion: 0.15
Nodes (13): code:powershell (dotnet restore TiaMcpServer.sln), code:powershell (npx -y @modelcontextprotocol/inspector dotnet .\TiaMcpServer), code:powershell (npx -y @modelcontextprotocol/inspector dotnet .\TiaMcpServer), code:json ({}), code:json ({), code:json ({), code:json ({), code:json ({) (+5 more)

### Community 1 - "WorkerRequest"
Cohesion: 0.05
Nodes (62): DownloadConfiguration, DownloadProvider, WorkerRequest, AllowedSourcePathsPath, AllowTiaConfirmations, ArchiveDirectory, ArchiveMode, ArchiveName (+54 more)

### Community 2 - "OpennessWorkerClient"
Cohesion: 0.07
Nodes (25): Process, Task, BatchWorkerInvoker, Func, JsonSerializerOptions, Task, WriteSafetyApplyContext, WriteSafetyTooling (+17 more)

### Community 4 - "BatchOperationRequest"
Cohesion: 0.05
Nodes (49): CombinedState, Error, Func, IReadOnlyList, Task, BatchExecutionEngine, BatchOperationRequest, BlockPath (+41 more)

### Community 5 - "TiaMcpServer.Tests"
Cohesion: 0.05
Nodes (29): TiaMcpServer, TiaMcpServer.Tests, TiaMcpServer.Batch, TiaMcpServer.Tools, TiaMcpServer.Safety, TiaMcpServer.Worker, JsonElement, BatchOperationResult (+21 more)

### Community 6 - "BlockAddress"
Cohesion: 0.06
Nodes (33): ArgumentException, Regex, IReadOnlyList, List, BlockAddress, BlockName, FolderPath, IsDeterministic (+25 more)

### Community 7 - "CompileChecker"
Cohesion: 0.06
Nodes (32): CompilerResult, CompilerResultMessage, CompilerResultState, MethodInfo, List, CompileCheckReport, BlockPath, OverallState (+24 more)

### Community 9 - "TiaPortalSession"
Cohesion: 0.08
Nodes (26): ConfirmationEventArgs, DateTime, EventArgs, NotificationEventArgs, ProjectArchivationMode, ProjectLifecycleResultInfo, Operation, Project (+18 more)

### Community 10 - ".List"
Cohesion: 0.09
Nodes (17): Assembly, IReadOnlyDictionary, IReadOnlySet, ResolveEventArgs, IReadOnlyList, BatchOperationCatalog, ReadOperationNames, WriteOperationNames (+9 more)

### Community 11 - "CatalogEntryInfo"
Cohesion: 0.08
Nodes (21): HashSet, CatalogEntryInfo, ArticleNumber, CatalogPath, Description, TypeIdentifier, TypeIdentifierNormalized, TypeName (+13 more)

### Community 13 - "ConfigureNetworkDeviceResultInfo"
Cohesion: 0.09
Nodes (20): Dictionary, List, ConfigureNetworkDeviceResultInfo, AppliedSettings, DeviceName, Messages, SkippedSettings, Device (+12 more)

### Community 15 - "WriteSafetyService"
Cohesion: 0.09
Nodes (24): ConcurrentDictionary, DateTimeOffset, ops, SafetyTokenEntry, states, IReadOnlyList, BatchCurrentState, BatchOperationTarget (+16 more)

### Community 17 - "TagTableInfo"
Cohesion: 0.10
Nodes (21): TagInfo, Comment, DataType, LogicalAddress, Name, List, TagTableInfo, FolderPath (+13 more)

### Community 19 - "OpcUaGenerationContext"
Cohesion: 0.07
Nodes (27): IDisposable, Dictionary, PlcSoftware, PlcUnit, XDocument, OpcUaGenerationContext, AccessLevelDictionary, Current (+19 more)

### Community 21 - "TagMutationResultInfo"
Cohesion: 0.22
Nodes (13): TagMutationResultInfo, FolderPath, Operation, PlcName, ProjectPath, Success, TableName, TagName (+5 more)

### Community 23 - "BuildXElement"
Cohesion: 0.11
Nodes (19): PlcStruct, PlcTypeUserGroup, BuildXElement(), CreateStructElements(), IEnumerable, XElement, GetAccessLevel(), GetArrayDimensions() (+11 more)

### Community 24 - "OpcUaInterfaceService"
Cohesion: 0.20
Nodes (9): FileInfo, OpcUaProvider, ServerInterface, ServerInterfaceComposition, IReadOnlyList, JsonSerializerOptions, PlcSoftware, Project (+1 more)

### Community 26 - "ProjectTreeNode"
Cohesion: 0.17
Nodes (16): PlcTypeGroup, Dictionary, List, ProjectTreeNode, Children, Details, Name, NodeType (+8 more)

### Community 28 - "HardwareConfigReader"
Cohesion: 0.21
Nodes (8): IEngineeringObject, Func, IEnumerable, NetworkInterface, Node, Project, Subnet, HardwareConfigReader

### Community 33 - "AddDeviceResultInfo"
Cohesion: 0.12
Nodes (14): List, AddDeviceResultInfo, DeviceName, RootItemName, TypeIdentifier, Warnings, DeviceItem, Func (+6 more)

### Community 34 - "ProjectSessionBinding"
Cohesion: 0.22
Nodes (6): ProjectSessionBinding, BoundProjectPath, Task, Program, Fact, ProjectSessionBindingTests

### Community 35 - "TiaMcpServer.Tests"
Cohesion: 0.11
Nodes (18): net48, netstandard2.0, coverlet.collector (6.0.2), Microsoft.Extensions.Hosting (8.0.0), Microsoft.NET.Test.Sdk (17.10.0), System.Text.Json (10.0.4), xunit (2.9.0), xunit.runner.visualstudio (2.8.2) (+10 more)

### Community 38 - "CrossReferenceReader"
Cohesion: 0.17
Nodes (10): CrossReferenceService, Location, ReferenceObject, SourceObject, CrossReferenceFilter, IEnumerable, List, PlcSoftware (+2 more)

### Community 39 - "TiaMcpServer.Contracts"
Cohesion: 0.16
Nodes (3): TiaMcpServer.Contracts, TiaMcpServer.OpennessWorker, TiaMcpServer.OpennessWorker.Openness

### Community 40 - "OpcUaGenerationResult"
Cohesion: 0.11
Nodes (13): TiaMcpServer.OpennessWorker.Openness.OpcUa.SiemensGenerator, TiaMcpServer.OpennessWorker.Openness.OpcUa, IReadOnlyList, OpcUaGenerationResult, DataTypeNodeCount, DefaultNodeCount, GlobalDbNodeCount, Variables (+5 more)

### Community 41 - "tia-portal-mcp"
Cohesion: 0.15
Nodes (12): Model Context Protocol, Siemens TIA Openness User Group, Phase 1: Implementation, Phase 2: Universal Block Support, Phase 3: Hardware and Network Discovery, Phase 4: Advanced Diagnostics, Architecture, Block Paths (+4 more)

### Community 45 - ".Generate"
Cohesion: 0.17
Nodes (8): PlcBlock, PlcBlockGroup, DataBlocksGlobal, PlcBlockGroup, PlcTagTableGroup, PlcTypeSystemGroup, ISet, PlcSoftware

### Community 51 - ".FindAll"
Cohesion: 0.20
Nodes (11): DiscoveredPlcSoftware, SoftwareContainer, Device, DeviceItemComposition, IEnumerable, PlcSoftware, DiscoveredPlcSoftware, DeviceName (+3 more)

### Community 52 - "DeviceItemInfo"
Cohesion: 0.16
Nodes (11): List, DeviceItemInfo, Address, Items, Name, NetworkInterfaces, PositionNumber, TypeIdentifier (+3 more)

### Community 54 - "HardwareConfigInfo"
Cohesion: 0.24
Nodes (7): List, HardwareConfigInfo, Devices, Subnets, Fact, JsonSerializerOptions, HardwareConfigInfoTests

### Community 55 - "OpcUaNodeCatalogEntry"
Cohesion: 0.15
Nodes (12): IReadOnlyList, XDocument, OpcUaNodeCatalog, OpcUaNodeCatalogEntry, AccessLevel, BrowseName, DataType, DisplayName (+4 more)

### Community 90 - "CrossReferenceInfoTests"
Cohesion: 0.31
Nodes (3): Fact, JsonSerializerOptions, CrossReferenceInfoTests

### Community 91 - "SubnetInfo"
Cohesion: 0.15
Nodes (11): List, IoSystemInfo, ConnectedDeviceNames, IoControllerName, Name, List, SubnetInfo, ConnectedNodeNames (+3 more)

### Community 92 - "NodeInfo"
Cohesion: 0.15
Nodes (11): List, NetworkInterfaceInfo, Name, Nodes, NodeInfo, IoSystemName, IpAddress, Name (+3 more)

### Community 93 - "OpcUaToolTests"
Cohesion: 0.23
Nodes (8): DescriptionAttribute, Fact, InlineData, McpServerToolAttribute, McpServerToolTypeAttribute, Task, Theory, OpcUaToolTests

### Community 94 - "InterfaceTemplate"
Cohesion: 0.20
Nodes (7): Stream, IEnumerable, List, XElement, InterfaceTemplate, TagTableDataTypes, TemplateDataTypes

### Community 98 - "SIEMENS_SOURCE_LICENSE.md"
Cohesion: 0.18
Nodes (10): 1.1 Use of the Software, 1.2 Security information, 1.3 Compliance with Export Control Regulations, 1. General information: Software in source code and object code, 2. Open Source License for Software in source code and generated source code, 3.1 License Grant, 3.2 Included third-party software components, 3.3 Disclaimer of liability (+2 more)

### Community 99 - "BatchToolMetadataTests"
Cohesion: 0.33
Nodes (5): DescriptionAttribute, Fact, InlineData, Theory, BatchToolMetadataTests

### Community 100 - "PlcProgramExportToolTests"
Cohesion: 0.24
Nodes (7): DescriptionAttribute, Fact, InlineData, McpServerToolAttribute, Task, Theory, PlcProgramExportToolTests

### Community 101 - "CrossReferenceLocationInfo"
Cohesion: 0.20
Nodes (9): CrossReferenceLocationInfo, Access, Address, Name, ReferencedAs, ReferencedAsName, ReferenceLocation, ReferenceType (+1 more)

### Community 102 - ".TryNormalize"
Cohesion: 0.33
Nodes (3): ArchiveModeNames, Fact, ArchiveModeNamesTests

### Community 103 - "CrossReferenceSourceInfo"
Cohesion: 0.22
Nodes (8): List, CrossReferenceSourceInfo, Address, Children, Name, Path, References, TypeName

### Community 104 - "CrossReferenceTargetInfo"
Cohesion: 0.22
Nodes (8): List, CrossReferenceTargetInfo, Address, Device, Locations, Name, Path, TypeName

### Community 105 - "PlcCrossReferenceInfo"
Cohesion: 0.22
Nodes (8): List, PlcCrossReferenceInfo, LocationCount, Messages, PlcName, ReferenceCount, SourceCount, Sources

### Community 106 - "CrossReferenceReport"
Cohesion: 0.25
Nodes (7): List, CrossReferenceReport, Filter, Plcs, TotalLocationCount, TotalReferenceCount, TotalSourceCount

### Community 107 - "DeviceInfo"
Cohesion: 0.33
Nodes (5): List, DeviceInfo, Items, Name, TypeIdentifier

### Community 108 - "UserConstants"
Cohesion: 0.47
Nodes (3): Dictionary, PlcTagTableGroup, UserConstants

### Community 109 - "Requirements"
Cohesion: 0.40
Nodes (5): code:text (C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21), code:powershell (dotnet build TiaMcpServer.sln -m:1 /p:UseTiaPortalReferenceS), code:powershell (dotnet build TiaMcpServer.sln -m:1 /p:UseTiaPortalReferenceS), code:text (TIA Openness compile references: C:\Program Files\Siemens\Au), Requirements

### Community 110 - "Run Locally"
Cohesion: 0.40
Nodes (5): code:powershell (dotnet run --project TiaMcpServer), code:powershell ('{ "method": "browse_project_tree", "projectPath": null }' |), code:json ({"success":true,"payload":"[...]"}), code:json ({"success":false,"error":"No running TIA Portal V21 instance), Run Locally

### Community 111 - "Local Package Build"
Cohesion: 0.40
Nodes (5): code:powershell (dotnet pack TiaMcpServer\TiaMcpServer.csproj -c Release), code:powershell (dotnet tool install -g TiaMcpServer --add-source .\TiaMcpSer), code:powershell (tia-mcp), code:powershell (tia-mcp --project C:\Projects\Line.ap21), Local Package Build

### Community 112 - "MCP Client Configuration"
Cohesion: 0.50
Nodes (4): code:json ({), code:json ({), code:json ({), MCP Client Configuration

### Community 113 - "Install"
Cohesion: 0.50
Nodes (4): code:powershell (dotnet tool install -g TiaMcpServer), code:powershell (tia-mcp), code:powershell (tia-mcp --project C:\Projects\Line.ap21), Install

### Community 114 - "CrossReferenceFilterNames"
Cohesion: 0.50
Nodes (3): IReadOnlyList, CrossReferenceFilterNames, Default

### Community 115 - "Build From Source"
Cohesion: 0.67
Nodes (3): Build From Source, code:powershell (dotnet restore TiaMcpServer.sln), code:text (TiaMcpServer\bin\Debug\net8.0\openness-worker)

## Knowledge Gaps
- **380 isolated node(s):** `DeviceName`, `RootItemName`, `TypeIdentifier`, `Warnings`, `PlcName` (+375 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 688 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **48 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `TiaMcpServer.Contracts` connect `TiaMcpServer.Contracts` to `TiaMcpServer.Tests`, `BlockAddress`, `CompileChecker`, `TiaPortalSession`, `CatalogEntryInfo`, `ConfigureNetworkDeviceResultInfo`, `TagTableInfo`, `TagMutationResultInfo`, `ProjectTreeNode`, `AddDeviceResultInfo`, `ProjectSessionBinding`, `DeviceItemInfo`, `HardwareConfigInfo`, `CrossReferenceInfoTests`, `SubnetInfo`, `NodeInfo`, `CrossReferenceLocationInfo`, `.TryNormalize`, `CrossReferenceSourceInfo`, `CrossReferenceTargetInfo`, `PlcCrossReferenceInfo`, `CrossReferenceReport`, `DeviceInfo`, `CrossReferenceFilterNames`?**
  _High betweenness centrality (0.235) - this node is a cross-community bridge._
- **Why does `OpennessWorkerClient` connect `OpennessWorkerClient` to `ProjectSessionBinding`, `BatchOperationRequest`, `TiaMcpServer.Tests`?**
  _High betweenness centrality (0.098) - this node is a cross-community bridge._
- **Why does `WorkerRequest` connect `WorkerRequest` to `OpennessWorkerClient`, `CrossReferenceReader`, `TiaMcpServer.Contracts`?**
  _High betweenness centrality (0.083) - this node is a cross-community bridge._
- **What connects `DeviceName`, `RootItemName`, `TypeIdentifier` to the rest of the system?**
  _380 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WorkerRequest` be split into smaller, more focused modules?**
  _Cohesion score 0.052650494159928125 - nodes in this community are weakly interconnected._
- **Should `OpennessWorkerClient` be split into smaller, more focused modules?**
  _Cohesion score 0.06796116504854369 - nodes in this community are weakly interconnected._
- **Should `BatchOperationRequest` be split into smaller, more focused modules?**
  _Cohesion score 0.052313883299798795 - nodes in this community are weakly interconnected._