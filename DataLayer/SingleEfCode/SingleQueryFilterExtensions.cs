// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DataLayer.SingleEfCode
{
    public enum SingleQueryFilterTypes { SingleSoftDelete, SingleSoftDeleteAndUserId, SingleSoftDeleteDdd }    

    public static class SingleQueryFilterExtensions                            
    {
        public static void SetSingleQueryFilter(this IMutableEntityType entityData,  
            SingleQueryFilterTypes queryFilterType, IUserId userIdProvider = null)                    
        {
            var methodName = $"Get{queryFilterType}Filter";        
            var methodToCall = typeof(SingleQueryFilterExtensions)
                .GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(entityData.ClrType);
            var filter = methodToCall                              
                .Invoke(null, new object[] { userIdProvider });                   
            entityData.SetQueryFilter((LambdaExpression)filter);   
        }

        private static LambdaExpression GetSingleSoftDeleteFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, ISingleSoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
            return filter;
        }

        private static LambdaExpression GetSingleSoftDeleteAndUserIdFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, IUserId, ISingleSoftDelete
        {
            Expression<Func<TEntity, bool>> filter = x => x.UserId == userIdProvider.UserId && !x.SoftDeleted;
            return filter;
        }

        private static LambdaExpression GetSingleSoftDeleteDddFilter<TEntity>(IUserId userIdProvider)
            where TEntity : class, ISingleSoftDeletedDDD
        {
            Expression<Func<TEntity, bool>> filter = x => !x.SoftDeleted;
            return filter;
        }

    }
}