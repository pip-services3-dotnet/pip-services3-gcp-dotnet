using PipServices3.Commons.Config;
using PipServices3.Commons.Refer;
using PipServices3.Components.Auth;
using PipServices3.Components.Connect;

using System;
using System.Threading.Tasks;

namespace PipServices3.Gcp.Connect
{
    /// <summary>
    /// Helper class to retrieve Google connection and credential parameters,
    /// validate them and compose a <see cref="GcpConnectionParams"/> value.
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
    /// - credentials:   
    ///     - account: the service account name
    ///     - auth_token:    Google-generated ID token or null if using custom auth(IAM)
    /// 
    /// ### References ###
    ///     - *:credential-store:*:*:1.0  (optional) Credential stores to resolve credentials
    /// 
    /// See <see cref="ConnectionParams"/> (in the Pip.Services components package)
    /// 
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// var config = GcpConnectionParams.FromTuples(
    ///     "connection.uri", "http://east-my_test_project.cloudfunctions.net/myfunction",
    ///     "connection.protocol", "http",
    ///     "connection.region", "east",
    ///     "connection.function", "myfunction",
    ///     "connection.project_id", "my_test_project",
    ///     "credential.auth_token", "1234"
    /// 
    /// );
    /// 
    /// var connectionResolver = new GcpConnectionResolver();
    /// connectionResolver.Configure(config);
    /// connectionResolver.SetReferences(references);
    /// 
    /// var connectionParams = await connectionResolver.ResolveAsync("123");
    /// 
    /// </code>
    /// </example>
    public class GcpConnectionResolver: IConfigurable, IReferenceable
    {
        /// <summary>
        /// The connection resolver.
        /// </summary>
        protected ConnectionResolver _connectionResolver = new();

        /// <summary>
        /// The credential resolver.
        /// </summary>
        protected CredentialResolver _credentialResolver = new();

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public void Configure(ConfigParams config)
        {
            _connectionResolver.Configure(config);
            _credentialResolver.Configure(config);
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies. </param>
        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        /// <summary>
        /// Resolves connection and credential parameters and generates a single
        /// GcpConnectionParams value.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <returns>GcpConnectionParams value or error.</returns>
        public async Task<GcpConnectionParams> ResolveAsync(string correlationId)
        {
            var connection = new GcpConnectionParams();

            var connectionParams = await this._connectionResolver.ResolveAsync(correlationId);
            connection.Append(connectionParams);

            var credentialParams = await this._credentialResolver.LookupAsync(correlationId);
            connection.Append(credentialParams);

            // Perform validation
            connection.Validate(correlationId);

            connection = this.ComposeConnection(connection);

            return connection;
        }

        private GcpConnectionParams ComposeConnection(GcpConnectionParams connection)
        {
            connection = GcpConnectionParams.MergeConfigs(connection);

            var uri = connection.Uri;

            if (string.IsNullOrEmpty(uri))
            {
                var protocol = connection.Protocol;
                var functionName = connection.Function;
                var projectId = connection.ProjectId;
                var region = connection.Region;
                // https://YOUR_REGION-YOUR_PROJECT_ID.cloudfunctions.net/FUNCTION_NAME
                uri = $"{protocol}://{region}-{projectId}.cloudfunctions.net" + (functionName != null ? "/" + functionName : "");

                connection.Uri = uri;
            }
            else
            {
                var address = new Uri(uri);
                var protocol = address.Scheme;
                var functionName = address.LocalPath.Replace("/", "");
                var region = uri.IndexOf('-') != -1 ? uri.Substring(uri.IndexOf("//") + 2, uri.IndexOf('-') - (uri.IndexOf("//") + 2)) : "";
                var projectId = uri.IndexOf('-') != -1 ? uri.Substring(uri.IndexOf('-') + 1, uri.IndexOf('.') - (uri.IndexOf('-') + 1)) : "";
                // let functionName = value.slice(-1) != '/' ? value.slice(value.lastIndexOf('/') + 1) : value.slice(value.slice(0, -1).lastIndexOf('/') + 1, -1);

                connection.Region = region;
                connection.ProjectId = projectId;
                connection.Function = functionName;
                connection.Protocol = protocol;
            }

            return connection;
        }
    }
}
