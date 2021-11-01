// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.Demos
{
    public class Demo3
    {
        private ITestOutputHelper _output;

        public Demo3(ITestOutputHelper output)
        {
            _output = output;
        }

        public class MyEntity
        {
            public int Id { get; set; }
            public bool SoftDeleted { get; set; }
        }

        public class MyDbContext : DbContext
        {
            public MyDbContext(DbContextOptions<MyDbContext> options)
                : base(options)
            {
            }

            public DbSet<MyEntity> MyEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<MyEntity>()
                    .HasQueryFilter(x => !x.SoftDeleted);

                modelBuilder.Entity<MyEntity>()
                    .HasIndex(x => x.SoftDeleted);
            }
        }

        [Fact]
        public void TestDemo()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<MyDbContext>();
            using var context = new MyDbContext(options);
            context.Database.EnsureCreated();

            var e1 = new MyEntity { SoftDeleted = false};
            var e2 = new MyEntity { SoftDeleted = true };
            context.AddRange(e1,e2);
            context.SaveChanges();

            //ATTEMPT
            var query = RelationalQueryableExtensions.FromSqlRaw(context.MyEntities, "SELECT * FROM MyEntities");
            var entities = query.ToList();

            //VERIFY
            var sql = query.ToQueryString();
            _output.WriteLine(sql);
            entities.Count().ShouldEqual(1);
        }
    }
}