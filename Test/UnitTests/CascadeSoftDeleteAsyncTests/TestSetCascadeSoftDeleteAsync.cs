// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace Test.UnitTests.CascadeSoftDeleteAsyncTests
{
    public class TestSetCascadeSoftDeleteAsync
    {
        private readonly ITestOutputHelper _output;
        private readonly Regex _selectMatchRegex = new Regex(@"SELECT "".""\.""Id"",", RegexOptions.None);

        public TestSetCascadeSoftDeleteAsync(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestCreateEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                //ATTEMPT
                var ceo = Employee.SeedEmployeeSoftDel(context);

                //VERIFY
                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(9);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
            }
        }

        [Fact]
        public async Task TestCascadeDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                //ATTEMPT
                context.Remove(ceo.WorksFromMe.First());
                await context.SaveChangesAsync();

                //VERIFY
                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(4);
                context.Contracts.Count().ShouldEqual(3);
            }
        }

        [Fact]
        public async Task TestManualSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                //ATTEMPT
                ceo.WorksFromMe.First().SoftDeleteLevel = 1;
                await context.SaveChangesAsync();

                //VERIFY
                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(10);
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteEmployeeSoftDelInfoOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First());

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);

                context.ChangeTracker.Clear();
                context.Employees.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(7);
                status.Message.ShouldEqual("You have soft deleted an entity and its 12 dependents");
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteEmployeeSoftDelInfoNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First(), false);
                context.Employees.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(0);
                context.SaveChanges();
                context.Employees.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(7);

                //VERIFY
                //Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.Result.ShouldEqual(7 + 6);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("You have soft deleted an entity and its 12 dependents");
            }
        }

        [Fact]
        public async Task TestGetSoftDeletedEntriesEmployeeSoftDeletedOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);
                (await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First())).IsValid.ShouldBeTrue();

                //ATTEMPT
                var softDeleted = await service.GetSoftDeletedEntries<Employee>().ToListAsync();

                //VERIFY
                context.ChangeTracker.Clear();
                softDeleted.Count.ShouldEqual(1);
                softDeleted.Single().Name.ShouldEqual(ceo.WorksFromMe.First().Name);
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteEmployeeSoftDelOneToOneOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First().Contract));

                //VERIFY
                ex.Message.ShouldEqual("You cannot soft delete a one-to-one relationship. It causes problems if you try to create a new version.");
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First());

                //VERIFY
                //Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);

                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(4);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.Employees.IgnoreQueryFilters().Select(x => x.SoftDeleteLevel).Where(x => x > 0).ToArray()
                    .ShouldEqual(new byte[] { 1, 2, 2, 3, 3, 3, 3 });
                context.Contracts.Count().ShouldEqual(3);
                context.Contracts.IgnoreQueryFilters().Count().ShouldEqual(9);
                context.Employees.IgnoreQueryFilters().Select(x => x.Contract).Where(x => x != null)
                    .Select(x => x.SoftDeleteLevel).Where(x => x > 0).ToArray()
                    .ShouldEqual(new byte[] { 2, 3, 3, 4, 4, 4 });
            }
        }

        [Theory]
        [InlineData(false, 4)]
        [InlineData(true, 7)]
        public async Task TestCascadeSoftDeleteEmployeeSoftDelWithLoggingOk(bool readEveryTime, int selectCount)
        {
            //SETUP
            var logs = new List<string>();
            var options = SqliteInMemory.CreateOptionsWithLogTo<CascadeSoftDelDbContext>(log => logs.Add(log));
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var cto = Employee.SeedEmployeeSoftDel(context).WorksFromMe.First();

                var config = new ConfigCascadeDeleteWithUserId(context)
                {
                    ReadEveryTime = readEveryTime
                };
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                logs.Clear();
                var status = await service.SetCascadeSoftDeleteAsync(cto);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                logs.Count(x =>  _selectMatchRegex.IsMatch(x)).ShouldEqual(selectCount);
                status.Result.ShouldEqual(7 + 6);

                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(4);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.Employees.IgnoreQueryFilters().Select(x => x.SoftDeleteLevel).Where(x => x > 0).ToArray()
                    .ShouldEqual(new byte[] { 1, 2, 2, 3, 3, 3, 3 });
                context.Contracts.Count().ShouldEqual(3);
                context.Contracts.IgnoreQueryFilters().Count().ShouldEqual(9);
                context.Employees.IgnoreQueryFilters().Select(x => x.Contract).Where(x => x != null)
                    .Select(x => x.SoftDeleteLevel).Where(x => x > 0).ToArray()
                    .ShouldEqual(new byte[] { 2, 3, 3, 4, 4, 4 });
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteExistingSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);
                
                var preNumSoftDeleted = (await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First().WorksFromMe.First())).Result;
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(ceo.WorksFromMe.First());

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                preNumSoftDeleted.ShouldEqual(3 + 3);
                status.Result.ShouldEqual(4 + 3);
                context.Employees.Count().ShouldEqual(4);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.Employees.IgnoreQueryFilters().Select(x => x.SoftDeleteLevel).Where(x => x > 0).ToArray()
                    .ShouldEqual(new byte[] { 1, 1, 2, 2, 2, 3, 3 });
                context.Contracts.Count().ShouldEqual(3);
                context.Contracts.IgnoreQueryFilters().Count().ShouldEqual(9);
                context.Employees.IgnoreQueryFilters().Select(x => x.Contract).Where(x => x != null)
                    .Select(x => x.SoftDeleteLevel).Where(x => x > 0)
                    .ToArray().ShouldEqual(new byte[] { 2, 2, 3, 3, 3, 4 });
            }
        }

        [Fact]
        public async Task TestCascadeSoftDeleteTwoLevelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var numInnerSoftDelete = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "ProjectManager1"))).Result;
                numInnerSoftDelete.ShouldEqual(3 + 3);
                var status = await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"));

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(4 + 3);
                context.Employees.Count().ShouldEqual(4);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.Employees.IgnoreQueryFilters().Select(x => x.SoftDeleteLevel).Where(x => x > 0)
                    //.ToList().ForEach(x => _output.WriteLine(x.ToString()));
                    .ToArray().ShouldEqual(new byte[] { 1, 1, 2, 2,2, 3,3 });
                context.Contracts.Count().ShouldEqual(3);
                context.Contracts.IgnoreQueryFilters().Count().ShouldEqual(9);
                context.Employees.IgnoreQueryFilters().Select(x => x.Contract).Where(x => x != null)
                    .Select(x => x.SoftDeleteLevel).Where(x => x > 0)
                    //.ToList().ForEach(x => _output.WriteLine(x.ToString()));
                    .ToArray().ShouldEqual(new byte[] { 2, 2, 3, 3, 3, 4 });
            }
        }

        [Fact]
        public async Task TestCircularLoopCascadeSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);
                var devEntry = context.Employees.Single(x => x.Name == "dev1a");
                devEntry.WorksFromMe = new List<Employee>{ devEntry.Manager.Manager};

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7+6);          
            }
        }

        //---------------------------------------------------------
        //SetCascadeSoftDelete disconnected tests

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestDisconnectedCascadeSoftDeleteEmployeeSoftDelOk(bool readEveryTime)
        {
            //SETUP
            var logs = new List<string>();
            var options = SqliteInMemory.CreateOptionsWithLogTo<CascadeSoftDelDbContext>(log => logs.Add(log));
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context)
                {
                    ReadEveryTime = readEveryTime
                };
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                logs.Clear();
                var status = await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                //logs.Count(x => _selectMatchRegex.IsMatch(x)).ShouldEqual(7);
                status.Result.ShouldEqual(7+6);

                context.ChangeTracker.Clear();
                context.Employees.Count().ShouldEqual(4);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(3);
                context.Contracts.IgnoreQueryFilters().Count().ShouldEqual(9);
            }
        }

        //------------------------------------------------------------
        //Check UserId Query Filter 

        [Fact]
        public void TestSeedCompanyWithQuotesOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();

                //ATTEMPT
                var company = Customer.SeedCustomerWithQuotes(context, userId);

                //VERIFY
                context.Companies.Count().ShouldEqual(1);

                context.ChangeTracker.Clear();
                context.Quotes.Count().ShouldEqual(4);
                context.Set<QuotePrice>().Count().ShouldEqual(4);
            }
        }

        [Fact]
        public void TestSeedCompanyWithQuotesQueryOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                Customer.SeedCustomerWithQuotes(context, userId);

                //ATTEMPT
                var query = context.Companies.Include(x => x.Quotes);
                var company = query.Single();

                //VERIFY
                _output.WriteLine(query.ToQueryString());

                context.ChangeTracker.Clear();
                company.Quotes.Count.ShouldEqual(4);
            }
        }

        [Fact]
        public void TestSeedCompanyWithQuotesQueryIgnoreOnIncludeOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                Customer.SeedCustomerWithQuotes(context, userId);
                Customer.SeedCustomerWithQuotes(context, userId, "company2");

                //ATTEMPT
                var companies = context.Companies.ToList();
                var quotesQuery = context.Quotes.IgnoreQueryFilters()
                    .Where(quote => companies.Select(company => company.Id).Contains(quote.Id));
                var quotes = quotesQuery.ToList();

                //VERIFY
                _output.WriteLine(quotesQuery.ToQueryString());
                companies.All(x => x.Quotes.Count == 4).ShouldBeTrue();
            }
        }

        [Fact]
        public async Task TestCascadeDeleteCompanyOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Customer.SeedCustomerWithQuotes(context, userId);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 4 + 4 + (4 * 4));
                status.Message.ShouldEqual("You have soft deleted an entity and its 24 dependents");
            }
        }

        [Fact]
        public async Task TestGetSoftDeletedEntriesCompanyOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Customer.SeedCustomerWithQuotes(context, userId);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);
                var status = await service.SetCascadeSoftDeleteAsync(company);

                //ATTEMPT
                var softDeleted = await service.GetSoftDeletedEntries<Customer>().ToListAsync();

                //VERIFY
                softDeleted.Count.ShouldEqual(1);
                softDeleted.Single().CompanyName.ShouldEqual(company.CompanyName);
            }
        }

        [Fact]
        public async Task TestCascadeDeleteCompanySomeQuotesDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Customer.SeedCustomerWithQuotes(context, userId);
                company.Quotes.First().UserId = Guid.NewGuid();
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 3 + 3 + (3 * 4));
                status.Message.ShouldEqual("You have soft deleted an entity and its 18 dependents");

                context.ChangeTracker.Clear();
                context.Quotes.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(3);
            }
        }

        [Fact]
        public async Task TestCascadeDeleteCompanySomeQuotePriceDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Customer.SeedCustomerWithQuotes(context, userId);
                company.Quotes.First().PriceInfo.UserId = Guid.NewGuid();
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetCascadeSoftDeleteAsync(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 4 + 3 + (4 * 4));
                status.Message.ShouldEqual("You have soft deleted an entity and its 23 dependents");

                context.ChangeTracker.Clear();
                context.Set<QuotePrice>().IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(3);
            }
        }

    }
}