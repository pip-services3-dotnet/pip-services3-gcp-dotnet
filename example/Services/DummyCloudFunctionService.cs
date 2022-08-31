using Microsoft.AspNetCore.Http;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Data;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Validate;
using PipServices3.Gcp.Containers;
using PipServices3.Rpc.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

using TypeCode = PipServices3.Commons.Convert.TypeCode;

namespace PipServices3.Gcp.Services
{
    public class DummyCloudFunctionService : CloudFunctionService
    {
        private IDummyController _controller;
        private IDictionary<string, string> _headers = new Dictionary<string, string>() { { "Content-Type", "application/json" } };

        public DummyCloudFunctionService() : base("dummies")
        {
            this._dependencyResolver.Put("controller", new Descriptor("pip-services-dummies", "controller", "default", "*", "*"));
        }

        public override void SetReferences(IReferences references)
        {
            base.SetReferences(references);
            _controller = _dependencyResolver.GetOneRequired<IDummyController>("controller");
        }

        private async Task GetPageByFilterAsync(HttpContext context)
        {
            var body = await CloudFunctionRequestHelper.GetBodyAsync(context);
            var page = await _controller.GetPageByFilterAsync(
                GetCorrelationId(context),
                FilterParams.FromString(body.GetAsNullableString("filter")),
                PagingParams.FromTuples(
                    "total", CloudFunctionRequestHelper.ExtractFromQuery("total", context),
                    "skip", CloudFunctionRequestHelper.ExtractFromQuery("skip", context),
                    "take", CloudFunctionRequestHelper.ExtractFromQuery("take", context)
                )
           );

            SetHeaders(context);

            await HttpResponseSender.SendResultAsync(context.Response, page);
        }

        private async Task GetOneByIdAsync(HttpContext context)
        {
            var body = await CloudFunctionRequestHelper.GetBodyAsync(context);
            var dummy = await this._controller.GetOneByIdAsync(
                GetCorrelationId(context),
                body.GetAsNullableString("dummy_id")
            );

            SetHeaders(context);

            if (dummy != null)
                await HttpResponseSender.SendResultAsync(context.Response, dummy);
            else
                await HttpResponseSender.SendEmptyResultAsync(context.Response);
        }

        private async Task CreateAsync(HttpContext context)
        {
            var body = await CloudFunctionRequestHelper.GetBodyAsync(context);
            var dummy = await this._controller.CreateAsync(
                GetCorrelationId(context),
                JsonConverter.FromJson<Dummy>(JsonConverter.ToJson(body.GetAsObject("dummy")))
            );

            SetHeaders(context);

            await HttpResponseSender.SendCreatedResultAsync(context.Response, dummy);
        }

        private async Task UpdateAsync(HttpContext context)
        {
            var body = await CloudFunctionRequestHelper.GetBodyAsync(context);
            var dummy = await this._controller.UpdateAsync(
                GetCorrelationId(context),
                JsonConverter.FromJson<Dummy>(JsonConverter.ToJson(body.GetAsObject("dummy")))
            );

            SetHeaders(context);

            await HttpResponseSender.SendCreatedResultAsync(context.Response, dummy);
        }

        private async Task DeleteByIdAsync(HttpContext context)
        {
            var body = await CloudFunctionRequestHelper.GetBodyAsync(context);
            var dummy = await this._controller.DeleteByIdAsync(
                GetCorrelationId(context),
                body.GetAsNullableString("dummy_id")
            );

            SetHeaders(context);
            await HttpResponseSender.SendDeletedResultAsync(context.Response, dummy);
        }

        protected override void Register()
        {
            RegisterAction("get_dummies", new ObjectSchema()
                .WithOptionalProperty("body",
                    new ObjectSchema()
                        .WithOptionalProperty("filter", new FilterParamsSchema())
                        .WithOptionalProperty("paging", new PagingParamsSchema())
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                GetPageByFilterAsync
            );

            RegisterAction("get_dummy_by_id", new ObjectSchema()
                .WithRequiredProperty("body",
                    new ObjectSchema()
                        .WithRequiredProperty("dummy_id", TypeCode.String)
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                GetOneByIdAsync
            );

            RegisterAction("create_dummy", new ObjectSchema()
                .WithRequiredProperty("body",
                    new ObjectSchema()
                        .WithRequiredProperty("dummy", new DummySchema())
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                CreateAsync
            );

            RegisterAction("update_dummy", new ObjectSchema()
                .WithRequiredProperty("body",
                    new ObjectSchema()
                        .WithRequiredProperty("dummy", new DummySchema())
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                UpdateAsync
            );

            RegisterAction("delete_dummy", new ObjectSchema()
                .WithRequiredProperty("body",
                    new ObjectSchema()
                        .WithRequiredProperty("dummy_id", TypeCode.String)
                        .WithRequiredProperty("cmd", TypeCode.String)
                ),
                DeleteByIdAsync
            );
        }

        private void SetHeaders(HttpContext context)
        {
            foreach (var key in _headers.Keys)
                context.Response.Headers.Add(key, _headers[key]);
        }
    }
}
