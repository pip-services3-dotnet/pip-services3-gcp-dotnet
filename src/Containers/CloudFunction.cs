using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Validate;
using PipServices3.Components.Count;
using PipServices3.Components.Log;
using PipServices3.Components.Trace;
using PipServices3.Gcp.Services;
using PipServices3.Rpc.Services;
using PipContainer = PipServices3.Container;

namespace PipServices3.Gcp.Containers
{
    /// <summary>
    /// Abstract Google Function, that acts as a container to instantiate and run components
    /// and expose them via external entry point.
    /// 
    /// When handling calls "cmd" parameter determines which what action shall be called, while
    /// other parameters are passed to the action itself.
    /// 
    /// Container configuration for this Google Function is stored in <code>"./config/config.yml"</code> file.
    /// But this path can be overriden by <code>CONFIG_PATH</code> environment variable.
    /// 
    /// ### References ###
    ///     - *:logger:*:*:1.0                              (optional) <see cref="ILogger"/> components to pass log messages
    ///     - *:counters:*:*:1.0                            (optional) <see cref="ICounters"/> components to pass collected measurements
    ///     - *:service:gcp-function:*:1.0                  (optional) <see cref="ICloudFunctionService"/> services to handle action requests
    ///     - *:service:commandable-gcp-function:*:1.0      (optional) <see cref="ICloudFunctionService"/> services to handle action requests
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// class MyCloudFunction : CloudFunction
    /// {
    ///     public MyCloudFunction() : base("mygroup", "MyGroup Google Function")
    ///     {
    /// 
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
    public abstract class CloudFunction : PipContainer.Container
    {
        private readonly ManualResetEvent _exitEvent = new ManualResetEvent(false);

        /// <summary>
        /// The performanc counters.
        /// </summary>
        protected CompositeCounters _counters = new CompositeCounters();

        /// <summary>
        /// The tracer.
        /// </summary>
        protected CompositeTracer _tracer = new CompositeTracer();

        /// <summary>
        /// The dependency resolver.
        /// </summary>
        protected DependencyResolver _dependencyResolver = new DependencyResolver();

        /// <summary>
        /// The map of registred validation schemas.
        /// </summary>
        protected Dictionary<string, Schema> _schemas = new();

        /// <summary>
        /// The map of registered actions.
        /// </summary>
        protected Dictionary<string, Func<HttpContext, Task>> _actions = new();

        /// <summary>
        /// The default path to config file.
        /// </summary>
        protected string _configPath = "../config/config.yml";

        public CloudFunction(string name = null, string description = null) : base(name, description)
        {
            this._logger = new ConsoleLogger();
        }

        private string GetConfigPath()
        {
            return Environment.GetEnvironmentVariable("CONFIG_PATH") ?? this._configPath;
        }

        private ConfigParams GetParameters()
        {
            return ConfigParams.FromValue(Environment.GetEnvironmentVariables());
        }

        private void CaptureErrors(string correlationId)
        {
            AppDomain.CurrentDomain.UnhandledException += (obj, e) =>
            {
                _logger.Fatal(correlationId, e.ExceptionObject.ToString(), "Process is terminated");
                _exitEvent.Set();
            };
        }

        private void CaptureExit(string correlationId)
        {
            _logger.Info(correlationId, "Press Control-C to stop the microservice...");

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _logger.Info(correlationId, "Goodbye!");

                eventArgs.Cancel = true;
                _exitEvent.Set();

                Environment.Exit(1);
            };

            // Wait and close
            _exitEvent.WaitOne();
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies. </param>
        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);
            this._counters.SetReferences(references);
            this._dependencyResolver.SetReferences(references);

            Register();
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public new async Task OpenAsync(string correlationId)
        {
            if (this.IsOpen()) return;

            await base.OpenAsync(correlationId);
            this.RegisterServices();
        }

        /// <summary>
        /// Adds instrumentation to log calls and measure call time. It returns a CounterTiming
        /// object that is used to end the time measurement.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <returns>CounterTiming object to end the time measurement.</returns>
        protected CounterTiming Instrument(string correlationId, string methodName)
        {
            _logger.Trace(correlationId, "Executing {0} method", methodName);
            _counters.IncrementOne(methodName + ".exec_count");
            return _counters.BeginTiming(methodName + ".exec_time");
        }

        /// <summary>
        /// Adds instrumentation to error handling.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="methodName">a method name.</param>
        /// <param name="ex">Error that occured during the method call</param>
        /// <param name="rethrow">True to throw the exception</param>
        protected void InstrumentError(string correlationId, string methodName, Exception ex, bool rethrow = false)
        {
            _logger.Error(correlationId, ex, "Failed to execute {0} method", methodName);
            _counters.IncrementOne(methodName + ".exec_errors");

            if (rethrow)
                throw ex;
        }

        /// <summary>
        /// Runs this Google Function, loads container configuration,
        /// instantiate components and manage their lifecycle,
        /// makes this function ready to access action calls.
        /// </summary>
        /// <param name="args">command line arguments</param>
        public async Task RunAsync()
        {
            var correlationId = _info.Name;
            var path = GetConfigPath();
            var parameters = GetParameters();
            this.ReadConfigFromFile(correlationId, path, parameters);

            CaptureErrors(correlationId);
            await OpenAsync(correlationId);
            CaptureExit(correlationId);
            await CloseAsync(correlationId);
        }

        /// <summary>
        /// Registers all actions in this Google Function.
        /// 
        /// Note: Overloading of this method has been deprecated. Use CloudFunctionService instead.
        /// </summary>
        [Obsolete("Overloading of this method has been deprecated. Use CloudFunctionService instead.", false)]
        protected virtual void Register() { }

        /// <summary>
        /// Registers all Google Function services in the container.
        /// </summary>
        protected void RegisterServices()
        {
            // Extract regular and commandable Google Function services from references
            var services = this._references.GetOptional<ICloudFunctionService>(
                new Descriptor("*", "service", "gcp-function", "*", "*")
            );
            var cmdServices = this._references.GetOptional<ICloudFunctionService>(
                new Descriptor("*", "service", "commandable-gcp-function", "*", "*")
            );

            services.AddRange(cmdServices);

            // Register actions defined in those services
            foreach (var service in services)
            {

                var actions = service.GetActions();
                foreach (var action in actions)
                {
                    RegisterAction(action.Cmd, action.Schema, action.Action);
                }
            }
        }

        /// <summary>
        /// Registers an action in this Google Function.
        /// 
        /// Note: This method has been deprecated. Use CloudFunctionService instead.
        /// </summary>
        /// <param name="cmd">a action/command name.</param>
        /// <param name="schema">a validation schema to validate received parameters.</param>
        /// <param name="action">an action function that is called when action is invoked.</param>
        /// <exception cref="UnknownException"></exception>
        protected void RegisterAction(string cmd, Schema schema, Func<HttpContext, Task> action)
        {
            if (string.IsNullOrEmpty(cmd))
                throw new UnknownException(null, "NO_COMMAND", "Missing command");

            if (action == null)
                throw new UnknownException(null, "NO_ACTION", "Missing action");

            if (this._actions.ContainsKey(cmd))
                throw new UnknownException(null, "DUPLICATED_ACTION", cmd + "action already exists");

            Func<HttpContext, Task> actionCurl = async (context) =>
            {
                // Perform validation
                if (schema != null)
                {
                    var param = await CloudFunctionRequestHelper.GetParametersAsync(context);
                    var correlationId = GetCorrelationId(context);
                    var err = schema.ValidateAndReturnException(correlationId, param, false);
                    if (err != null)
                    {
                        await HttpResponseSender.SendErrorAsync(context.Response, err);
                        return;
                    }
                }

                // Todo: perform verification?
                await action(context);
            };

            this._actions[cmd] = actionCurl;
        }

        /// <summary>
        /// Returns correlationId from Googel Function request.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <param name="context">Googel Function request</param>
        /// <returns>Returns correlationId from request</returns>
        protected string GetCorrelationId(HttpContext context)
        {
            return CloudFunctionRequestHelper.GetCorrelationId(context);
        }

        /// <summary>
        /// Returns command from Google Function request.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <param name="context">Google Function request</param>
        /// <returns>Returns command from request</returns>
        protected async Task<string> GetCommandAsync(HttpContext context)
        {
            return await CloudFunctionRequestHelper.GetCommand(context);
        }

        /// <summary>
        /// Executes this Google Function and returns the result.
        /// This method can be overloaded in child classes
        /// if they need to change the default behavior
        /// </summary>
        /// <param name="context">the context function</param>
        /// <returns>task</returns>
        /// <exception cref="BadRequestException"></exception>
        protected async Task ExecuteAsync(HttpContext context)
        {
            string cmd = await GetCommandAsync(context);
            string correlationId = GetCorrelationId(context);

            if (string.IsNullOrEmpty(cmd))
            {
                throw new BadRequestException(
                    correlationId,
                    "NO_COMMAND",
                    "Cmd parameter is missing"
                );
            }

            var action = this._actions[cmd];
            if (action == null)
            {
                throw new BadRequestException(
                    correlationId,
                    "NO_ACTION",
                    "Action " + cmd + " was not found"
                )
                .WithDetails("command", cmd);
            }

            await action(context);
        }

        private async Task Handler(HttpContext context)
        {
            // If already started then execute
            if (IsOpen())
            {
                await ExecuteAsync(context);
                return;
            }
            // Start before execute
            await RunAsync();
            await ExecuteAsync(context);
        }

        /// <summary>
        /// Gets entry point into this Google Function.
        /// </summary>
        /// <returns>Returns plugin function</returns>
        public Func<HttpContext, Task> GetHandler()
        {
            return Handler;
        }
    }
}
