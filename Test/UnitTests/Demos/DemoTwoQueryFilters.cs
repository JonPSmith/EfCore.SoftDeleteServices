// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.Demos
{
    public class DemoTwoQueryFilters
    {
        public class MyEntity
        {
            public int Id { get; set; }
            public bool SoftDeleted { get; set; }

            public string UserId { get; set; }
        }

        public class MyDbContext : DbContext
        {
            private readonly string _userId;

            public MyDbContext(DbContextOptions<MyDbContext> options, string userId)
                : base(options)
            {
                _userId = userId;
            }

            public DbSet<MyEntity> MyEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<MyEntity>()
                    .HasQueryFilter(x => !x.SoftDeleted && x.UserId == _userId);

                modelBuilder.Entity<MyEntity>()
                    .HasIndex(x => x.SoftDeleted);
                modelBuilder.Entity<MyEntity>()
                    .HasIndex(x => x.UserId);
            }
        }

        [Fact]
        public void TestTwoQueryFilters()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<MyDbContext>();
            var userId = "GoodId";
            using var context = new MyDbContext(options, userId);
            context.Database.EnsureCreated();

            //ATTEMPT
            var e1 = new MyEntity { SoftDeleted = false, UserId = userId};
            var e2 = new MyEntity { SoftDeleted = true,  UserId = userId };
            var e3 = new MyEntity { SoftDeleted = false, UserId = "BadId" };
            context.AddRange(e1,e2, e3);
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
            context.MyEntities.Count().ShouldEqual(1);
            context.MyEntities.IgnoreQueryFilters().Count().ShouldEqual(3);
        }
    }
}