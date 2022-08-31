using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using PipServices3.Commons.Run;
using System;
using System.Text;
using System.Threading.Tasks;

namespace PipServices3.Gcp.Containers
{
    /// <summary>
    /// Class that helps to prepare function requests
    /// </summary>
    public class CloudFunctionRequestHelper
    {
        /// <summary>
        /// Returns correlationId from Google Function request.
        /// </summary>
        /// <param name="context">the Google Function context</param>
        /// <returns>returns correlationId from context</returns>
        public static string GetCorrelationId(HttpContext context)
        {
            var request = context.Request;

            var result = request.Query.TryGetValue("correlation_id", out StringValues correlationId)
                ? correlationId.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(result))
            {
                result = request.Headers.TryGetValue("correlation_id", out correlationId)
                    ? correlationId.ToString()
                    : null;
            }

            return result;
        }
        /// <summary>
        /// Returns command from Google Function context.
        /// </summary>
        /// <param name="context">the Google Function context</param>
        /// <returns>returns command from context</returns>
        public static async Task<string> GetCommand(HttpContext context)
        {
            string cmd = context.Request.Query.TryGetValue("cmd", out StringValues command)
                ? command.ToString()
                : "";
            try
            {
                if (string.IsNullOrWhiteSpace(cmd))
                {
                    var result = await context.Request.BodyReader.ReadAsync();
                    context.Request.BodyReader.AdvanceTo(result.Buffer.Start);
                    string body = EncodingExtensions.GetString(Encoding.UTF8, result.Buffer);

                    var parameters = string.IsNullOrEmpty(body) ? new Parameters() : Parameters.FromJson(body);
                    cmd = parameters.TryGetValue("cmd", out object bodyCommand) ? bodyCommand.ToString() : "";
                }

            } 
            catch
            {
                // Ignore the error
            }

            return cmd;
        }

        /// <summary>
        /// Returns body from Google Function request.
        /// </summary>
        /// <param name="context">the Google Function request</param>
        /// <returns>returns body from request</returns>
        public static async Task<Parameters> GetBodyAsync(HttpContext context)
        {
            string body = "";

            try
            {
                var result = await context.Request.BodyReader.ReadAsync();
                context.Request.BodyReader.AdvanceTo(result.Buffer.Start);
                body = EncodingExtensions.GetString(Encoding.UTF8, result.Buffer);
            } 
            catch
            {
                // Ignore the error
            }

            return string.IsNullOrEmpty(body) ? new Parameters() : Parameters.FromJson(body);
        }

        /// <summary>
        /// Returns body, query and header parameters from Google Function request.
        /// </summary>
        /// <param name="context">the Google Function request</param>
        /// <returns>returns body from request</returns>
        public static async Task<Parameters> GetParametersAsync(HttpContext context)
        {
            
            var result = await context.Request.BodyReader.ReadAsync();
            context.Request.BodyReader.AdvanceTo(result.Buffer.Start);

            string body = EncodingExtensions.GetString(Encoding.UTF8, result.Buffer);

            body = "{ \"body\":" + body + " }";

            var parameters = string.IsNullOrEmpty(body)
                ? new Parameters() : Parameters.FromJson(body);

            foreach (var pair in context.Request.Query)
                parameters.Set(pair.Key, pair.Value[0]);

            //foreach (var pair in request.Headers)
            //    parameters.Set(pair.Key, pair.Value[0]);

            return parameters;
        }

        /// <summary>
        /// Extracts parameter from query by key
        /// </summary>
        /// <param name="parameter">parameter name</param>
        /// <param name="context">context object</param>
        /// <returns>query param or empty</returns>
        public static string ExtractFromQuery(string parameter, HttpContext context)
        {
            return context.Request.Query.TryGetValue(parameter, out StringValues sortValues)
                ? sortValues.ToString()
                : string.Empty;
        }
    }
}
