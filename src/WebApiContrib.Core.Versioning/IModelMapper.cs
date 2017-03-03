using System;
using System.Collections.Generic;

namespace WebApiContrib.Core.Versioning
{

    public interface IModelMapper
    {
        Type ResultType { get; }

        object MapCollection(IEnumerable<object> collection);

        object Map(object model);
    }
}