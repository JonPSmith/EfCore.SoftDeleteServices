// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class TestSetCascadeSoftDelete
    {
        private readonly ITestOutputHelper _output;
        private readonly Regex _selectMatchRegex = new Regex(@"SELECT "".""\.""Id"",", RegexOptions.None);

        public TestSetCascadeSoftDelete(ITestOutputHelper output)
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
                context.Employees.Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(9);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
            }
        }

        [Fact]
        public void TestCascadeDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                //ATTEMPT
                context.Remove(ceo.WorksFromMe.First());
                context.SaveChanges();

                //VERIFY
                context.Employees.Count().ShouldEqual(4);
                context.Contracts.Count().ShouldEqual(3);
            }
        }

        [Fact]
        public void TestManualSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                //ATTEMPT
                ceo.WorksFromMe.First().SoftDeleteLevel = 1;
                context.SaveChanges();

                //VERIFY
                context.Employees.Count().ShouldEqual(10);
            }
        }

        [Fact]
        public void TestCascadeSoftDeleteEmployeeSoftDelInfoOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(ceo.WorksFromMe.First());

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                context.Employees.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(7);
                status.Message.ShouldEqual("You have soft deleted an entity and its 12 dependents");
            }
        }

        [Fact]
        public void TestCascadeSoftDeleteEmployeeSoftDelInfoNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(ceo.WorksFromMe.First(), false);
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
        public void TestGetSoftDeletedEntriesEmployeeSoftDeletedOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);
                service.SetCascadeSoftDelete(ceo.WorksFromMe.First()).IsValid.ShouldBeTrue();

                //ATTEMPT
                var softDeleted = service.GetSoftDeletedEntries<Employee>().ToList();

                //VERIFY
                softDeleted.Count.ShouldEqual(1);
                softDeleted.Single().Name.ShouldEqual(ceo.WorksFromMe.First().Name);
            }
        }

        [Fact]
        public void TestCascadeSoftDeleteEmployeeSoftDelOneToOneOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var ex = Assert.Throws<InvalidOperationException>(() => service.SetCascadeSoftDelete(ceo.WorksFromMe.First().Contract));

                //VERIFY
                ex.Message.ShouldEqual("You cannot soft delete a one-to-one relationship. It causes problems if you try to create a new version.");
            }
        }

        [Fact]
        public void TestCascadeSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(ceo.WorksFromMe.First());

                //VERIFY
                //Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
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
        [InlineData(false, 3)]
        [InlineData(true, 6)]
        public void TestCascadeSoftDeleteEmployeeSoftDelWithLoggingOk(bool readEveryTime, int selectCount)
        {
            //SETUP
            var logs = new List<string>();
            var options = SqliteInMemory.CreateOptionsWithLogging<CascadeSoftDelDbContext>(log => logs.Add(log.DecodeMessage()));
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context)
                {
                    ReadEveryTime = readEveryTime
                };
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                logs.Clear();
                var status = service.SetCascadeSoftDelete(ceo.WorksFromMe.First());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                logs.Count(x =>  _selectMatchRegex.IsMatch(x)).ShouldEqual(selectCount);
                status.Result.ShouldEqual(7 + 6);
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
        public void TestCascadeSoftDeleteExistingSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);
                
                var preNumSoftDeleted = service.SetCascadeSoftDelete(ceo.WorksFromMe.First().WorksFromMe.First()).Result;
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(ceo.WorksFromMe.First());

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
        public void TestCascadeSoftDeleteTwoLevelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var numInnerSoftDelete = service.SetCascadeSoftDelete(context.Employees.Single(x => x.Name == "ProjectManager1")).Result;
                numInnerSoftDelete.ShouldEqual(3 + 3);
                var status = service.SetCascadeSoftDelete(context.Employees.Single(x => x.Name == "CTO"));

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
        public void TestCircularLoopCascadeSoftDeleteEmployeeSoftDelOk()
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
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(context.Employees.Single(x => x.Name == "CTO"));

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
        public void TestDisconnectedCascadeSoftDeleteEmployeeSoftDelOk(bool readEveryTime)
        {
            //SETUP
            var logs = new List<string>();
            var options = SqliteInMemory.CreateOptionsWithLogging<CascadeSoftDelDbContext>(log => logs.Add(log.DecodeMessage()));
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                Employee.SeedEmployeeSoftDel(context);
            }
            using (var context = new CascadeSoftDelDbContext(options))
            {
                var config = new ConfigCascadeDeleteWithUserId(context)
                {
                    ReadEveryTime = readEveryTime
                };
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                logs.Clear();
                var status = service.SetCascadeSoftDelete(context.Employees.Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                logs.Count(x => _selectMatchRegex.IsMatch(x)).ShouldEqual(7);
                status.Result.ShouldEqual(7+6);
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
                var company = Company.SeedCompanyWithQuotes(context, userId);

                //VERIFY
                context.Companies.Count().ShouldEqual(1);
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
                Company.SeedCompanyWithQuotes(context, userId);

                //ATTEMPT
                var query = context.Companies.Include(x => x.Quotes);
                var company = query.Single();

                //VERIFY
                _output.WriteLine(query.ToQueryString());
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
                Company.SeedCompanyWithQuotes(context, userId);
                Company.SeedCompanyWithQuotes(context, userId, "company2");

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
        public void TestCascadeDeleteCompanyOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 4 + 4);
                status.Message.ShouldEqual("You have soft deleted an entity and its 8 dependents");
            }
        }

        [Fact]
        public void TestGetSoftDeletedEntriesCompanyOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);
                var status = service.SetCascadeSoftDelete(company);

                //ATTEMPT
                var softDeleted = service.GetSoftDeletedEntries<Company>().ToList();

                //VERIFY
                softDeleted.Count.ShouldEqual(1);
                softDeleted.Single().CompanyName.ShouldEqual(company.CompanyName);
            }
        }

        [Fact]
        public void TestCascadeDeleteCompanySomeQuotesDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);
                company.Quotes.First().UserId = Guid.NewGuid();
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 3 + 3);
                status.Message.ShouldEqual("You have soft deleted an entity and its 6 dependents");
                context.Quotes.IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(3);
            }
        }

        [Fact]
        public void TestCascadeDeleteCompanySomeQuotePriceDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);
                company.Quotes.First().PriceInfo.UserId = Guid.NewGuid();
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 4 + 3);
                status.Message.ShouldEqual("You have soft deleted an entity and its 7 dependents");
                context.Set<QuotePrice>().IgnoreQueryFilters().Count(x => x.SoftDeleteLevel != 0).ShouldEqual(3);
            }
        }

        [Fact]
        public void TestCascadeDeleteQuoteDoesNotSoftDeleteCompanyOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);
                company.Quotes.First().UserId = Guid.NewGuid();
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = service.SetCascadeSoftDelete(company.Quotes.First());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 1);
                status.Message.ShouldEqual("You have soft deleted an entity and its 1 dependents");
                context.Companies.Count().ShouldEqual(1);
                context.Quotes.Count().ShouldEqual(3);
            }
        }

    }
}