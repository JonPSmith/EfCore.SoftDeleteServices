// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using SoftDeleteServices.Configuration;

namespace Test.ExampleConfigs
{
    public class ConfigCascadeDeleteWithUserId : CascadeSoftDeleteConfiguration<ICascadeSoftDelete>
    {

        public ConfigCascadeDeleteWithUserId(CascadeSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleteLevel;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleteLevel = value; };
            OtherFilters.Add(typeof(IUserId), entity => ((IUserId)entity).UserId == context.UserId);
        }

    }
}