using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Commands;
using PipServices3.Commons.Run;
using PipServices3.Gcp.Containers;
using PipServices3.Rpc.Services;
using System;
using System.Threading.Tasks;

using PipServices3.Components.Count;
using PipServices3.Components.Log;

namespace PipServices3.Gcp.Services
{
    /// <summary>
    /// Abstract service that receives commands via Google Function protocol
    /// to operations automatically generated for commands defined in <see cref="ICommandable"/> components.
    /// Each command is exposed as invoke method that receives command name and parameters.
    /// 
    /// Commandable services require only 3 lines of code to implement a robust external
    /// Google Function-based remote interface.
    /// 
    /// This service is intended to work inside Google Function container that
    /// exploses registered actions externally.
    /// 
    /// ### Configuration parameters ###
    ///     - dependencies:
    ///         - controller:            override for Controller dependency
    ///         
    /// ### References ###
    /// 
    ///     - *:logger:*:*:1.0              (optional) <see cref="ILogger"/> components to pass log messages
    ///     - *:counters:*:*:1.0            (optional) <see cref="ICounters"/> components to pass collected measurements
    /// 
    /// See <see cref="CloudFunctionService"/>
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// 
    /// class MyCommandableCloudFunctionService : CommandableCloudFunctionService
    /// {
    ///     private IMyController _controller;
    ///     // ...
    ///     public MyCommandableCloudFunctionService() : base()
    ///     {
    ///         this._dependencyResolver.Put(
    ///             "controller",
    ///             new Descriptor("mygroup", "controller", "*", "*", "1.0")
    ///         );
    ///     }
    /// }
    /// 
    /// ...
    /// 
    /// var service = new MyCommandableCloudFunctionService();
    /// service.SetReferences(References.FromTuples(
    ///    new Descriptor("mygroup", "controller", "default", "default", "1.0"), controller
    /// ));
    /// await service.OpenAsync("123");
    /// 
    /// Console.WriteLine("The Google Function service is running");
    /// 
    /// 
    /// </code>
    /// </example>
    public abstract class CommandableCloudFunctionService: CloudFunctionService
    {
        private CommandSet _commandSet;

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        /// <param name="name">a service name.</param>
        public CommandableCloudFunctionService(string name): base(name)
        {
            this._dependencyResolver.Put("controller", "none");
        }

        /// <summary>
        /// Creates a new instance of the service.
        /// </summary>
        public CommandableCloudFunctionService()
        {
            this._dependencyResolver.Put("controller", "none");
        }

        /// <summary>
        /// Returns body from Google Function context.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <param name="context">Google Function context</param>
        /// <returns>Returns Parameters from context</returns>
        protected async Task<Parameters> GetParametrs(HttpContext context)
        {
            return await CloudFunctionRequestHelper.GetBodyAsync(context);
        }

        /// <summary>
        /// Registers all actions in Google Function.
        /// </summary>
        protected override void Register()
        {
            ICommandable controller = this._dependencyResolver.GetOneRequired<ICommandable>("controller");
            _commandSet = controller.GetCommandSet();

            var commands = this._commandSet.Commands;
            for (var index = 0; index < commands.Count; index++)
            {
                var command = commands[index];
                var name = command.Name;

                this.RegisterAction(name, null, async (context) => {
                    var correlationId = this.GetCorrelationId(context);
                    var args = await this.GetParametrs(context);
                    args.Remove("correlation_id");

                    try
                    {
                        using var timing = Instrument(correlationId, name);
                        var result = await command.ExecuteAsync(correlationId, args);
                        await HttpResponseSender.SendResultAsync(context.Response, result);
                    }
                    catch (Exception ex)
                    {
                        InstrumentError(correlationId, name, ex);
                        await HttpResponseSender.SendErrorAsync(context.Response, ex);
                    }
                });
            }
        }
    }
}
