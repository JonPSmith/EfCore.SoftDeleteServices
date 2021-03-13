// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SoftDeleteServices.Concrete.Internal
{
    public static class CheckOneToOne
    {
        public static void ThrowExceptionIfPrincipalOneToOne(this DbContext context, object entityToCheck)
        {
            var keys = context.Entry(entityToCheck).Metadata.GetForeignKeys();
            if (!keys.All(x =>
                    x.DependentToPrincipal?.IsCollection == true || // many-to-one
                    x.PrincipalToDependent?.IsCollection == true || // one-to-many
                    x.DependentToPrincipal?.ForeignKey.DeclaringEntityType.ClrType == entityToCheck.GetType())
                ) // one-to-one, but foreign key is in this entity
                //This it is a one-to-one entity - setting a one-to-one as soft deleted causes problems when you try to create a replacement
                throw new InvalidOperationException("You cannot soft delete a one-to-one relationship. " +
                                                    "It causes problems if you try to create a new version.");
        }
    }
}