using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Fusillade;

namespace Shiny.WebApi
{
    public interface IWebApi<TWebApi>
    {
        IObservable<TResult> Execute<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod, Priority priority = Priority.UserInitiated);
        Task<TResult> ExecuteAsync<TResult>(Expression<Func<TWebApi, Task<TResult>>> executeApiMethod, Priority priority = Priority.UserInitiated);
        Task ExecuteAsync(Expression<Func<TWebApi, Task>> executeApiMethod, Priority priority = Priority.UserInitiated);
    }
}
