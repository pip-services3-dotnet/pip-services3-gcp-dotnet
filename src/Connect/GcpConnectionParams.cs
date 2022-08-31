using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Components.Auth;
using System.Collections.Generic;

namespace PipServices3.Gcp.Connect
{
    /// <summary>
    /// Contains connection parameters to authenticate against Google
    /// and connect to specific Google Cloud Platform.
    /// 
    /// The class is able to compose and parse Google Platform connection parameters.
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
    ///
    /// - credentials:   
    ///     - account: the service account name
    ///     - auth_token:    Google-generated ID token or null if using custom auth(IAM)
    /// 
    /// In addition to standard parameters <see cref="CredentialParams"/> may contain any number of custom parameters
    /// 
    /// See <see cref="GcpConnectionResolver"/>
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// var connection = GcpConnectionParams.FromTuples(
    ///     "connection.uri", "http://east-my_test_project.cloudfunctions.net/myfunction",
    ///     "connection.protocol", "http",
    ///     "connection.region", "east",
    ///     "connection.function", "myfunction",
    ///     "connection.project_id", "my_test_project",
    ///     "credential.auth_token", "1234"
    /// 
    /// );
    /// var uri = connection.Uri;                     // Result: 'http://east-my_test_project.cloudfunctions.net/myfunction'
    /// var region = connection.Region;               // Result: 'east'
    /// var protocol = connection.Protocol;           // Result: 'http'
    /// var functionName = connection.Function;       // Result: 'myfunction'
    /// var projectId = connection.ProjectId;         // Result: 'my_test_project'
    /// var authToken = connection.AuthToken;         // Result: '123'
    /// 
    /// 
    /// </code>
    /// </example>
    public class GcpConnectionParams : ConfigParams
    {
        public GcpConnectionParams() { }

        /// <summary>
        /// Creates an new instance of the connection parameters.
        /// </summary>
        /// <param name="values"> (optional) an object to be converted into key-value pairs to initialize this connection.</param>
        public GcpConnectionParams(IDictionary<string, string> values) : base(values)
        {

        }

        /// <summary>
        /// Gets or sets the Google Platform service connection protocol.
        /// </summary>
        public string Protocol
        {
            get { return GetAsNullableString("protocol"); }
            set { base.Set("protocol", value); }
        }

        /// <summary>
        /// Gets or sets the Google Platform service uri.
        /// </summary>
        public string Uri
        {
            get { return GetAsNullableString("uri"); }
            set { base.Set("uri", value); }
        }

        /// <summary>
        /// Gets or sets the Google function name.
        /// </summary>
        public string Function
        {
            get { return GetAsNullableString("function"); }
            set { base.Set("function", value); }
        }

        /// <summary>
        /// Gets the region where your function is deployed.
        /// </summary>
        public string Region
        {
            get { return GetAsNullableString("region"); }
            set { base.Set("region", value); }
        }

        /// <summary>
        /// Gets or sets the Google Cloud Platform project ID.
        /// </summary>
        public string ProjectId
        {
            get { return GetAsNullableString("project_id"); }
            set { base.Set("project_id", value); }
        }
        /// <summary>
        ///  Gets or sets an ID token with the request to authenticate themselves
        /// </summary>
        public string AuthToken
        {
            get { return GetAsNullableString("auth_token"); }
            set { base.Set("auth_token", value); }
        }

        /// <summary>
        /// Sets or gets an ID token with the request to authenticate themselves
        /// </summary>
        public string Account
        {
            get { return GetAsNullableString("account"); }
            set { base.Set("account", value);  }
        }

        /// <summary>
        /// Gets or sets organization name
        /// </summary>
        public string OrgId
        {
            get { return GetAsNullableString("org_id"); }
            set { base.Set("org_id", value); }
        }

        /// <summary>
        /// Validates this connection parameters 
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <exception cref="ConfigException"></exception>
        public void Validate(string correlationId)
        {
            string uri = Uri;
            string protocol = Protocol;
            string functionName = Function;
            string region = Region;
            string projectId = ProjectId;

            if (uri == null && (projectId == null && region == null && functionName == null && protocol == null))
            {
                throw new ConfigException(
                    correlationId,
                    "NO_CONNECTION_URI",
                    "No uri, project_id, region and function is configured in Google function uri"
                );
            }

            if (protocol != null && "http" != protocol && "https" != protocol)
            {
                throw new ConfigException(
                    correlationId, "WRONG_PROTOCOL", "Protocol is not supported by REST connection")
                    .WithDetails("protocol", protocol);
            }
        }

        /// <summary>
        /// Creates a new GcpConnectionParams object filled with key-value pairs serialized as a string.
        /// </summary>
        /// <param name="line">a string with serialized key-value pairs as "key1=value1;key2=value2;..."
        /// Example: "Key1=123;Key2=ABC;Key3=2016-09-16T00:00:00.00Z"</param>
        /// <returns>a new GcpConnectionParams object.</returns>
        public static new GcpConnectionParams FromString(string line)
        {
            var map = StringValueMap.FromString(line);
            return new GcpConnectionParams(map);
        }

        /// <summary>
        /// Retrieves GcpConnectionParams from configuration parameters.
        /// The values are retrieves from "connection" and "credential" sections.
        /// </summary>
        /// <param name="config">configuration parameters</param>
        /// <returns>the generated GcpConnectionParams object.</returns>
        public static GcpConnectionParams FromConfig(ConfigParams config)
        {
            var result = new GcpConnectionParams();

            var credentials = CredentialParams.ManyFromConfig(config);
            foreach (var credential in credentials)
                result.Append(credential);

            var connections = CredentialParams.ManyFromConfig(config);
            foreach (var connection in connections)
                result.Append(connection);

            return result;
        }


        /// <summary>
        /// Creates a new ConfigParams object filled with provided key-value pairs called tuples.
        /// Tuples parameters contain a sequence of key1, value1, key2, value2, ... pairs.
        /// </summary>
        /// <param name="tuples">the tuples to fill a new ConfigParams object.</param>
        /// <returns>a new ConfigParams object.</returns>
        public static new GcpConnectionParams FromTuples(params object[] tuples)
        {
            var config = ConfigParams.FromTuples(tuples);
            return GcpConnectionParams.FromConfig(config);
        }

        /// <summary>
        /// Retrieves GcpConnectionParams from multiple configuration parameters.
        /// The values are retrieves from "connection" and "credential" sections.
        /// </summary>
        /// <param name="configs">a list with configuration parameters</param>
        /// <returns>the generated GcpConnectionParams object.</returns>
        public static GcpConnectionParams MergeConfigs(params ConfigParams[] configs)
        {
            var config = ConfigParams.MergeConfigs(configs);
            return new GcpConnectionParams(config);
        }
    }
}
