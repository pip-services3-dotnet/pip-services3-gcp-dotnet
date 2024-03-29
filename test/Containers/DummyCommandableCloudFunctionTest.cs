﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace PipServices3.Gcp.Containers
{
    public class DummyCommandableCloudFunctionTest : IDisposable
    {
        private DummyCloudFunctionFixture fixture;

        public DummyCommandableCloudFunctionTest()
        {
            fixture = new DummyCloudFunctionFixture(typeof(CommandableFunction));
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
