using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WebApiContrib.Core.Versioning
{
    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class ModelMapperProvider<TModel> : IModelMapperProvider
    {
        Type IModelMapperProvider.ModelType => typeof(TModel);

        IModelMapperRegistry IModelMapperProvider.GetRegistry()
        {
            var registry = new ModelMapperRegistry();

            Populate(registry);

            return registry;
        }

        protected abstract void Populate(ModelMapperRegistry registry);

        public override string ToString()
        {
            return typeof(TModel).Name;
        }

        [DebuggerTypeProxy(typeof(ModelMapperProvider<>.ModelMapperRegistry.DebuggerProxy))]
        protected class ModelMapperRegistry : IModelMapperRegistry
        {
            private IDictionary<int, IModelMapper> Mappers { get; } = new Dictionary<int, IModelMapper>();

            private IModelMapper DefaultMapper { get; set; }

            bool IModelMapperRegistry.TryGetMapper(int? version, out IModelMapper mapper)
            {
                if (version.HasValue)
                {
                    if (Mappers.TryGetValue(version.Value, out mapper))
                    {
                        return true;
                    }
                }

                if (DefaultMapper == null)
                {
                    mapper = null;
                    return false;
                }

                mapper = DefaultMapper;
                return true;
            }

            public ModelMapperRegistry MapDefault<TResult>(Func<TModel, TResult> mapper)
            {
                DefaultMapper = new ModelMapper<TResult>(null, mapper);
                return this;
            }

            public ModelMapperRegistry Map<TResult>(int version, Func<TModel, TResult> mapper)
            {
                Mappers[version] = new ModelMapper<TResult>(version, mapper);
                return this;
            }

            [DebuggerDisplay("{ToString(),nq}")]
            private class ModelMapper<TResult> : IModelMapper
            {
                public ModelMapper(int? version, Func<TModel, TResult> mapper)
                {
                    Version = version;
                    Mapper = mapper;
                }

                private int? Version { get; }

                private Func<TModel, TResult> Mapper { get; }

                public Type ResultType => typeof(TResult);

                public object MapCollection(IEnumerable<object> collection)
                {
                    var list = new List<TResult>();

                    foreach (var value in collection)
                    {
                        list.Add((TResult) Map(value));
                    }

                    return list;
                }

                public object Map(object model)
                {
                    return Mapper((TModel) model);
                }

                public override string ToString()
                {
                    var versionString = Version.HasValue ? Version.Value.ToString() : "Any";
                    return $"{typeof(TModel).Name} v{versionString} -> {typeof(TResult).Name}";
                }
            }

            private class DebuggerProxy
            {
                public DebuggerProxy(ModelMapperRegistry registry)
                {
                    var mappers = registry.Mappers.Select(x => x.Value);

                    if (registry.DefaultMapper != null)
                    {
                        mappers = mappers.Concat(new[] { registry.DefaultMapper });
                    }

                    Mappers = mappers.ToArray();
                }

                [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
                public IModelMapper[] Mappers { get; }
            }
        }
    }
}