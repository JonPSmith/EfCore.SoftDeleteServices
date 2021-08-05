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

namespace Test.UnitTests.SingleSoftDeleteTests
{
    public class TestSetSoftDelete
    {
        private readonly ITestOutputHelper _output;

        public TestSetSoftDelete(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestAddBookWithReviewOk()
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
                var book = context.Books
                    .Include(x => x.Reviews)
                    .Include(x => x.OneToOneRelationship)
                    .Single();
                book.Title.ShouldEqual("test");
                book.OneToOneRelationship.ShouldNotBeNull();
                book.Reviews.ShouldNotBeNull();
                book.Reviews.Single().NumStars.ShouldEqual(1);
            }
        }

        [Fact]
        public void TestQueryBookWithReviewsOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                context.AddBookWithReviewToDb();

                //ATTEMPT
                var query = context.Books.Include(x => x.Reviews);
                var book = query.Single();

                //VERIFY
                _output.WriteLine(query.ToQueryString());
                book.Title.ShouldEqual("test");
                book.Reviews.ShouldNotBeNull();
                book.Reviews.Single().NumStars.ShouldEqual(1);
            }
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using var context = new SingleSoftDelDbContext(options);
            context.Database.EnsureCreated();
            var book = context.AddBookWithReviewToDb();

            var config = new ConfigSoftDeleteWithUserId(context);
            var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

            //ATTEMPT
            var status = service.SetSoftDelete(book);

            //VERIFY
            context.ChangeTracker.Clear();
            
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.ShouldEqual(1);
            context.Books.Count().ShouldEqual(0);
            context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteStopCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.SetSoftDelete(book, false);
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
        public void TestSoftDeleteServiceSetSoftDeleteViaKeysOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.SetSoftDeleteViaKeys<Book>(book.Id);

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
        public void TestSoftDeleteServiceSetSoftDeleteOneToOneBad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var ex = Assert.Throws<InvalidOperationException>(() => service.SetSoftDeleteViaKeys<OneToOne>(1));

                //VERIFY
                ex.Message.ShouldEqual("You cannot soft delete a one-to-one relationship. It causes problems if you try to create a new version.");
            }
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteViaKeysBadKeyType()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var ex = Assert.Throws<ArgumentException>(() => service.SetSoftDeleteViaKeys<Book>(book));

                //VERIFY
                ex.Message.ShouldEqual("Mismatch in keys: your provided key 1 (of 1) is of type Book but entity key's type is System.Int32 (Parameter 'keyValues')");
            }
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteViaKeysBadNumberOfKeys()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var ex = Assert.Throws<ArgumentException>(() => service.SetSoftDeleteViaKeys<Book>(1,2));

                //VERIFY
                ex.Message.ShouldEqual("Mismatch in keys: your provided 2 key(s) and the entity has 1 key(s) (Parameter 'keyValues')");
            }
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteViaKeysNotFoundBad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.SetSoftDeleteViaKeys<Book>(123);

                //VERIFY
                status.IsValid.ShouldBeFalse();
                status.GetAllErrors().ShouldEqual("Could not find the entry you ask for.");
                status.Result.ShouldEqual(0);
            }
        }

        [Fact]
        public void TestSoftDeleteServiceSetSoftDeleteViaKeysNotFoundReturnsZero()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();

                var config = new ConfigSoftDeleteWithUserId(context) {NotFoundIsNotAnError = true};
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.SetSoftDeleteViaKeys<Book>(123);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(0);
            }
        }

        //----------------------------------------
        //DDD Set

        [Fact]
        public void TestSoftDeleteServiceDddSetSoftDeleteOk()
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
                var service = new SingleSoftDeleteService<ISingleSoftDeletedDDD>(config);

                //ATTEMPT
                var status = service.SetSoftDelete(bookDdd);

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
        public void TestSoftDeleteServiceSetSoftDddDeleteViaKeysOk()
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
                var service = new SingleSoftDeleteService<ISingleSoftDeletedDDD>(config);

                //ATTEMPT
                var status = service.SetSoftDeleteViaKeys<BookDDD>(bookDdd.Id);

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