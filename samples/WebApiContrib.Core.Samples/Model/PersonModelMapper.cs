using WebApiContrib.Core.Versioning;

namespace WebApiContrib.Core.Samples.Model
{
    public class PersonModelMapper : ModelMapperProvider<PersonModel>
    {
        protected override void Populate(ModelMapperRegistry registry)
        {
            registry.MapDefault(model => model);
            registry.Map(2, model => new PersonModel.V2(model));
        }
    }
}