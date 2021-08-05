// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.Demos
{
    public class DemoOneToOne
    {
        public class Principal
        {
            public int Id { get; set; }
            public Dependent Dependent { get; set; }
        }

        public class Dependent
        {
            public int Id { get; set; }
            public bool SoftDeleted { get; set; }

            public int PrincipalId { get; set; }
        }

        public class MyDbContext : DbContext
        {
            public MyDbContext(DbContextOptions<MyDbContext> options)
                : base(options)
            {
            }

            public DbSet<Principal> Principals { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Dependent>()
                    .HasQueryFilter(x => !x.SoftDeleted);

                modelBuilder.Entity<Dependent>()
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

            var e1 = new Principal
            {
                Dependent = new Dependent { SoftDeleted = true }
            };
            context.Add(e1);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            context.Principals.Include(x => x.Dependent)
                .Single().Dependent.ShouldBeNull();

            //ATTEMPT
            var principal = context.Principals.Single();
            principal.Dependent = new Dependent();

            var ex = Assert.Throws<DbUpdateException>(() => context.SaveChanges());

            //VERIFY
            ex.InnerException.Message.ShouldEqual("SQLite Error 19: 'UNIQUE constraint failed: Dependent.PrincipalId'.");
        }
    }
}