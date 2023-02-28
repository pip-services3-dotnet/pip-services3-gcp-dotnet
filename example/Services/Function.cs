using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Config;
using PipServices3.Gcp.Containers;
using System.Threading.Tasks;

namespace PipServices3.Gcp.Services
{
    public class Function : IHttpFunction
    {
        private static CloudFunction _service;
        private ConfigParams _config = ConfigParams.FromTuples(
            "logger.descriptor", "pip-services:logger:console:default:1.0",
            "service.descriptor", "pip-services-dummies:service:cloudfunc:default:1.0"
        );

        public async Task HandleAsync(HttpContext context)
        {
            if (_service == null)
            {
                _service = new DummyCloudFunction();
                _service.Configure(_config);
                await _service.OpenAsync(null);
            }

            await _service.GetHandler().Invoke(context);
        }
    }
}
