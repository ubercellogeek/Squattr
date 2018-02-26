using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Squattr.RESTAPI.Services.Authentication;
using System.Collections.Generic;
using System.Configuration;
using Squattr.RESTAPI.API.Async;

namespace Squattr.RESTAPI.API
{
    /// <summary>
    /// Configures AutoFac dependency injection for the application.
    /// </summary>
    public static class AutofacConfig
    {
        public static void RegisterWebAPI(HttpConfiguration config)
        {
            ContainerBuilder builder = new ContainerBuilder();
            RegisterServices(builder);

            builder.RegisterApiControllers(typeof(AutofacConfig).Assembly);

            IContainer container = builder.Build();

            AutofacWebApiDependencyResolver resolver = new AutofacWebApiDependencyResolver(container);
            config.DependencyResolver = resolver;
        }

        private static void RegisterServices(ContainerBuilder builder)
        {
            // Register service layer
            Assembly[] refAssemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();
            builder.RegisterAssemblyTypes(refAssemblies)
                .InNamespace("Squattr.RESTAPI.Services")
                .Where(t => t.Name.EndsWith("Service")).AsSelf().InstancePerRequest();

            // Register single-instance auth provider
            var oAuthParams = new List<Autofac.Core.Parameter>();
            oAuthParams.Add(new NamedParameter("ClientId", ConfigurationManager.AppSettings["ClientId"]));
            oAuthParams.Add(new NamedParameter("ClientSecret", ConfigurationManager.AppSettings["ClientSecret"]));
            oAuthParams.Add(new NamedParameter("TennantId", ConfigurationManager.AppSettings["TennantId"]));
            builder.RegisterType<GraphOAuth2Provider>().WithParameters(oAuthParams).SingleInstance();
            builder.Register(c => new HttpContextWrapper(HttpContext.Current)).As<HttpContextBase>().InstancePerRequest();
            builder.RegisterType<AsyncRunner>().As<IAsyncRunner>().SingleInstance();
        }
    }
}