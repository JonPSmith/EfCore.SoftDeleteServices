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
    public class TestResetSoftDeleteAndGetSoftDeleted
    {

        [Fact]
        public void TestSoftDeleteServiceResetSoftDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                service.SetSoftDelete(book);

                //ATTEMPT
                var status = service.ResetSoftDelete(book);

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
        public void TestSoftDeleteServiceResetSoftDeleteNoCallSaveChangesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book = context.AddBookWithReviewToDb();

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                service.SetSoftDelete(book);

                //ATTEMPT
                var status = service.ResetSoftDelete(book, false);
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
        public void TestSoftDeleteServiceDddResetSoftDeleteOk()
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
                var service = new SingleSoftDeleteService<ISingleSoftDeletedDDD>(config);
                service.SetSoftDelete(bookDdd);

                //ATTEMPT
                var status = service.ResetSoftDelete(bookDdd);

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
        public void TestSoftDeleteServiceResetSoftDeleteViaKeysOk()
        {
            //SETUP
            int bookId;
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                bookId = context.AddBookWithReviewToDb().Id;

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status1 = service.SetSoftDeleteViaKeys<Book>(bookId);
                status1.IsValid.ShouldBeTrue(status1.GetAllErrors());

                //ATTEMPT
                var status2 = service.ResetSoftDeleteViaKeys<Book>(bookId);

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
        public void TestHardDeleteViaKeysWithUserIdOk()
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
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.ResetSoftDeleteViaKeys<Order>(orderId);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Result.ShouldEqual(1);
                context.Orders.IgnoreQueryFilters().Count().ShouldEqual(4);
                context.Orders.Count().ShouldEqual(2);
            }
        }

        [Fact]
        public void TestHardDeleteViaKeysWithWrongUserIdBad()
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
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var status = service.ResetSoftDeleteViaKeys<Order>(orderId);

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
        public void TestSoftDeleteServiceGetSoftDeletedEntriesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<SingleSoftDelDbContext>();
            using (var context = new SingleSoftDelDbContext(options))
            {
                context.Database.EnsureCreated();
                var book1 = context.AddBookWithReviewToDb("test1");
                var book2 = context.AddBookWithReviewToDb("test2");

                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);
                var status = service.SetSoftDelete(book1);
                status.IsValid.ShouldBeTrue(status.GetAllErrors());

            }
            using (var context = new SingleSoftDelDbContext(options))
            {
                var config = new ConfigSoftDeleteWithUserId(context);
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var softDelBooks = service.GetSoftDeletedEntries<Book>().ToList();

                //VERIFY
                softDelBooks.Count.ShouldEqual(1);
                softDelBooks.Single().Title.ShouldEqual("test1");
                context.Books.Count().ShouldEqual(1);
                context.Books.IgnoreQueryFilters().Count().ShouldEqual(2);
            }
        }

        [Fact]
        public void TestSoftDeleteServiceGetSoftDeletedEntriesWithUserIdOk()
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
                var service = new SingleSoftDeleteService<ISingleSoftDelete>(config);

                //ATTEMPT
                var orders = service.GetSoftDeletedEntries<Order>().ToList();

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