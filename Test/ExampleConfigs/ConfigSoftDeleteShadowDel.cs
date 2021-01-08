// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Configuration;

namespace Test.ExampleConfigs
{
    public class ConfigSoftDeleteShadowDel : SingleSoftDeleteConfiguration<IShadowSoftDelete>
    {

        public ConfigSoftDeleteShadowDel(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => EF.Property<bool>(entity, "SoftDeleted");
            SetSoftDeleteValue = (entity, value) => { context.Entry(entity).Property("SoftDeleted").CurrentValue = value; }; 
        }
    }
}