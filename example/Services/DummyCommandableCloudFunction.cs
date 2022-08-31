using PipServices3.Commons.Refer;
using PipServices3.Gcp.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipServices3.Gcp.Services
{
    public class DummyCommandableCloudFunction: CommandableCloudFunction
    {
        public DummyCommandableCloudFunction(): base("dummy", "Dummy commandable cloud function")
        {
            this._dependencyResolver.Put("controller", new Descriptor("pip-services-dummies", "controller", "default", "*", "*"));
            this._factories.Add(new DummyFactory());
        }
    }
}
