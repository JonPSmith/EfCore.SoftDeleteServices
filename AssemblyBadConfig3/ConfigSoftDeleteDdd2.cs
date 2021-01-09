using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig3
{
    public class ConfigSoftDeleteDdd2 : SingleSoftDeleteConfiguration<ISingleSoftDeletedDDD>
    {

        public ConfigSoftDeleteDdd2(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => entity.ChangeSoftDeleted(value);
        }
    }
}
