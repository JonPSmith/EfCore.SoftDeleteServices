// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using DataLayer.CascadeEfClasses;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using DataLayer.SingleEfClasses;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Concrete;
using Test.ExampleConfigs;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.IssuesTests
{
    public class TestIssue001
    {
        private readonly ITestOutputHelper _output;

        public TestIssue001(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestSoftDeleteOrderWithAddressOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            context.Add(new Order
            {
                OrderRef = "123",
                UserAddress = new Address {FullAddress = "xxx"}
            });
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigSoftDeleteWithUserId(context);
            var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

            //ATTEMPT
            var status = service.SetSoftDeleteViaKeys<Order>(1);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.Orders.Count().ShouldEqual(0);
            context.Addresses.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestSoftDeleteAsyncOrderWithAddressOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            context.Add(new Order
            {
                OrderRef = "123",
                UserAddress = new Address { FullAddress = "xxx" }
            });
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigSoftDeleteWithUserId(context);
            var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

            //ATTEMPT
            var status = await service.SetSoftDeleteViaKeysAsync<Order>(1);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.Orders.Count().ShouldEqual(0);
            context.Addresses.Count().ShouldEqual(1);
        }

        [Fact]
        public void TestCascadeDeleteOneQuoteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            context.Add(new Customer
            {
                CompanyName = "xxx",
                MoreInfo = new CustomerInfo()
            });
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigCascadeDeleteWithUserId(context);
            var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

            //ATTEMPT
            var status = service.SetCascadeSoftDeleteViaKeys<Customer>(1);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.Companies.Count().ShouldEqual(0);
            context.CompanyInfos.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestCascadeDeleteAsyncOneQuoteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            context.Add(new Customer
            {
                CompanyName = "xxx",
                MoreInfo = new CustomerInfo()
            });
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigCascadeDeleteWithUserId(context);
            var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

            //ATTEMPT
            var status = await service.SetCascadeSoftDeleteViaKeysAsync<Customer>(1);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.Companies.Count().ShouldEqual(0);
            context.CompanyInfos.Count().ShouldEqual(1);
        }
    }
}