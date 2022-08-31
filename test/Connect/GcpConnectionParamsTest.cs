using PipServices3.Commons.Config;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Gcp.Connect
{
    public class GcpConnectionParamsTest
    {
        [Fact]
        public void TestEmptyConnection()
        {
            var connection = new GcpConnectionParams();
            Assert.Null(connection.Uri);
            Assert.Null(connection.ProjectId);
            Assert.Null(connection.Function);
            Assert.Null(connection.Region);
            Assert.Null(connection.Protocol);
            Assert.Null(connection.AuthToken);
        }

        [Fact]
        public async Task TestComposeConfig()
        {
            var config1 = ConfigParams.FromTuples(
                "connection.uri", "http://east-my_test_project.cloudfunctions.net/myfunction",
                "credential.auth_token", "1234"
            );
            var config2 = ConfigParams.FromTuples(
                "connection.protocol", "http",
                "connection.region", "east",
                "connection.function", "myfunction",
                "connection.project_id", "my_test_project",
                "credential.auth_token", "1234"
    
            );
            var resolver = new GcpConnectionResolver();
            resolver.Configure(config1);
            var connection = await resolver.ResolveAsync("");

            Assert.Equal("http://east-my_test_project.cloudfunctions.net/myfunction", connection.Uri);
            Assert.Equal("east", connection.Region);
            Assert.Equal("http", connection.Protocol);
            Assert.Equal("myfunction", connection.Function);
            Assert.Equal("my_test_project", connection.ProjectId);
            Assert.Equal("1234", connection.AuthToken);

            resolver = new GcpConnectionResolver();
            resolver.Configure(config2);
            connection = await resolver.ResolveAsync("");

            Assert.Equal("http://east-my_test_project.cloudfunctions.net/myfunction", connection.Uri);
            Assert.Equal("east", connection.Region);
            Assert.Equal("http", connection.Protocol);
            Assert.Equal("myfunction", connection.Function);
            Assert.Equal("my_test_project", connection.ProjectId);
            Assert.Equal("1234", connection.AuthToken);
        }
    }
}
