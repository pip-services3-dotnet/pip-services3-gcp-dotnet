using Microsoft.AspNetCore.Http;
using System;

using PipServices3.Commons.Commands;
using PipServices3.Commons.Run;
using PipServices3.Rpc.Services;
using System.Threading.Tasks;

using PipServices3.Components.Log;
using PipServices3.Components.Count;
using PipServices3.Gcp.Services;

namespace PipServices3.Gcp.Containers
{
    /// <summary>
    /// Abstract Google Function function, that acts as a container to instantiate and run components
    /// and expose them via external entry point. All actions are automatically generated for commands
    /// defined in <see cref="ICommandable"/> components. Each command is exposed as an action defined by "cmd" parameter.
    /// 
    /// Container configuration for this Google Function is stored in <code>"./config/config.yml"</code> file.
    /// But this path can be overridden by <code>CONFIG_PATH</code> environment variable.
    /// 
    /// Note: This component has been deprecated. Use CloudFunctionService instead.
    /// 
    /// ### References ###
    ///     - *:logger:*:*:1.0                              (optional) <see cref="ILogger"/> components to pass log messages
    ///     - *:counters:*:*:1.0                            (optional) <see cref="ICounters"/> components to pass collected measurements
    ///     - *:service:gcp-function:*:1.0                  (optional) <see cref="CloudFunctionService"/> services to handle action requests
    ///     - *:service:commandable-gcp-function:*:1.0      (optional) <see cref="CloudFunctionService"/> services to handle action requests
    /// 
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// class MyCloudFunction : CommandableCloudFunction
    /// {
    ///     private IMyController _controller;
    ///     ...
    ///     public MyCloudFunction() : base("mygroup", "MyGroup CloudFunction")
    ///     {
    /// 
    ///         this._dependencyResolver.Put(
    ///             "controller",
    ///             new Descriptor("mygroup", "controller", "*", "*", "1.0")
    ///         );
    ///     }
    /// }
    /// 
    /// ...
    /// 
    /// var cloudFunction = new MyCloudFunction();
    /// 
    /// await cloudFunction.RunAsync();
    /// Console.WriteLine("MyCloudFunction is started");
    /// 
    /// </code>
    /// </example>
    public class CommandableCloudFunction: CloudFunction
    {
        /// <summary>
        /// Creates a new instance of this Google Function.
        /// </summary>
        /// <param name="name">(optional) a container name (accessible via ContextInfo)</param>
        /// <param name="description">(optional) a container description (accessible via ContextInfo)</param>
        public CommandableCloudFunction(string name, string description = null): base(name, description)
        {
            this._dependencyResolver.Put("controller", "none");
        }

        /// <summary>
        /// Creates a new instance of this Google Function.
        /// </summary>
        public CommandableCloudFunction(): this(null, null)
        {
        }

        /// <summary>
        /// Returns body from Google Function request.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <returns></returns>
        protected async Task<Parameters> GetParameters(HttpContext context)
        {
            return await CloudFunctionRequestHelper.GetBodyAsync(context);
        }

        private void RegisterCommandSet(CommandSet commandSet)
        {
            var commands = commandSet.Commands;

            for (var index = 0; index < commands.Count; index++)
            {
                var command = commands[index];

                RegisterAction(command.Name, null, async (context) => {
                    var correlationId = this.GetCorrelationId(context);
                    var args = await this.GetParameters(context);
                    
                    try
                    {
                        using var timing = this.Instrument(correlationId, _info.Name + '.' + command.Name);
                        var result = await command.ExecuteAsync(correlationId, args);
                        await HttpResponseSender.SendResultAsync(context.Response, result);
                    }
                    catch (Exception ex)
                    {
                        InstrumentError(correlationId, _info.Name + '.' + command.Name, ex);
                        await HttpResponseSender.SendErrorAsync(context.Response, ex);
                    }
                });
            }
        }

        /// <summary>
        /// Registers all actions in this Google Function.
        /// </summary>
        [Obsolete("Overloading of this method has been deprecated. Use CloudFunctionService instead.", false)]
        protected override void Register()
        {
            ICommandable controller = this._dependencyResolver.GetOneRequired<ICommandable>("controller");
            var commandSet = controller.GetCommandSet();
            this.RegisterCommandSet(commandSet);
        }
    }
}
