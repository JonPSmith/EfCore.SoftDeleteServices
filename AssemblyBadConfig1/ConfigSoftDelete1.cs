
using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig1
{
    public class ConfigSoftDelete1 : SingleSoftDeleteConfiguration<ISingleSoftDelete>
    {

        public ConfigSoftDelete1(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleted = value; };
        }
    }
}
