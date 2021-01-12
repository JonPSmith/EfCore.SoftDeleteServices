// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using DataLayer.Interfaces;
using DataLayer.SingleEfClasses;
using DataLayer.SingleEfCode;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete;
using Test.EfHelpers;
using Test.ExampleConfigs;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.OtherTests
{
    public class TestShadowPropertySoftDel
    {
        private readonly ITestOutputHelper _output;

        public TestShadowPropertySoftDel(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestManuallySoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowDelClass();
            context.Add(shadowClass);
            context.SaveChanges();

            //ATTEMPT
            context.Entry(shadowClass).Property("SoftDeleted").CurrentValue = true;
            context.SaveChanges();

            //VERIFY
            context.ShadowDelClasses.Count().ShouldEqual(0);
            context.ShadowDelClasses.IgnoreQueryFilters().Count().ShouldEqual(1);
        }

        [Fact]
        public void TestSetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowDelClass();
            context.Add(shadowClass);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigSoftDeleteShadowDel(context);
            var service = new SingleSoftDeleteService<IShadowSoftDelete>(config);

            //ATTEMPT
            var status = service.SetSoftDeleteViaKeys<ShadowDelClass>(shadowClass.Id) ;

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldEqual(1);
            context.ShadowDelClasses.Count().ShouldEqual(0);
            context.ShadowDelClasses.IgnoreQueryFilters().Count().ShouldEqual(1);
        }

        [Fact]
        public void TestResetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowDelClass();
            context.Add(shadowClass);
            context.Entry(shadowClass).Property("SoftDeleted").CurrentValue = true;
            context.SaveChanges();

            var config = new ConfigSoftDeleteShadowDel(context);
            var service = new SingleSoftDeleteService<IShadowSoftDelete>(config);

            //ATTEMPT
            var status = service.ResetSoftDelete(shadowClass);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldEqual(1);
            context.ShadowDelClasses.Count().ShouldEqual(1);
        }

        [Fact]
        public void TestGetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowDelClass();
            context.Add(shadowClass);
            context.Entry(shadowClass).Property("SoftDeleted").CurrentValue = true;
            context.SaveChanges();
            
            context.ChangeTracker.Clear();

            var config = new ConfigSoftDeleteShadowDel(context);
            var service = new SingleSoftDeleteService<IShadowSoftDelete>(config);

            //ATTEMPT
            var entities = service.GetSoftDeletedEntries<ShadowDelClass>().ToList();

            //VERIFY
            entities.Count().ShouldEqual(1);
        }


    }
}