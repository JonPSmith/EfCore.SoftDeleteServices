// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.CascadeEfClasses;
using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.CascadeEfCode
{
    public class CascadeSoftDelDbContext : DbContext, IUserId
    {
        /// <summary>
        /// This holds the current userId, or GUID.Empty if not given
        /// </summary>
        public Guid UserId { get; private set; }

        public CascadeSoftDelDbContext(DbContextOptions<CascadeSoftDelDbContext> options, Guid userId = default)
            : base(options)
        {
            UserId = userId;
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeContract> Contracts { get; set; }

        public DbSet<Customer> Companies { get; set; }
        public DbSet<Quote> Quotes { get; set; }

        public DbSet<ShadowCascadeDelClass> ShadowCascadeDelClasses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>()
                .HasMany(x => x.WorksFromMe)
                .WithOne(x => x.Manager)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<Employee>()
                .HasOne(x => x.Contract)
                .WithOne(x => x.Employee)
                .HasForeignKey<EmployeeContract>(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<Customer>()
                .HasMany(x => x.Quotes)
                .WithOne(x => x.BelongsTo)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.Entity<Quote>()
                .HasOne(x => x.PriceInfo)
                .WithOne()
                .HasForeignKey<QuotePrice>(x => x.QuoteId)
                .OnDelete(DeleteBehavior.ClientCascade);

            //This automatically configures the query filters
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ICascadeSoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    if (typeof(IUserId).IsAssignableFrom(entityType.ClrType))
                    {
                        if (typeof(ISingleSoftDelete).IsAssignableFrom(entityType.ClrType))
                            entityType.SetCascadeQueryFilter(CascadeQueryFilterTypes.CascadeAndSingleAndUserId, this);
                        else
                            entityType.SetCascadeQueryFilter(CascadeQueryFilterTypes.CascadeSoftDeleteAndUserId, this);
                    }
                    else
                        entityType.SetCascadeQueryFilter(CascadeQueryFilterTypes.CascadeSoftDelete, this);
                }
                
            }

            modelBuilder.Entity<ShadowCascadeDelClass>()
                .Property<byte>("SoftDeleteLevel");
            modelBuilder.Entity<ShadowCascadeDelClass>()
                .HasQueryFilter(x => EF.Property<byte>(x, "SoftDeleteLevel") == 0);
        }
    }
}
