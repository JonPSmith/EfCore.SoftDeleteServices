// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.Demos
{
    public class DemoAutoConfigQueryFilter
    {
        public interface ISoftDelete
        {
            bool SoftDeleted { get; set; }
        }

        public class MyEntity : ISoftDelete
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

            private void AddSoftDeleteQueryFilter(IMutableEntityType entityData)
            {
                var methodToCall = GetType()
                    .GetMethod(nameof(GetSoftDeleteFilter),
                        BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(entityData.ClrType);
                var filter = methodToCall.Invoke(this, new object[] { });
                entityData.SetQueryFilter((LambdaExpression)filter);
                entityData.AddIndex(entityData.FindProperty(nameof(ISingleSoftDelete.SoftDeleted)));
            }

            private LambdaExpression GetSoftDeleteFilter<TEntity>()
                where TEntity : class, ISoftDelete
            {
                Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
                return filter;
            }

            public DbSet<MyEntity> MyEntities { get; set; }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                    {
                        AddSoftDeleteQueryFilter(entityType);
                    }
                }
            }
        }

        [Fact]
        public void TestDemo()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<MyDbContext>();
            using var context = new MyDbContext(options);
            context.Database.EnsureCreated();

            //ATTEMPT
            var e1 = new MyEntity { SoftDeleted = false};
            var e2 = new MyEntity { SoftDeleted = true };
            context.AddRange(e1,e2);
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
            context.MyEntities.Count().ShouldEqual(1);
            context.MyEntities.IgnoreQueryFilters().Count().ShouldEqual(2);
        }
    }
}