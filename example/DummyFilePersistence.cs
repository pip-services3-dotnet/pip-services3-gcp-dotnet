//using PipServices3.Commons.Config;
//using PipServices3.Commons.Data;
//using PipServices3.Data.Persistence;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace PipServices3.Gcp
//{
//    public class DummyFilePersistence: IdentifiableMemoryPersistence<Dummy, string>
//    {
//        protected JsonFilePersister<Dummy> _persister;

//        public DummyFilePersistence(string path = null) : base()
//        {
//            _persister = new JsonFilePersister<Dummy>(path);
//            _loader = this._persister;
//            _saver = this._persister;
//        }

//        public override void Configure(ConfigParams config)
//        {
//            base.Configure(config);
//            this._persister.Configure(config);
//        }

//        private List<Func<Dummy, bool>> ComposeFilter(FilterParams filter)
//        {
//            filter ??= new FilterParams();

//            var id = filter.GetAsNullableString("id");
//            var key = filter.GetAsNullableString("key");
//            var content = filter.GetAsNullableString("content");

//            return new List<Func<Dummy, bool>>()
//            {
//                (item) =>
//                {
//                    if (id != null && item.Id != id)
//                        return false;
//                    if (key != null && item.Key != key)
//                        return false;
//                    if (content != null && item.Content != content)
//                        return false;
//                    return true;
//                }
//            };
//        }

//        public async Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
//        {
//            return await base.GetPageByFilterAsync(correlationId, this.ComposeFilter(filter), paging);
//        }
//    }
//}
