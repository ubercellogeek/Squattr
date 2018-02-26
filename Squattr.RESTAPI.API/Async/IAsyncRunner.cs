using Autofac;
using Autofac.Core.Lifetime;
using System;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Squattr.RESTAPI.API.Async
{
    /// <summary>
    /// Interface which when implemented will take in a Type T and run an action on it.
    /// </summary>
    public interface IAsyncRunner
    {
        Task Run<T>(Action<T> action);
    }
}