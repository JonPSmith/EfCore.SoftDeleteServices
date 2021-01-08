// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SoftDeleteServices.Configuration
{
    public class BaseSoftDeleteConfiguration
    {
        protected BaseSoftDeleteConfiguration(DbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// This is used by the soft delete services to gain access to the specific DbContext used
        /// </summary>
        internal DbContext Context { get; }

        /// <summary>
        /// If you have other query filters, such as a multi-tenant system with a UserId or DataKey,
        /// then you MUST add the same query(s) to this dictionary.
        /// This will apply these filters to all the soft delete commands to make sure you aren't seeing/altering an entry you shouldn't be able to access
        /// See this example https://github.com/JonPSmith/EfCore.SoftDeleteServices/blob/master/Test/ExampleConfigs/ConfigSoftDeleteWithUserId.cs 
        /// </summary>
        public Dictionary<Type, Expression<Func<object, bool>>> OtherFilters { get; } = new Dictionary<Type, Expression<Func<object, bool>>>();

        /// <summary>
        /// If this property is set to true, then it won't produce an error 
        /// This is useful for Web APIs where not finding something require a different return
        /// </summary>
        public bool NotFoundIsNotAnError { get; set; }

        //The following text properties allows you to change what the user sees. 
        //For instance, you might refer to soft deleting something as "deleted" where only the Admin can get it back

        /// <summary>
        /// This text is used in various errors and success messages when talking about soft deleting
        /// for instance "This entry is already soft deleted" 
        /// </summary>
        public string TextSoftDeletedPastTense { get; set; } = "soft deleted";

        public string TextHardDeletedPastTense { get; set; } = "hard deleted";

        public string TextResetSoftDelete { get; set; } = "reset the soft delete";
    }
}