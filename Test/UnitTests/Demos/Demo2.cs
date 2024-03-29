﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.Demos
{
    public class Demo2
    {
        public class Principal
        {
            public int Id { get; set; }
            public List<Dependent> Dependents { get; set; }
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

            var e1 = new Principal{ Dependents = new List<Dependent>
            {
                new Dependent { SoftDeleted = false },
                new Dependent { SoftDeleted = true }
            }};

            context.Add(e1);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            //ATTEMPT
            var principal = context.Principals
                .Include(x => x.Dependents)
                .Single();

            //VERIFY
            principal.Dependents.Count.ShouldEqual(1);
        }
    }
}