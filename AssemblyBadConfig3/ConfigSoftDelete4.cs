using DataLayer.Interfaces;
using DataLayer.SingleEfCode;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig3
{
    public class ConfigSoftDelete4 : SingleSoftDeleteConfiguration<ISingleSoftDelete>
    {

        public ConfigSoftDelete4(SingleSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleted = value; };
        }
    }
}
