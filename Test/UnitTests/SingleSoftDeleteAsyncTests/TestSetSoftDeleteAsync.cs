// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
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
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.SingleSoftDeleteAsyncTests
{
    public class TestSetSoftDeleteAsync
    {
        private readonly ITestOutputHelper _output;

        public TestSetSoftDeleteAsync(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddBookWithReviewOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                //ATTEMPT
                context.AddBookWithReviewToDb();
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                //VERIFY
                var book = await context.Books.Include(x => x.Reviews).SingleAsync();
                book.Title.ShouldEqual("test");
                book.Reviews.ShouldNotBeNull();
                book.Reviews.Single().NumStars.ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestQueryBookWithReviewsOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                context.AddBookWithReviewToDb();

                //ATTEMPT
                var query = context.Books.Include(x => x.Reviews);
                var book = await query.SingleAsync();

                //VERIFY
                _output.WriteLine(query.ToQueryString());
                book.Title.ShouldEqual("test");
                book.Reviews.ShouldNotBeNull();
                book.Reviews.Single().NumStars.ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteAsync(book);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.Count().ShouldEqual(0);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteStopCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteAsync(book, false);
                context.Books.Count().ShouldEqual(1);
                context.SaveChanges();
                context.Books.Count().ShouldEqual(0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteViaKeysOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteViaKeysAsync<Book>(book.Id);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.Count().ShouldEqual(0);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteViaKeysBadKeyType()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.SetSoftDeleteViaKeysAsync<Book>(book));

                //VERIFY
                ex.Message.ShouldEqual("Mismatch in keys: your provided key 1 (of 1) is of type Book but entity key's type is System.Int32 (Parameter 'keyValues')");
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteViaKeysBadNumberOfKeys()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await service.SetSoftDeleteViaKeysAsync<Book>(1,2));

                //VERIFY
                ex.Message.ShouldEqual("Mismatch in keys: your provided 2 key(s) and the entity has 1 key(s) (Parameter 'keyValues')");
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteViaKeysNotFoundBad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteViaKeysAsync<Book>(123);

                //VERIFY
                status.IsValid.ShouldBeFalse();
                status.GetAllErrors().ShouldEqual("Could not find the entry you ask for.");
                status.Result.ShouldEqual(0);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDeleteViaKeysNotFoundReturnsZero()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                var config = new ConfigSoftDeleteWithUserId(context) {NotFoundIsNotAnError = true};
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteViaKeysAsync<Book>(123);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(0);
            }
        }

        //----------------------------------------
        //DDD Set

        [Fact]
        public async Task TestSoftDeleteServiceDddSetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var bookDdd = new BookDDD("Test");
                context.Add(bookDdd);
                context.SaveChanges();

                var config = new ConfigSoftDeleteDDD(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDeletedDDD>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteAsync(bookDdd);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.BookDdds.Count().ShouldEqual(0);
                context.BookDdds.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceSetSoftDddDeleteViaKeysOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var bookDdd = new BookDDD("Test");
                context.Add(bookDdd);
                context.SaveChanges();

                var config = new ConfigSoftDeleteDDD(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDeletedDDD>(config);

                //ATTEMPT
                var status = await service.SetSoftDeleteViaKeysAsync<BookDDD>(bookDdd.Id);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.BookDdds.Count().ShouldEqual(0);
                context.BookDdds.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }



    }
}