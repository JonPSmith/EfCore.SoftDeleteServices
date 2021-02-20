// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataLayer.CascadeEfCode
{
    public enum CascadeQueryFilterTypes { CascadeSoftDelete, CascadeSoftDeleteAndUserId, CascadeAndSingleAndUserId }

    public static class CascadeQueryFilterExtensions
    {
        public static void SetCascadeQueryFilter(this IMutableEntityType entityData,
            CascadeQueryFilterTypes queryFilterType, IUserId userIdProvider)
        {
            var methodName = $"Get{queryFilterType}Filter";
            var methodToCall = typeof(CascadeQueryFilterExtensions)
                .GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall
                .Invoke(null, new object[] { userIdProvider });
            entityData.SetQueryFilter((LambdaExpression)filter);

            if (queryFilterType == CascadeQueryFilterTypes.CascadeSoftDelete)
                entityData.AddIndex(entityData.FindProperty(nameof(ICascadeSoftDelete.SoftDeleteLevel)));
            if (queryFilterType == CascadeQueryFilterTypes.CascadeSoftDeleteAndUserId ||
                queryFilterType == CascadeQueryFilterTypes.CascadeAndSingleAndUserId)
                entityData.AddIndex(entityData.FindProperty(nameof(IUserId.UserId)));
            if (queryFilterType == CascadeQueryFilterTypes.CascadeAndSingleAndUserId)
                entityData.AddIndex(entityData.FindProperty(nameof(ISingleSoftDelete.SoftDeleted)));
        }

        private static LambdaExpression GetCascadeSoftDeleteFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, ICascadeSoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => x.SoftDeleteLevel == 0;
            return filter;
        }

        private static LambdaExpression GetCascadeSoftDeleteAndUserIdFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, ICascadeSoftDelete, IUserId
        {
            Expression<Func<TEntity, bool>> filter = x => x.SoftDeleteLevel == 0 && x.UserId == userIdProvider.UserId;
            return filter;
        }

        private static LambdaExpression GetCascadeAndSingleAndUserIdFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, ICascadeSoftDelete, ISingleSoftDelete, IUserId
        {
            Expression<Func<TEntity, bool>> filter = x => x.SoftDeleteLevel == 0 
                                                          && !x.SoftDeleted
                                                          && x.UserId == userIdProvider.UserId;
            return filter;
        }
    }
}