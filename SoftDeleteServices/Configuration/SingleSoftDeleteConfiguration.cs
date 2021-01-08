// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SoftDeleteServices.Configuration
{
    public class SingleSoftDeleteConfiguration<TInterface> : BaseSoftDeleteConfiguration 
        where TInterface : class
    {
        public SingleSoftDeleteConfiguration(DbContext context) : base(context)
        {
        }

        /// <summary>
        /// This should contain a LINQ query that returns the soft delete value - MUST work in EF Core query
        /// e.g. entity => entity.SoftDeleted
        /// </summary>
        public Expression<Func<TInterface, bool>> GetSoftDeleteValue { get; set; }

        /// <summary>
        /// This should contain an action to set the soft delete value
        /// e.g. (entity, value) => { entity.SoftDeleted = value; };
        /// </summary>
        public Action<TInterface, bool> SetSoftDeleteValue { get; set; }
    }
}