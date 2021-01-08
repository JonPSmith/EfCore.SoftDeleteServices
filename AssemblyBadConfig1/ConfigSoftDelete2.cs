
using DataLayer.CascadeEfCode;
using DataLayer.Interfaces;
using SoftDeleteServices.Configuration;

namespace AssemblyBadConfig1
{
    public class ConfigSoftDelete2 : SingleSoftDeleteConfiguration<ISingleSoftDelete>
    {

        public ConfigSoftDelete2(CascadeSoftDelDbContext context)
            : base(context)
        {
            GetSoftDeleteValue = entity => entity.SoftDeleted;
            SetSoftDeleteValue = (entity, value) => { entity.SoftDeleted = value; };
        }
    }
}
