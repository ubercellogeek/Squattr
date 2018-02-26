using Autofac;
using Autofac.Core.Lifetime;
using System;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Squattr.RESTAPI.API.Async
{
    /// <summary>
    /// Implementation of the <see cref="IAsyncRunner"/> interface which provides a way to inject a graph into a seperate
    /// asynchronous task via the HostingEnvironment.QueueBackgroundWorkItem() method.
    /// </summary>
    public class AsyncRunner : IAsyncRunner
    {
        public ILifetimeScope LifetimeScope { get; set; }

        public AsyncRunner(ILifetimeScope lifetimeScope)
        {
            LifetimeScope = lifetimeScope;
        }

        /// <summary>
        /// This method runs an <see cref="Action"/> on for a given type.
        /// </summary>
        /// <typeparam name="T">The type to use for this action.</typeparam>
        /// <param name="action">The action to run in the background.</param>
        public async Task Run<T>(Action<T> action)
        {
            await Task.Run(() => {
                HostingEnvironment.QueueBackgroundWorkItem(ct =>
                {
                    // Create a nested container which will use the same dependency
                    // registrations as set for HTTP request scopes.
                    using (var container = LifetimeScope.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag))
                    {
                        var service = container.Resolve<T>();
                        action(service);
                    }
                });
            });
        }
    }
}