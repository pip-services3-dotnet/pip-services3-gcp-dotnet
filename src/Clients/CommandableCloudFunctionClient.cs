using System;
using System.Threading.Tasks;
using PipServices3.Commons.Commands;
using PipServices3.Gcp.Containers;

namespace PipServices3.Gcp.Clients
{
    /// <summary>
    /// Abstract client that calls commandable Google Cloud Functions.
    /// 
    /// Commandable services are generated automatically for <see cref="ICommandable"/> objects.
    /// Each command is exposed as action determined by "cmd" parameter.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - connections:                   
    ///     - uri:           full connection uri with specific app and function name
    ///     - protocol:      connection protocol
    ///     - project_id:    is your Google Cloud Platform project ID
    ///     - region:        is the region where your function is deployed
    ///     - function:      is the name of the HTTP function you deployed
    ///     - org_id:        organization name 
    /// - options:
    ///     - retries:               number of retries(default: 3)
    ///     - connect_timeout:       connection timeout in milliseconds(default: 10 sec)
    ///     - timeout:               invocation timeout in milliseconds(default: 10 sec)
    /// - credentials:   
    ///     - account: the service account name
    ///     - auth_token:    Google-generated ID token or null if using custom auth(IAM)
    /// 
    /// ### References ###
    ///     - *:logger:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    ///     - *:counters:*:*:1.0         (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_count_1_1_i_counters.html">ICounters</a> components to pass collected measurements
    ///     - *:discovery:*:*:1.0        (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services to resolve connection
    ///     - *:credential-store:*:*:1.0  (optional) Credential stores to resolve credentials
    ///     
    /// See <see cref="CloudFunction"/>
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// class MyCommandableGoogleClient : CommandableCloudFunctionClient, IMyClient
    /// {
    /// ...
    /// 
    ///     public async Task<MyData> GetDataAsync(string correlationId, string id) {
    ///         return await this.CallCommand<MyData>("get_data", correlationId, new { id=id });
    ///     }
    ///     ...
    /// 
    ///     public async Task Main()
    ///     {
    ///         var client = new MyCommandableGoogleClient();
    ///         client.Configure(ConfigParams.FromTuples(
    ///             "connection.uri", "http://region-id.cloudfunctions.net/myfunction",
    ///             "connection.protocol", "http",
    ///             "connection.region", "region",
    ///             "connection.function", "myfunction",
    ///             "connection.project_id", "id",
    ///             "credential.auth_token", "XXX"
    ///         ));
    /// 
    ///         var  result = await client.GetDataAsync("123", "1");
    ///     }
    /// }
    /// 
    /// 
    /// </code>
    /// </example>
    public class CommandableCloudFunctionClient: CloudFunctionClient
    {
        private readonly string _name;

        /// <summary>
        /// Creates a new instance of this client.
        /// </summary>
        /// <param name="name">a service name.</param>
        public CommandableCloudFunctionClient(string name): base()
        {
            this._name = name;
        }

        /// <summary>
        /// Calls a remote action in Google Function.
        /// The name of the action is added as "cmd" parameter
        /// to the action parameters. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cmd">an action name</param>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="args">command parameters.</param>
        /// <returns>action result.</returns>
        public async Task<T> CallCommand<T>(string cmd, string correlationId, object args)
            where T : class
        {
            var timing = Instrument(correlationId, this._name + '.' + cmd);
            try
            {
                return await CallAsync<T>(correlationId, correlationId, args);
            }
            catch (Exception ex)
            {
                InstrumentError(correlationId, this._name + '.' + cmd, ex);
                throw;
            }
            finally
            {
                timing.EndTiming();
            }
        }
    }
}
