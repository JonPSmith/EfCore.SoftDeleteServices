// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using DataLayer.CascadeEfClasses;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete;
using Test.ExampleConfigs;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.CascadeSoftDeleteTests
{
    public class ExampleCascadeSoftDelete
    {
        private readonly ITestOutputHelper _output;

        public ExampleCascadeSoftDelete(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestCascadeDeleteCompanyQuotesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                //ATTEMPT
                var customer = Customer.SeedCustomerWithQuotes(context, Guid.Empty);

                //VERIFY
                context.ChangeTracker.Clear();
                context.Companies.Count().ShouldEqual(1);
                context.Quotes.Count().ShouldEqual(4);
                context.Set<LineItem>().Count().ShouldEqual(4 * 4);
                context.Set<QuotePrice>().Count().ShouldEqual(4);
            }
        }

        [Fact]
        public void TestCascadeDeleteOneQuoteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var customer = Customer.SeedCustomerWithQuotes(context, Guid.Empty);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(customer.Quotes.First());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("You have soft deleted an entity and its 5 dependents");

                context.ChangeTracker.Clear();
                context.Companies.Count().ShouldEqual(1);
                context.Quotes.Count().ShouldEqual(3);
                context.Set<LineItem>().Count().ShouldEqual(3 * 4);
                context.Set<QuotePrice>().Count().ShouldEqual(3);
                status.Result.ShouldEqual(1 + 4 + 1);            
                context.Set<LineItem>().IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(4);
            }
        }

        [Fact]
        public void TestCascadeDeleteOneQuoteThenStockCheckOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var customer = Customer.SeedCustomerWithQuotes(context, Guid.Empty);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                var status = service.SetCascadeSoftDelete(customer.Quotes.First());
                status.IsValid.ShouldBeTrue(status.GetAllErrors());

                //ATTEMPT
                var requiredProducts = context.Set<LineItem>().ToList()
                    .GroupBy(x => x.ProductSku, y => y.NumProduct)
                    .ToDictionary(x => x.Key, y => y.Sum());

                //VERIFY
                foreach (var productSku in requiredProducts.Keys)
                {
                    _output.WriteLine($"{productSku}: {requiredProducts [productSku]} needed.");
                }
            }
        }
    }
}