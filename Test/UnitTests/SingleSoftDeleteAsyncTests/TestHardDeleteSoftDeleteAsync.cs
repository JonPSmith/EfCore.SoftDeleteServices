// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
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

namespace Test.UnitTests.SingleSoftDeleteAsyncTests
{
    public class TestHardDeleteSoftDeleteAsync
    {

        [Fact]
        public async Task TestHardDeleteSoftDeletedEntryOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
#if NET6_0_OR_GREATER
            options.StopNextDispose();
#endif
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status = await service.SetSoftDeleteAsync(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.HardDeleteSoftDeletedEntryAsync(context.Books.IgnoreQueryFilters().Single());

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);

                context.ChangeTracker.Clear();
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);
            }
        }

        [Fact]
        public async Task TestHardDeleteSoftDeletedEntryNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
#if NET6_0_OR_GREATER
            options.StopNextDispose();
#endif
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status = await service.SetSoftDeleteAsync(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.HardDeleteSoftDeletedEntryAsync(context.Books.IgnoreQueryFilters().Single(), false);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
                context.SaveChanges();
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestHardDeleteViaKeysOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
#if NET6_0_OR_GREATER
            options.StopNextDispose();
#endif
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status = await service.SetSoftDeleteAsync(book);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.HardDeleteViaKeysAsync<Book>(context.Books.IgnoreQueryFilters().Single().Id);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);

                context.ChangeTracker.Clear();
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(0);
            }
        }

        [Fact]
        public async Task TestHardDeleteViaKeysNotFoundOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status1 = await service.SetSoftDeleteAsync(book);
                status1.IsValid.ShouldBeTrue(status1.GetAllErrors());

                //ATTEMPT
                var status = await service.HardDeleteViaKeysAsync<Book>(234);

                //VERIFY
                status.IsValid.ShouldBeFalse(status.GetAllErrors());
                status.GetAllErrors().ShouldEqual("Could not find the entry you ask for.");
            }
        }

    }
}