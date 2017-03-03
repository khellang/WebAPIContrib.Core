using System;

namespace WebApiContrib.Core.Versioning
{
    public interface IModelMapperProvider
    {
        Type ModelType { get; }

        IModelMapperRegistry GetRegistry();
    }
}