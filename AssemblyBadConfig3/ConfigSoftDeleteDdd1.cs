using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig3
{
    public class ConfigSoftDeleteDdd1 : SingleSoftDeleteConfiguration<ISingleSoftDeletedDDD>
    {

        public ConfigSoftDeleteDdd1(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => entity.ChangeSoftDeleted(value);
        }
    }
}
