// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Concrete;
using SoftDeleteServices.Configuration;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.IssuesTests
{
    public class TestIssue011
    {
        [Fact]
        public void TestSetupDatabaseOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<TestDbContext>();
            using var context = new TestDbContext(options);
            context.Database.EnsureCreated();

            //ATTEMPT
            var company = new Company
            {
                Name = "test",
                ExternalIds = new List<ExternalId> { new ExternalId { Context = "context" } }
            };
            context.Add(company);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            //VERIFY
            var readC = context.Companies
                .Include(x => x.Address)
                .Include(x => x.ExternalIds)
                .Single();
            readC.Address.ShouldBeNull();
            readC.ExternalIds.Count.ShouldEqual(1);
        }

        [Fact]
        public void TestCascadeDeleteOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<TestDbContext>();
            using var context = new TestDbContext(options);
            context.Database.EnsureCreated();

            var company = new Company
            {
                Name = "test",
                ExternalIds = new List<ExternalId> { new ExternalId { Context = "context" } }
            };
            context.Add(company);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var config = new ConfigCascadeTestDbContext(context);
            var service = new CascadeSoftDelService<ICascadeSoftDelete>(config);

            //ATTEMPT
            var status = service.SetCascadeSoftDeleteViaKeys<Company>(company.Id);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual("You have soft deleted an entity and its 1 dependents");
            var readC = context.Companies.IgnoreQueryFilters().Single();
            readC.SoftDeleteLevel.ShouldEqual((byte)1);
            var readE= context.ExternalIds.IgnoreQueryFilters().Single();
            readE.SoftDeleteLevel.ShouldEqual((byte)2);
        }


        public class ConfigCascadeTestDbContext : CascadeSoftDeleteConfiguration<ICascadeSoftDelete>
        {
            public ConfigCascadeTestDbContext(TestDbContext context)
                : base(context)
            {
                GetSoftDeleteValue = entity => entity.SoftDeleteLevel;
                SetSoftDeleteValue = (entity, value) => { entity.SoftDeleteLevel = value; };
            }
        }

        public class TestDbContext : DbContext, IUserId
        {
            public Guid UserId { get; }

            public TestDbContext(DbContextOptions<TestDbContext> options)
                : base(options)
            { }

            public DbSet<Company> Companies { get; set; }
            public DbSet<Address> Addresses { get; set; }
            public DbSet<ExternalId> ExternalIds { get; set; }


            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<Address>()
                    .HasOne(c => c.Company)
                    .WithOne(p => p.Address)
                    .HasForeignKey<Address>(a => a.CompanyId)
                    .OnDelete(DeleteBehavior.ClientCascade)
                    .HasConstraintName("FK_Addresses_Companies");

                modelBuilder.Entity<ExternalId>()
                    .HasOne(e => e.Company)
                    .WithMany(c => c.ExternalIds)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.ClientCascade)
                    .HasConstraintName("FK_ExternalId_Company");
            }


        }

        public class Company : ICascadeSoftDelete
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Address? Address { get; set; }
            public ICollection<ExternalId>? ExternalIds { get; set; }
            public byte SoftDeleteLevel { get; set; }
        }

        public class Address : ICascadeSoftDelete
        {
            public int Id { get; set; }
            public string? Street { get; set; }
            public string? City { get; set; }
            public int? CompanyId { get; set; }
            public Company? Company { get; set; }
            public byte SoftDeleteLevel { get; set; }
        }

        public class ExternalId : ICascadeSoftDelete
        {
            public int Id { get; set; }
            public string Context { get; set; }
            public string Value { get; set; }
            public int? CompanyId { get; set; }
            public Company? Company { get; set; }
            public byte SoftDeleteLevel { get; set; }
        }
    }
}