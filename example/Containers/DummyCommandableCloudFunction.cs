using PipServices3.Commons.Refer;

namespace PipServices3.Gcp.Containers
{
    public class DummyCommandableCloudFunction: CommandableCloudFunction
    {
        public DummyCommandableCloudFunction() : base("dummy", "Dummy GCP function")
        {
            this._dependencyResolver.Put("controller", new Descriptor("pip-services-dummies", "controller", "default", "*", "*"));
            this._factories.Add(new DummyFactory());
        }
    }
}
