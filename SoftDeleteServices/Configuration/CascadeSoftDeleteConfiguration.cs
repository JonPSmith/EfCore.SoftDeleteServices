// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SoftDeleteServices.Configuration
{
    public class CascadeSoftDeleteConfiguration<TInterface> : BaseSoftDeleteConfiguration 
        where TInterface : class
    {
        public CascadeSoftDeleteConfiguration(DbContext context) : base(context)
        {
        }

        /// <summary>
        /// This should contain a LINQ query that returns the soft delete value - MUST work in EF Core query
        /// e.g. entity => entity.SoftDeleted
        /// </summary>
        public Expression<Func<TInterface, byte>> GetSoftDeleteValue { get; set; }

        /// <summary>
        /// This should contain an action to set the soft delete value
        /// e.g. (entity, value) => { entity.SoftDeleted = value; };
        /// </summary>
        public Action<TInterface, byte> SetSoftDeleteValue { get; set; }


        //------------------------------------------------
        //Cascade only properties

        /// <summary>
        /// If you are using my approach to collections, where a collection is null if it isn't loaded, then you can
        /// improve the performance of Cascade soft delete by loading the entity with Includes to load the collections and setting this property to false
        /// NOTE: It only works on SetCascadeSoftDelete as on the reset you can't include soft deleted entities 
        /// </summary>
        public bool ReadEveryTime { get; set; } = true;
    }
}