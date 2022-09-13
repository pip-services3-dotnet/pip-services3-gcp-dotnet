using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using PipServices3.Commons.Config;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Validate;
using PipServices3.Gcp.Containers;
using PipServices3.Gcp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipServices3.Gcp
{
    public class MyCloudFunctionService : CloudFunctionService
    {
        private IMyController _controller;

        public MyCloudFunctionService(IMyController controller) : base("v1.myservice")
        {
            _controller = controller;

            this._dependencyResolver.Put("controller", new Descriptor("mygroup", "controller", "*", "*", "1.0"));
        }

        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);
            _controller = _dependencyResolver.GetRequired<IMyController>("controller");
        }

        public static void m()
        {
            var service = new MyCloudFunctionService(controller);
            service.Configure(ConfigParams.FromTuples(
                "connection.protocol", "http",
            "connection.host", "localhost",
                "connection.port", 8080
            ));
            service.SetReferences(References.FromTuples(
               new Descriptor("mygroup", "controller", "default", "default", "1.0"), controller
            ));

            await service.OpenAsync("123");
        }

        protected override void Register()
        {
            RegisterAction("get_dummies", new ObjectSchema()
                .WithOptionalProperty("body",
                    new ObjectSchema()
                        .WithOptionalProperty("filter", new FilterParamsSchema())
                        .WithOptionalProperty("paging", new PagingParamsSchema())
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                async (req) =>
                {
                    var correlationId = GetCorrelationId(req);
                    var body = await CloudFunctionRequestHelper.GetBodyAsync(req);
                    var id = body.GetAsString("id");
                    return await this._controller.getMyData(correlationId, id);
                }
            );
        }
    }
}
