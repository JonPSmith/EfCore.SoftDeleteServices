// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.SingleSoftDeleteTests
{
    public class TestHardDeleteSoftDelete
    {

        [Fact]
        public void TestHardDeleteSoftDeletedEntryOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status = service.SetSoftDelete(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.HardDeleteSoftDeletedEntry(context.Books.IgnoreQueryFilters().Single());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);
            }
        }

        [Fact]
        public void TestHardDeleteSoftDeletedEntryNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status = service.SetSoftDelete(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.HardDeleteSoftDeletedEntry(context.Books.IgnoreQueryFilters().Single(), false);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
                context.SaveChanges();
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);

            }
        }

        [Fact]
        public void TestHardDeleteViaKeysOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status = service.SetSoftDelete(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.HardDeleteViaKeys<Book>(context.Books.IgnoreQueryFilters().Single().Id);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);
            }
        }

        [Fact]
        public void TestHardDeleteViaKeysNotFoundOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status1 = service.SetSoftDelete(book);
                status1.IsValid.ShouldBeTrue(status1.GetAllErrors());

                //ATTEMPT
                var status = service.HardDeleteViaKeys<Book>(234);

                //VERIFY
                status.IsValid.ShouldBeFalse(status.GetAllErrors());
                status.GetAllErrors().ShouldEqual("Could not find the entry you ask for.");
            }
        }

    }
}