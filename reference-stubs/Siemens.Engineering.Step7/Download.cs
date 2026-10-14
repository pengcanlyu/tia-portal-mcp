// Compile-only V21 declarations. No Siemens implementation code.
#nullable disable
namespace Siemens.Engineering.Download.Configurations
{
    public abstract class TargetForSoftware : DownloadSelectionConfiguration
    {
        public TargetForSoftwareSelections CurrentSelection { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
    }
    public enum TargetForSoftwareSelections { CPU = 0, PlcSimulationAdvanced = 1 }
    public abstract class ConsistentBlocksDownload : DownloadSelectionConfiguration
    {
        public ConsistentBlocksDownloadSelections CurrentSelection { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
    }
    public enum ConsistentBlocksDownloadSelections { ConsistentDownload = 0 }
}
