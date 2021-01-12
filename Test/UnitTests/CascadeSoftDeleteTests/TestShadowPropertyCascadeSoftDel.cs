// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using DataLayer.CascadeEfClasses;
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using DataLayer.SingleEfClasses;
using DataLayer.SingleEfCode;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete;
using Test.ExampleConfigs;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.CascadeSoftDeleteTests
{
    public class TestShadowPropertyCascadeSoftDel
    {
        private readonly ITestOutputHelper _output;

        public TestShadowPropertyCascadeSoftDel(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestManuallySoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowCascadeDelClass();
            context.Add(shadowClass);
            context.SaveChanges();

            //ATTEMPT
            context.Entry(shadowClass).Property("SoftDeleteLevel").CurrentValue = (byte)1;
            context.SaveChanges();

            //VERIFY
            context.ShadowCascadeDelClasses.Count().ShouldEqual(0);
            context.ShadowCascadeDelClasses.IgnoreQueryFilters().Count().ShouldEqual(1);
        }

        [Fact]
        public void TestSetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowCascadeDelClass();
            context.Add(shadowClass);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigCascadeDeleteShadowDel(context);
            var service = new CascadeSoftDelService<IShadowCascadeSoftDelete>(config);

            //ATTEMPT
            var status = service.SetCascadeSoftDeleteViaKeys<ShadowCascadeDelClass>(shadowClass.Id) ;

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldEqual(1);
            context.ShadowCascadeDelClasses.Count().ShouldEqual(0);
            context.ShadowCascadeDelClasses.IgnoreQueryFilters().Count().ShouldEqual(1);
        }

        [Fact]
        public void TestResetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowCascadeDelClass();
            context.Add(shadowClass);
            context.Entry(shadowClass).Property("SoftDeleteLevel").CurrentValue = (byte)1;
            context.SaveChanges();

            var config = new ConfigCascadeDeleteShadowDel(context);
            var service = new CascadeSoftDelService<IShadowCascadeSoftDelete>(config);

            //ATTEMPT
            var status = service.ResetCascadeSoftDelete(shadowClass);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldEqual(1);
            context.ShadowCascadeDelClasses.Count().ShouldEqual(1);
        }

        [Fact]
        public void TestGetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<CascadeSoftDelDbContext>();
            using var context = new CascadeSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var shadowClass = new ShadowCascadeDelClass();
            context.Add(shadowClass);
            context.Entry(shadowClass).Property("SoftDeleteLevel").CurrentValue = (byte)1;
            context.SaveChanges();
            
            context.ChangeTracker.Clear();

            var config = new ConfigCascadeDeleteShadowDel(context);
            var service = new CascadeSoftDelService<IShadowCascadeSoftDelete>(config);

            //ATTEMPT
            var entities = service.GetSoftDeletedEntries<ShadowCascadeDelClass>().ToList();

            //VERIFY
            entities.Count().ShouldEqual(1);
        }


    }
}