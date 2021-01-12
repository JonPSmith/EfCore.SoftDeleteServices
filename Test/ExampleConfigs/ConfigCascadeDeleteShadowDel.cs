// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using Microsoft.EntityFrameworkCore;
using SoftDeleteServices.Configuration;

namespace Test.ExampleConfigs
{
    public class ConfigCascadeDeleteShadowDel : CascadeSoftDeleteConfiguration<IShadowCascadeSoftDelete>
    {

        public ConfigCascadeDeleteShadowDel(CascadeSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => (byte)context.Entry(entity).Property("SoftDeleteLevel").CurrentValue;
            QuerySoftDeleteValue = entity => EF.Property<byte>(entity, "SoftDeleteLevel");
            SetSoftDeleteValue = (entity, value) => context.Entry(entity).Property("SoftDeleteLevel").CurrentValue = value;
        }

    }
}