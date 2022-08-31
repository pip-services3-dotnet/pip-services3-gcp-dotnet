using PipServices3.Commons.Refer;

namespace PipServices3.Gcp.Services
{
    public class DummyCommandableCloudFunctionService: CommandableCloudFunctionService
    {
        public DummyCommandableCloudFunctionService(): base("dummies")
        {
            this._dependencyResolver.Put("controller", new Descriptor("pip-services-dummies", "controller", "default", "*", "*"));
        }
    }
}
