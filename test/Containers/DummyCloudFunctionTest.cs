﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Gcp.Containers
{
    public class DummyCloudFunctionTest: IDisposable
    {
        private DummyCloudFunctionFixture fixture;

        public DummyCloudFunctionTest()
        {
            fixture = new DummyCloudFunctionFixture(typeof(Function));
        }

        [Fact]
        public async Task CrudOperationsTest()
        {
            await fixture.TestCrudOperations();
        }

        public void Dispose()
        {
            fixture.Dispose();
        }
    }
}
