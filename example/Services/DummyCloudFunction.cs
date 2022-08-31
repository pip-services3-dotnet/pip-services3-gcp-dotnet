using PipServices3.Gcp.Containers;

namespace PipServices3.Gcp.Services
{
    public class DummyCloudFunction: CloudFunction
    {
        public DummyCloudFunction(): base("dummy", "Dummy cloud function") 
        {
            this._factories.Add(new DummyFactory());
        }
    }
}
