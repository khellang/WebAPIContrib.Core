namespace WebApiContrib.Core.Versioning
{
    public interface IModelMapperRegistry
    {
        bool TryGetMapper(int? version, out IModelMapper mapper);
    }
}