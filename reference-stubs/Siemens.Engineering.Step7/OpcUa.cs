// Compile-only V21 declarations. No Siemens implementation code.
#nullable disable
namespace Siemens.Engineering.SW.OpcUa
{
    public abstract class OpcUaProvider : IEngineeringService
    {
        object IEngineeringObject.GetAttribute(string name) => throw new System.NotSupportedException();
        void IEngineeringObject.SetAttribute(string name, object value) => throw new System.NotSupportedException();
        System.Collections.Generic.IList<EngineeringAttributeInfo> IEngineeringObject.GetAttributeInfos() => throw new System.NotSupportedException();
        IEngineeringObject IEngineeringInstance.Parent => throw new System.NotSupportedException();
        public OpcUaCommunicationGroup CommunicationGroup => throw new System.NotSupportedException();
    }
    public abstract class OpcUaCommunicationGroup
    {
        public ServerInterfaceGroup ServerInterfaceGroup => throw new System.NotSupportedException();
    }
    public abstract class ServerInterfaceGroup
    {
        public ServerInterfaceComposition ServerInterfaces => throw new System.NotSupportedException();
        public SimaticInterfaceComposition SimaticInterfaces => throw new System.NotSupportedException();
    }
    public abstract class ServerInterface
    {
        public string Name { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
        public string Author { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
        public bool Enabled { get => throw new System.NotSupportedException(); set => throw new System.NotSupportedException(); }
        public System.DateTime CreationTime => throw new System.NotSupportedException();
        public System.DateTime LastModified => throw new System.NotSupportedException();
        public void Import(System.IO.FileInfo file) => throw new System.NotSupportedException();
        public void Export(System.IO.FileInfo file) => throw new System.NotSupportedException();
        public void Delete() => throw new System.NotSupportedException();
    }
    public abstract class ServerInterfaceComposition : System.Collections.Generic.IEnumerable<ServerInterface>
    {
        public ServerInterface Create(string name) => throw new System.NotSupportedException();
        public System.Collections.Generic.IEnumerator<ServerInterface> GetEnumerator() => throw new System.NotSupportedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotSupportedException();
    }
    public abstract class SimaticInterface
    {
        public string Name => throw new System.NotSupportedException();
    }
    public abstract class SimaticInterfaceComposition : System.Collections.Generic.IEnumerable<SimaticInterface>
    {
        public System.Collections.Generic.IEnumerator<SimaticInterface> GetEnumerator() => throw new System.NotSupportedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new System.NotSupportedException();
    }
}
