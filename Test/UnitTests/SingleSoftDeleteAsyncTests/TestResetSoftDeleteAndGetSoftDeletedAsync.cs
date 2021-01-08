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
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.SingleSoftDeleteAsyncTests
{
    public class TestResetSoftDeleteAndGetSoftDeletedAsync
    {

        [Fact]
        public async Task TestSoftDeleteServiceResetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                await service.SetSoftDeleteAsync(book);

                //ATTEMPT
                var status = await service.ResetSoftDeleteAsync(book);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.Count().ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceResetSoftDeleteNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                await service.SetSoftDeleteAsync(book);

                //ATTEMPT
                var status = await service.ResetSoftDeleteAsync(book, false);
                context.Books.Count().ShouldEqual(0);
                context.SaveChanges();
                context.Books.Count().ShouldEqual(1);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceDddResetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                context.Database.EnsureCreated();
                var bookDdd = new BookDDD("Test");
                context.Add(bookDdd);
                context.SaveChanges();

                var config = new ConfigSoftDeleteDDD(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDeletedDDD>(config);
                await service.SetSoftDeleteAsync(bookDdd);

                //ATTEMPT
                var status = await service.ResetSoftDeleteAsync(bookDdd);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.BookDdds.Count().ShouldEqual(1);
                context.BookDdds.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceResetSoftDeleteViaKeysOk()
        {
            //SETUP
            int bookId;
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                bookId = context.AddBookWithReviewToDb().Id;

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status1 = await service.SetSoftDeleteViaKeysAsync<Book>(bookId);
                status1.IsValid.ShouldBeTrue(status1.GetAllErrors());

                //ATTEMPT
                var status2 = await service.ResetSoftDeleteViaKeysAsync<Book>(bookId);

                //VERIFY
                status2.IsValid.ShouldBeTrue(status2.GetAllErrors());
                status2.Result.ShouldEqual(1);
            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Books.Count().ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestHardDeleteViaKeysWithUserIdOk()
        {
            //SETUP
            var currentUser = Guid.NewGuid();
            int orderId;
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var order1 = new Order
                    { OrderRef = "Cur user Order, soft del", SoftDeleted = true, UserId = currentUser };
                var order2 = new Order
                    { OrderRef = "Cur user Order", SoftDeleted = false, UserId = currentUser };
                var order3 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = true, UserId = Guid.NewGuid() };
                var order4 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = false, UserId = Guid.NewGuid() };
                context.AddRange(order1, order2, order3, order4);
                context.SaveChanges();
                orderId = order1.Id;
            }
            using (var context = new SingleSoftDelDbContext(options, currentUser))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.ResetSoftDeleteViaKeysAsync<Order>(orderId);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
                context.Orders.IgnoreQueryFilters().Count().ShouldEqual(4);
                context.Orders.Count().ShouldEqual(2);
            }
        }

        [Fact]
        public async Task TestHardDeleteViaKeysWithWrongUserIdBad()
        {
            //SETUP
            var currentUser = Guid.NewGuid();
            int orderId;
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var order1 = new Order
                    { OrderRef = "Cur user Order, soft del", SoftDeleted = true, UserId = currentUser };
                var order2 = new Order
                    { OrderRef = "Cur user Order", SoftDeleted = false, UserId = currentUser };
                var order3 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = true, UserId = Guid.NewGuid() };
                var order4 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = false, UserId = Guid.NewGuid() };
                context.AddRange(order1, order2, order3, order4);
                context.SaveChanges();
                orderId = order3.Id;
            }
            using (var context = new SingleSoftDelDbContext(options, currentUser))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = await service.ResetSoftDeleteViaKeysAsync<Order>(orderId);

                //VERIFY
                status.IsValid.ShouldBeFalse();
                status.GetAllErrors().ShouldEqual("Could not find the entry you ask for.");
                context.Orders.IgnoreQueryFilters().Count().ShouldEqual(4);
                context.Orders.Count().ShouldEqual(1);
            }
        }

        //-------------------------------------------
        //GetSoftDeletedEntries 

        [Fact]
        public async Task TestSoftDeleteServiceGetSoftDeletedEntriesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book1 = context.AddBookWithReviewToDb("test1");
                var book2 = context.AddBookWithReviewToDb("test2");

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);
                var status = await service.SetSoftDeleteAsync(book1);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());

            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var softDelBooks = await service.GetSoftDeletedEntries<Book>().ToListAsync();

                //VERIFY
                softDelBooks.Count.ShouldEqual(1);
                softDelBooks.Single().Title.ShouldEqual("test1");
                context.Books.Count().ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(2);
            }
        }

        [Fact]
        public async Task TestSoftDeleteServiceGetSoftDeletedEntriesWithUserIdOk()
        {
            //SETUP
            var currentUser = Guid.NewGuid();
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var order1 = new Order
                    { OrderRef = "Cur user Order, soft del", SoftDeleted = true, UserId = currentUser};
                var order2 = new Order
                    { OrderRef = "Cur user Order", SoftDeleted = false, UserId = currentUser };
                var order3 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = true, UserId = Guid.NewGuid() };
                var order4 = new Order
                    { OrderRef = "Diff user Order", SoftDeleted = false, UserId = Guid.NewGuid() };
                context.AddRange(order1, order2, order3, order4);
                context.SaveChanges();
            }
            using (var context = new SingleSoftDelDbContext(options, currentUser))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteServiceAsync<ISingleSoftDelete>(config);

                //ATTEMPT
                var orders = await service.GetSoftDeletedEntries<Order>().ToListAsync();

                //VERIFY
                orders.Count.ShouldEqual(1);
                orders.Single(x => x.UserId == currentUser).OrderRef.ShouldEqual("Cur user Order, soft del");
                context.Orders.IgnoreQueryFilters().Count().ShouldEqual(4);
                var all = context.Orders.IgnoreQueryFilters().ToList();
                context.Orders.Count().ShouldEqual(1);
            }
        }
    }
}