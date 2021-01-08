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
    public class TestHardDeleteCascadeSoftDelete
    {
        private readonly ITestOutputHelper _output;

        public TestHardDeleteCascadeSoftDelete(ITestOutputHelper output)
        {
            _output = output;
        }

        //---------------------------------------------------------
        //check

        [Fact]
        public async Task TestCheckCascadeSoftOfPreviousDeleteInfo()
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
                var status = await service.CheckCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                status.Result.ShouldEqual(7 + 6);
                status.Message.ShouldEqual("Are you sure you want to hard delete this entity and its 12 dependents");
            }
        }

        [Fact]
        public async Task TestCheckCascadeSoftDeleteNoSoftDeleteInfo()
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
                var status = await service.CheckCascadeSoftDeleteAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "ProjectManager1"));

                //VERIFY
                status.IsValid.ShouldBeFalse();
                status.Result.ShouldEqual(0);
                status.GetAllErrors().ShouldEqual("This entry isn't soft deleted.");
            }
        }

        //---------------------------------------------------------
        //hard delete

        [Fact]
        public async Task TestHardDeleteCascadeSoftOfPreviousDeleteInfo()
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
                var status = await service.HardDeleteSoftDeletedEntriesAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"));

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                status.Message.ShouldEqual("You have hard deleted an entity and its 12 dependents");
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(4);
            }
        }

        [Fact]
        public async Task TestHardDeleteCascadeSoftOfPreviousDeleteNoCallSaveChanges()
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
                var status = await service.HardDeleteSoftDeletedEntriesAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "CTO"), false);
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(11);
                context.SaveChanges();
                context.Employees.IgnoreQueryFilters().Count().ShouldEqual(4);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(7 + 6);
                status.Message.ShouldEqual("You have hard deleted an entity and its 12 dependents");
            }
        }

        [Fact]
        public async Task TestHardDeleteCascadeSoftDeleteNoSoftDeleteInfo()
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
                var status = await service.HardDeleteSoftDeletedEntriesAsync(context.Employees.IgnoreQueryFilters().Single(x => x.Name == "ProjectManager1"));

                //VERIFY
                status.IsValid.ShouldBeFalse();
                status.Result.ShouldEqual(0);
                status.GetAllErrors().ShouldEqual("This entry isn't soft deleted.");
            }
        }

        //------------------------------------------------------------
        //Check UserId Query Filter

        [Fact]
        public async Task TestHardDeleteCascadeDeleteCompanySomeQuotesDifferentUserIdOk()
        {
            //SETUP
            var userId = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using (var context = new CascadeSoftDelDbContext(options, userId))
            {
                context.Database.EnsureCreated();
                var company = Company.SeedCompanyWithQuotes(context, userId);
                Company.SeedCompanyWithQuotes(context, Guid.Empty, "Other company");
                context.SaveChanges();

                var config = new ConfigCascadeDeleteWithUserId(context);
                var service = new CascadeSoftDelServiceAsync<ICascadeSoftDelete>(config);
                (await service.SetCascadeSoftDeleteAsync(company)).Result.ShouldEqual(1 + 4 + 4);

                //ATTEMPT
                var status = await service.HardDeleteSoftDeletedEntriesAsync(company);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1 + 4 + 4);
                status.Message.ShouldEqual("You have hard deleted an entity and its 8 dependents");
                context.Quotes.Count().ShouldEqual(0);  
                context.Quotes.IgnoreQueryFilters().Count().ShouldEqual(4); 
            }
        }

    }
}