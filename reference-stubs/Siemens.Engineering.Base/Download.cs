// Compile-only V21 declarations. No Siemens implementation code.
#nullable disable
namespace Siemens.Engineering.Download
{
    public delegate void DownloadConfigurationDelegate(Configurations.DownloadConfiguration configuration);
    public abstract class DownloadProvider : IEngineeringService
    {
        object IEngineeringObject.GetAttribute(string name) => throw new System.NotSupportedException();
        void IEngineeringObject.SetAttribute(string name, object value) => throw new System.NotSupportedException();
        System.Collections.Generic.IList<EngineeringAttributeInfo> IEngineeringObject.GetAttributeInfos() => throw new System.NotSupportedException();
        IEngineeringObject IEngineeringInstance.Parent => throw new System.NotSupportedException();
        public DownloadResult Download(System.IO.DirectoryInfo directory, DownloadConfigurationDelegate configuration) => throw new System.NotSupportedException();
    }
    public abstract class DownloadResult
    {
        public int ErrorCount => throw new System.NotSupportedException();
        public int WarningCount => throw new System.NotSupportedException();
    }
}
namespace Siemens.Engineering.Download.Configurations
{
    public abstract class DownloadConfiguration { }
    public abstract class DownloadSelectionConfiguration : DownloadConfiguration { }
    public abstract class OverwriteOnMemoryCard : DownloadSelectionConfiguration
    {
        public OverwriteOnMemoryCardSelections CurrentSelection { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
    }
    public enum OverwriteOnMemoryCardSelections { NoAction = 0, Load = 1 }
    public abstract class AlarmTextLibrariesDownload : DownloadSelectionConfiguration
    {
        public AlarmTextLibrariesDownloadSelections CurrentSelection { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
    }
    public enum AlarmTextLibrariesDownloadSelections { ConsistentDownload = 0, NoAction = 1 }
}
