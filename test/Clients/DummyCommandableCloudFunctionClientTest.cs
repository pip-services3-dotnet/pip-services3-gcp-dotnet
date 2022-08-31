using PipServices3.Commons.Config;
using PipServices3.Gcp.Services;
using System;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Gcp.Clients
{
    [Collection("Sequential")]
    public class DummyCommandableCloudFunctionClientTest : IDisposable
    {
        protected DummyCloudFunctionClient client;
        protected DummyClientFixture fixture;
        public DummyCommandableCloudFunctionClientTest()
        {
            var functionName = Environment.GetEnvironmentVariable("GCP_FUNCTION_NAME");
            var protocol = Environment.GetEnvironmentVariable("GCP_FUNCTION_PROTOCOL");
            var region = Environment.GetEnvironmentVariable("GCP_FUNCTION_REGION");
            var projectId = Environment.GetEnvironmentVariable("GCP_PROJECT_ID");
            var uri = Environment.GetEnvironmentVariable("GCP_FUNCTION_URI") ?? "http://localhost:3000";

            if (uri == null && (region == null || functionName == null || protocol == null || projectId == null))
                return;

            var config = ConfigParams.FromTuples(
                "connection.uri", uri,
                "connection.protocol", protocol,
                "connection.region", region,
                "connection.function", functionName,
                "connection.project_id", projectId
            );

            client = new DummyCloudFunctionClient();
            client.Configure(config);

            fixture = new DummyClientFixture(typeof(Function), client);

            client.OpenAsync(null).Wait();

            fixture.StartCloudServiceLocally("PipServices3.Gcp.Services.CommandableFunction");
        }

        [Fact]
        public async Task TestCrudOperations()
        {
            await fixture.TestCrudOperations();
        }

        public void Dispose()
        {
            client.CloseAsync(null).Wait();
            fixture.Dispose();
        }
    }
}
