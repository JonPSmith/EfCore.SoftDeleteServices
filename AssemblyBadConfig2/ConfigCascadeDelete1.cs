// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig2
{
    public class ConfigCascadeDelete1 : CascadeSoftDeleteConfiguration<ICascadeSoftDelete>
    {

        public ConfigCascadeDelete1(CascadeSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleteLevel;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleteLevel = value; };
        }

    }
}