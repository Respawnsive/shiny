using System.Collections.Generic;
using System.Text;

namespace Shiny.WebApi.Lazying
{
    public interface ILazyDependency<T>
    {
        T Value { get; }
    }
}
