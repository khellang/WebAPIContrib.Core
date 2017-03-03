using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Reflection;

namespace WebApiContrib.Core.Versioning
{
    /// <summary>
    /// A filter to negotiate resource version representations based on different version strategies.
    /// </summary>
    public class VersionNegotiationResultFilter : IResultFilter
    {
        /// <summary>
        /// Creates a new instance of <see cref="VersionNegotiationResultFilter"/>.
        /// </summary>
        /// <param name="options">The versioning options.</param>
        public VersionNegotiationResultFilter(IVersionStrategy strategy, IEnumerable<IModelMapperProvider> mapperProviders, IOptions<VersionNegotiationOptions> options)
        {
            Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            MapperProviders = mapperProviders ?? throw new ArgumentNullException(nameof(mapperProviders));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private IVersionStrategy Strategy { get; }

        private IEnumerable<IModelMapperProvider> MapperProviders { get; }

        private IOptions<VersionNegotiationOptions> Options { get; }

        /// <inheritdoc />
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = context.Result as ObjectResult;

            if (result == null || result.Value == null)
            {
                return;
            }

            var versionResult = Strategy.GetVersion(context.HttpContext, context.RouteData);

            if (versionResult.HasValue)
            {
                var value = versionResult.Value;

                var newValue = MapResult(result, value.Version);

                context.Result = new ObjectResult(newValue);

                if (Options.Value.EmitVaryHeader && !string.IsNullOrEmpty(value.VaryOn))
                {
                    context.HttpContext.Response.Headers.Append("Vary", value.VaryOn);
                }

                return;
            }
            else
            {
                var newValue = MapResult(result, null);

                context.Result = new ObjectResult(newValue);
            }
        }

        /// <inheritdoc />
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // Meh. Not used.
        }

        private object MapResult(ObjectResult result, int? version)
        {
            var resultType = GetResultType(result, out var isCollection);

            var mapper = GetMapper(resultType, version);

            if (mapper == null || resultType == mapper.ResultType)
            {
                return result.Value;
            }

            if (isCollection)
            {
                return mapper.MapCollection((IEnumerable<object>) result.Value);
            }

            return mapper.Map(result.Value);
        }

        private static Type GetResultType(ObjectResult result, out bool isCollection)
        {
            var resultType = result.DeclaredType;

            if (resultType == null || resultType == typeof(object))
            {
                resultType = result.Value.GetType();
            }

            if (resultType != typeof(string))
            {
                if (resultType.IsArray)
                {
                    isCollection = true;
                    return resultType.GetElementType();
                }

                if (resultType.IsAssignableToGenericTypeDefinition(typeof(IEnumerable<>), out var typeArguments))
                {
                    isCollection = true;
                    return typeArguments[0];
                }
            }

            isCollection = false;
            return resultType;
        }

        private IModelMapper GetMapper(Type type, int? version)
        {
            var provider = GetProvider(type);

            if (provider != null)
            {
                // TODO: Cache registry.
                var registry = provider.GetRegistry();

                if (registry.TryGetMapper(version, out var mapper))
                {
                    return mapper;
                }
            }

            if (Options.Value.ThrowOnMissingMapper)
            {
                throw new MissingModelMapperException(type);
            }

            return null;
        }

        private IModelMapperProvider GetProvider(Type type)
        {
            return MapperProviders.FirstOrDefault(x => x.ModelType.GetTypeInfo().IsAssignableFrom(type));
        }
    }
}