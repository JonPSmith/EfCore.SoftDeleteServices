// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
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
    public class TestResetCascadeSoftDeleteAsync
    {
        private readonly ITestOutputHelper _output;

        public TestResetCascadeSoftDeleteAsync(ITestOutputHelper output)
        {
            _output = output;
        }

        //---------------------------------------------------------
        //reset 

        [Fact]
        public async Task TestResetCascadeSoftOfPreviousDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numSoftDeleted = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numSoftDeleted.ShouldEqual(7 + 6);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                context.Employees.Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(9);
            }
        }

        [Fact]
        public async Task TestResetCascadeSoftOfPreviousDeleteNoCalSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numSoftDeleted = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numSoftDeleted.ShouldEqual(7 + 6);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"), false);
                context.Employees.Count().ShouldEqual(11-7);
                context.Contracts.Count().ShouldEqual(9-6);
                context.SaveChanges();
                context.Employees.Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(9);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
            }
        }

        [Fact]
        public async Task TestResetCascadeSoftOfPreviousDeleteInfo()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numSoftDeleted = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numSoftDeleted.ShouldEqual(7 + 6);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                status.Message.ShouldEqual("You have recovered an entity and its 12 dependents");
            }
        }

        [Fact]
        public async Task TestResetCascadeSoftDeletePartialOfPreviousDeleteDoesNothingOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numSoftDeleted = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numSoftDeleted.ShouldEqual(7 + 6);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "ProjectManager1"));

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeFalse();
                status.GetAllErrors().ShouldEqual("This entry was soft deleted 1 level above here");
                status.Result.ShouldEqual(0);
            }
        }

        [Fact]
        public async Task TestResetCascadeSoftDeleteTwoLevelSoftDeleteThenResetTopOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numInnerSoftDelete = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "ProjectManager1"))).Result;
                numInnerSoftDelete.ShouldEqual(3 + 3);
                var numOuterSoftDelete = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numOuterSoftDelete.ShouldEqual(4 + 3);
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                Employee.ShowHierarchical(ceo, x => _output.WriteLine(x), false);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(4 + 3);
                var cto = context.Employees.Include(x => x.WorksFromMe).Single(x => x.Name == "CTO");
                cto.WorksFromMe.Single(x => x.SoftDeleteLevel == 0).Name.ShouldEqual("ProjectManager2");
            }
        }

        //-------------------------------------------------------------
        //disconnected state

        [Fact]
        public async Task TestDisconnectedResetCascadeSoftDeleteEmployeeSoftDelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            options.StopNextDispose();
            using (var context = new CascadeSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var ceo = Employee.SeedEmployeeSoftDel(context);

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                var numSoftDeleted = (await service.SetCascadeSoftDeleteAsync(context.Employees.Single(x => x.Name == "CTO"))).Result;
                numSoftDeleted.ShouldEqual(7 + 6);
            }
            using (var context = new CascadeSoftDelDbContext(options))
            {
                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                context.Employees.Count().ShouldEqual(11);
                context.Contracts.Count().ShouldEqual(9);
            }
        }

        //------------------------------------------------------------
        //Check UserId Query Filter

        [Fact]
        public async Task TestResetCascadeDeleteCompanySomeQuotesDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            options.StopNextDispose();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Customer.SeedCustomerWithQuotes(context, userId);
                company.Quotes.First().UserId = Guid.NewGuid();
                company.Quotes.First().SoftDeleteLevel = 1;  //Set to deleted
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);
                (await service.SetCascadeSoftDeleteAsync(company)).Result.ShouldEqual(1 + 3 + 3 + (3 * 4));
            }
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);

                //ATTEMPT
                var status = await service.ResetCascadeSoftDeleteAsync(context.Companies.IgnoreQueryFilters().Single());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 3 + 3 + (3 * 4));
                status.Message.ShouldEqual("You have recovered an entity and its 18 dependents");
                context.Quotes.Count().ShouldEqual(3);
            }
        }

    }
}