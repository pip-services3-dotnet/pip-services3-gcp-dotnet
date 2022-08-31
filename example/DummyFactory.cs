using PipServices3.Commons.Refer;
using PipServices3.Components.Build;
using PipServices3.Gcp.Services;

namespace PipServices3.Gcp
{
    public class DummyFactory: Factory
    {
        public static readonly Descriptor Descriptor = new Descriptor("pip-services-dummies", "factory", "default", "default", "1.0");
        public static readonly Descriptor ControllerDescriptor = new Descriptor("pip-services-dummies", "controller", "default", "*", "1.0");
        public static readonly Descriptor CloudFunctionServiceDescriptor = new Descriptor("pip-services-dummies", "service", "gcp-function", "*", "1.0");
        public static readonly Descriptor CmdCloudFunctionServiceDescriptor = new Descriptor("pip-services-dummies", "service", "commandable-gcp-function", "*", "1.0");
    
        public DummyFactory(): base()
        {
            this.RegisterAsType(DummyFactory.ControllerDescriptor, typeof(DummyController));
            this.RegisterAsType(DummyFactory.CloudFunctionServiceDescriptor, typeof(DummyCloudFunctionService));
            this.RegisterAsType(DummyFactory.CmdCloudFunctionServiceDescriptor, typeof(DummyCommandableCloudFunctionService));
        }
    }
}
