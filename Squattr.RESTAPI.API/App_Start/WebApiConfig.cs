using System.Linq;
using System.Web.Http;

namespace Squattr.RESTAPI.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Dependency Injection
            AutofacConfig.RegisterWebAPI(config);

            // Attribute routes
            config.MapHttpAttributeRoutes();

            // Default routes
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Set the default formatter to be JSON (XML is sooo 1999)
            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes.FirstOrDefault(t => t.MediaType == "application/xml");
            config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);

            // TODO: We should probably have some sort of api-wide error handling solution here once this thing gets famous.
        }
    }
}
