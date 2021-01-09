using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig3
{
    public class ConfigSoftDelete3 : SingleSoftDeleteConfiguration<ISingleSoftDelete>
    {

        public ConfigSoftDelete3(CascadeSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleted = value; };
        }
    }
}
