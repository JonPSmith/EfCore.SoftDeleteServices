// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using SoftDeleteServices.Configuration;
using static System.Linq.Expressions.Expression;

[assembly: InternalsVisibleTo("Test")]

namespace SoftDeleteServices.Concrete.Internal
{
    internal static class ExpressionBuilder
    {
        /// <summary>
        /// This returns a where filter that returns all the valid entities that have been single soft deleted
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>> FilterToGetValueSingleSoftDeletedEntities<TEntity, TInterface>(
            this SingleSoftDeleteConfiguration<TInterface> config)
            where TInterface : class
            where TEntity : TInterface
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return FilterToGetSoftDeletedEntities<TEntity,TInterface, bool>(config.GetSoftDeleteValue, config.OtherFilters, true);
        }

        /// <summary>
        /// This returns a where filter that returns all the valid entities that have been cascade soft deleted
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="config"></param>
        /// <param name="levelToLookFor"></param>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>> FilterToGetValueCascadeSoftDeletedEntities<TEntity, TInterface>(
            this CascadeSoftDeleteConfiguration<TInterface> config, byte levelToLookFor)
            where TInterface : class
            where TEntity : TInterface
        {
            return FilterToGetSoftDeletedEntities<TEntity, TInterface, byte>(config.GetSoftDeleteValue, config.OtherFilters, levelToLookFor);
        }

        private static Expression<Func<TEntity, bool>> FilterToGetSoftDeletedEntities<TEntity, TInterface, TValue>(
            Expression<Func<TInterface, TValue>> getSoftDeleteValue,
            Dictionary<Type, Expression<Func<object, bool>>> otherFilters,
            TValue valueToFilterBy)
            where TInterface : class
            where TEntity : TInterface
        {
            var parameter = Parameter(typeof(TEntity), getSoftDeleteValue.Parameters.Single().Name);
            var left = Invoke(getSoftDeleteValue, parameter);
            var right = Constant(valueToFilterBy);
            var result = Equal(left, right);

            return AddOtherFilters<TEntity>(result, parameter, otherFilters);
        }

        /// <summary>
        /// This returns all the entities of this type that are valid, e.g. not filtered out by other parts of the Query filters
        /// Relies on the user filling in the OtherFilters part of the config
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>> FormOtherFiltersOnly<TEntity>(this Dictionary<Type, Expression<Func<object, bool>>> otherFilters)
        {
            var parameter = otherFilters.Values.Any()
                ? Parameter(typeof(TEntity), otherFilters.Values.First().Parameters.Single().Name)
                : null;
            return AddOtherFilters<TEntity>(null, parameter, otherFilters);
        }

        private static Expression<Func<TEntity, bool>> AddOtherFilters<TEntity>(
            BinaryExpression initialExpression,
            ParameterExpression parameter,
            Dictionary<Type, Expression<Func<object, bool>>> otherFilters)
        {
            if (!otherFilters.Any(x => x.Key.IsAssignableFrom(typeof(TEntity))))
                //no other filters to add, so go with the single one
                return initialExpression == null
                    ? (Expression<Func<TEntity, bool>>) null
                    : Lambda<Func<TEntity, bool>>(initialExpression, parameter);

            Expression result = initialExpression;
            foreach (var otherFilterType in otherFilters.Keys)
            {
                if (otherFilterType.IsAssignableFrom(typeof(TEntity)))
                {
                    var specificFilter = otherFilters[otherFilterType];
                    if (specificFilter.Parameters.Single().Name != parameter.Name)
                        throw new InvalidOperationException(
                            $"The filter parameter for {otherFilterType.Name} must must be the same in all usages , i.e. {parameter.Name}.");

                    if (result == null)
                        result = Invoke(otherFilters[otherFilterType], parameter);
                    else
                        result = AndAlso(result, Invoke(otherFilters[otherFilterType], parameter));
                }
            }

            return Lambda<Func<TEntity, bool>>(result, parameter);
        }
    }
}