using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PipServices3.Commons.Config;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Commons.Validate;
using PipServices3.Components.Count;
using PipServices3.Components.Log;
using PipServices3.Components.Trace;
using PipServices3.Gcp.Containers;
using PipServices3.Rpc.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.Gcp.Services
{
    /// <summary>
    /// Abstract service that receives remove calls via Google Function protocol.
    /// 
    /// This service is intended to work inside CloudFunction container that
    /// exposes registered actions externally.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - dependencies:
    ///     - controller:            override for Controller dependency
    ///     
    /// ### References ###
    ///     - *:logger:*:*:1.0               (optional) <see cref="ILogger"/> components to pass log messages
    ///     - *:counters:*:*:1.0             (optional) <see cref="ICounters"/> components to pass collected measurements
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// 
    /// public class MyCloudFunctionService : CloudFunctionService
    /// {
    ///     private IMyController _controller;
    /// 
    ///     public MyCloudFunctionService(IMyController controller) : base("v1.myservice")
    ///     {
    ///         _controller = controller;
    /// 
    ///         this._dependencyResolver.Put("controller", new Descriptor("mygroup", "controller", "*", "*", "1.0"));
    ///     }
    /// 
    ///     public override void SetReferences(IReferences references)
    ///     {
    ///         base.SetReferences(references);
    ///         _controller = _dependencyResolver.GetRequired<IMyController>("controller");
    ///     }
    /// 
    ///     public static void m()
    ///     {
    ///         var service = new MyCloudFunctionService(controller);
    ///         service.Configure(ConfigParams.FromTuples(
    ///             "connection.protocol", "http",
    ///         "connection.host", "localhost",
    ///             "connection.port", 8080
    ///         ));
    ///         service.SetReferences(References.FromTuples(
    ///            new Descriptor("mygroup", "controller", "default", "default", "1.0"), controller
    ///         ));
    /// 
    ///         await service.OpenAsync("123");
    ///     }
    /// 
    ///     protected override void Register()
    ///     {
    ///         RegisterAction("get_dummies", new ObjectSchema()
    ///             .WithOptionalProperty("body",
    ///                 new ObjectSchema()
    ///                     .WithOptionalProperty("filter", new FilterParamsSchema())
    ///                     .WithOptionalProperty("paging", new PagingParamsSchema())
    ///                     .WithRequiredProperty("cmd", TypeCode.String)
    ///             ),
    ///             async (req) =>
    ///             {
    ///                 var correlationId = GetCorrelationId(req);
    ///                 var body = await CloudFunctionRequestHelper.GetBodyAsync(req);
    ///                 var id = body.GetAsString("id");
    ///                 return await this._controller.getMyData(correlationId, id);
    ///             }
    ///         );
    ///     }
    /// }
    /// 
    /// 
    /// 
    /// </code>
    /// </example>
    public abstract class CloudFunctionService : ICloudFunctionService, IOpenable, IConfigurable, IReferenceable
    {
        private string _name;
        private List<CloudFunctionAction> _actions = new();
        private List<Func<Func<HttpContext, Task>, Task>> _interceptors = new();
        private bool _opened;

        /// <summary>
        /// The dependency resolver.
        /// </summary>
        protected DependencyResolver _dependencyResolver = new DependencyResolver();

        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();

        /// <summary>
        /// The performance counters.
        /// </summary>
        protected CompositeCounters _counters = new CompositeCounters();

        /// <summary>
        /// The tracer.
        /// </summary>
        protected CompositeTracer _tracer = new CompositeTracer();

        /// <summary>
        /// Creates an instance of this service.
        /// </summary>
        public CloudFunctionService()
        {

        }

        /// <summary>
        /// Creates an instance of this service.
        /// </summary>
        /// <param name="name">a service name to generate action cmd.</param>
        public CloudFunctionService(string name = null)
        {
            _name = name;
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public virtual void Configure(ConfigParams config)
        {
            this._dependencyResolver.Configure(config);
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies. </param>
        public virtual void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _counters.SetReferences(references);
            _tracer.SetReferences(references);
            _dependencyResolver.SetReferences(references);
        }

        /// <summary>
        /// Get all actions supported by the service.
        /// </summary>
        /// <returns>an array with supported actions.</returns>
        public List<CloudFunctionAction> GetActions()
        {
            return this._actions;
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
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public bool IsOpen()
        {
            return this._opened;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public Task OpenAsync(string correlationId)
        {
            if (this._opened)
                return Task.CompletedTask;

            this.Register();

            this._opened = true;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public Task CloseAsync(string correlationId)
        {
            if (!this._opened)
                return Task.CompletedTask;

            this._opened = false;
            this._actions.Clear();
            this._interceptors.Clear();

            return Task.CompletedTask;
        }

        protected Func<HttpContext, Task> ApplyValidation(Schema schema, Func<HttpContext, Task> action)
        {
            // Create an action function
            Func<HttpContext, Task> actionWrapper = async (context) =>
            {
                // Validate object
                if (schema != null && context != null)
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

                await action(context);
            };

            return actionWrapper;
        }

        protected Func<HttpContext, Task>  ApplyInterceptors(Func<HttpContext, Task> action)
        {
            var actionWrapper = action;

            for (var index = this._interceptors.Count - 1; index >= 0; index--)
            {
                var interceptor = this._interceptors[index];

                Func<HttpContext, Task> wrapper(Func<HttpContext, Task> action)
                {
                    return (HttpContext context) =>
                    {
                        return interceptor(action);
                    };
                }

                actionWrapper = wrapper(actionWrapper);
            }

            return actionWrapper;
        }

        protected string GenerateActionCmd(string name)
        {
            var cmd = name;
            if (_name != null)
                cmd = _name + "." + cmd;
            return cmd;
        }

        protected void RegisterAction(string name, Schema schema, Func<HttpContext, Task> action)
        {
            var actionWrapper = this.ApplyValidation(schema, action);
            actionWrapper = this.ApplyInterceptors(actionWrapper);

            CloudFunctionAction registeredAction = new CloudFunctionAction {
                Cmd=GenerateActionCmd(name), 
                Schema=schema,
                Action=(HttpContext context) => { return actionWrapper(context); }
            };
            this._actions.Add(registeredAction);
        }

        protected void RegisterActionWithAuth(string name, Schema schema,
            Func<HttpContext, Func<HttpContext, Task>, Task> authorize,
            Func<HttpContext, Task> action)
        {
            var actionWrapper = this.ApplyValidation(schema, action);

            // Add authorization just before validation
            actionWrapper = (req) =>
            {
                return authorize(req, actionWrapper);
            };
            actionWrapper = this.ApplyInterceptors(actionWrapper);

            var self = this;
            var registeredAction = new CloudFunctionAction()
            {
                Cmd = GenerateActionCmd(name),
                Schema = schema,
                Action = async (req) => { await actionWrapper(req); }
            };

            _actions.Add(registeredAction);
        }

        /// <summary>
        /// Registers a middleware for actions in Google Function service.
        /// </summary>
        /// <param name="action">an action function that is called when middleware is invoked.</param>
        protected void RegisterInterceptor(Func<Func<HttpContext, Task>, Task> action)
        {
            _interceptors.Add(action);
        }

        /// <summary>
        /// Registers all service routes in HTTP endpoint.
        /// 
        /// This method is called by the service and must be overridden
        /// in child classes.
        /// </summary>
        protected abstract void Register();

        /// <summary>
        /// Returns correlationId from Google Function request.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <param name="context">the function request</param>
        /// <returns>returns correlationId from request</returns>
        protected string GetCorrelationId(HttpContext context)
        {
            return CloudFunctionRequestHelper.GetCorrelationId(context);
        }

        /// <summary>
        /// Returns command from Google Function request.
        /// This method can be overloaded in child classes
        /// </summary>
        /// <param name="context">the function request</param>
        /// <returns>returns command from request</returns>
        protected async Task<string> GetCommand(HttpContext context)
        {
            return await CloudFunctionRequestHelper.GetCommandAsync(context);
        }
    }
}
