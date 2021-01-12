// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SoftDeleteServices.Configuration
{
    /// <summary>
    /// Inherit this class to configure a cascade soft delete service
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    public class CascadeSoftDeleteConfiguration<TInterface> : BaseSoftDeleteConfiguration 
        where TInterface : class
    {
        /// <summary>
        /// This must be called by your configuration constructor to provide the DbContext to be used
        /// </summary>
        /// <param name="context"></param>
        protected CascadeSoftDeleteConfiguration(DbContext context) : base(context)
        {
        }

        /// <summary>
        /// This should contain an expression that returns the soft delete value.
        ///  e.g. entity => entity.SoftDeletedLevel
        /// Ideally the expression should work in a EF Core query, but if it can't then please provide
        /// a expression for the <see cref="QuerySoftDeleteValue"/> that does work in a query
        /// </summary>
        public Expression<Func<TInterface, byte>> GetSoftDeleteValue { get; set; }

        /// <summary>
        /// OPTIONAL: if the <see cref="GetSoftDeleteValue"/> expression can't be used in a LINQ query,
        /// then you need to provide a query that will work. 
        /// e.g. <code>EF.Property{byte}(entity, "SoftDeletedLevel");</code> 
        /// </summary>
        public Expression<Func<TInterface, byte>> QuerySoftDeleteValue { get; set; }

        /// <summary>
        /// This should contain an action to set the soft delete value
        /// e.g. (entity, value) => { entity.SoftDeletedLevel = value; };
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