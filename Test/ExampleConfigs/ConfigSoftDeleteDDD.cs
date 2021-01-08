// Copyright (c) 2020 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Configuration;

namespace Test.ExampleConfigs
{
    public class ConfigSoftDeleteDDD : SingleSoftDeleteConfiguration<ISingleSoftDeletedDDD>
    {

        public ConfigSoftDeleteDDD(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => entity.ChangeSoftDeleted(value);
        }
    }
}