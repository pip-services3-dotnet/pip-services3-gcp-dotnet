using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Testing;
using PipServices3.Commons.Convert;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Gcp.Services
{
    public class DummyCloudFunctionFixture: IDisposable
    {
        protected FunctionTestServer server;
        protected HttpClient client;

        public DummyCloudFunctionFixture(Type function)
        {
            server = new FunctionTestServer(function);
            client = server.CreateClient();
        }

        public async Task TestCrudOperations()
        {
            var DUMMY1 = new Dummy(null, "Key 1", "Content 1");
            var DUMMY2 = new Dummy(null, "Key 2", "Content 2");

            string textResponse;

            // Create one dummy
            var body = new Dictionary<string, object>() { 
                { "cmd", "dummies.create_dummy" }, 
                { "dummy", DUMMY1 } 
            };

            textResponse = await ExecuteRequest(body);

            Dummy dummy1 = JsonConverter.FromJson<Dummy>(textResponse);
            Assert.Equal(dummy1.Content, DUMMY1.Content);
            Assert.Equal(dummy1.Key, DUMMY1.Key);

            // Create another dummy
            body = new Dictionary<string, object>() {
                { "cmd", "dummies.create_dummy" },
                { "dummy", DUMMY2 }
            };

            textResponse = await ExecuteRequest(body);

            Dummy dummy2 = JsonConverter.FromJson<Dummy>(textResponse);
            Assert.Equal(dummy2.Content, DUMMY2.Content);
            Assert.Equal(dummy2.Key, DUMMY2.Key);

            // Update the dummy
            dummy1.Content = "Updated Content 1";
            body = new Dictionary<string, object>() {
                { "cmd", "dummies.update_dummy" },
                { "dummy", dummy1 }
            };

            textResponse = await ExecuteRequest(body);

            Dummy updatedDummy1 = JsonConverter.FromJson<Dummy>(textResponse);
            Assert.Equal(updatedDummy1.Id, dummy1.Id);
            Assert.Equal(updatedDummy1.Content, dummy1.Content);
            Assert.Equal(updatedDummy1.Key, dummy1.Key);
            dummy1 = updatedDummy1;

            // Delete dummy
            body = new Dictionary<string, object>() {
                { "cmd", "dummies.delete_dummy" },
                { "dummy_id", dummy1.Id }
            };

            textResponse = await ExecuteRequest(body);

            Dummy deleted = JsonConverter.FromJson<Dummy>(textResponse);

            Assert.Equal(deleted.Id, dummy1.Id);
            Assert.Equal(deleted.Content, dummy1.Content);
            Assert.Equal(deleted.Key, dummy1.Key);

            // Try to get deleted dummy
            body = new Dictionary<string, object>() {
                { "cmd", "dummies.get_dummy_by_id" },
                { "dummy_id", dummy1.Id }
            };

            textResponse = await ExecuteRequest(body);

            Assert.True(string.IsNullOrEmpty(textResponse));

            // Failed validation
            body = new Dictionary<string, object>() {
                { "cmd", "dummies.create_dummy" },
                { "dummy", null }
            };

            textResponse = await ExecuteRequest(body, true);

            Assert.Contains("INVALID_DATA", textResponse);
        }

        private async Task<string> ExecuteRequest(Dictionary<string, object> data, bool errResponse = false)
        {
            var json = JsonConverter.ToJson(data);
            var request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(json)
            };

            HttpResponseMessage response = await client.SendAsync(request);
            if (!errResponse)
                Assert.True(((int)response.StatusCode) < 400);
            return await response.Content.ReadAsStringAsync();
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
