using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Shiny.WebApi
{
    public interface IWebApi<TWebApi>
    {
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod);
        Task ExecuteAsync(Expression<Func<TWebApi, Task>> executeApiMethod);
    }
}
