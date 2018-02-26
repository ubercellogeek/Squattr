using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Diagnostics;

namespace Squattr.RESTAPI.API.Filters
{
    /// <summary>
    /// Authorization filter for the static API key.
    /// </summary>
    public class APIKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (AuthorizeRequest(actionContext))
            {
                return;
            }

            HandleUnauthorizedRequest(actionContext);
        }

        protected override void HandleUnauthorizedRequest(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            actionContext.Response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden);
        }

        /// <summary>
        /// Authorizes a given request based on the existence of the API key.
        /// </summary>
        /// <param name="actionContext">The current <see cref="HttpActionContext"/> to inspect.</param>
        private bool AuthorizeRequest(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (Debugger.IsAttached)
            {
                return true;
            }

            List<string> values = new List<string>();
            
            try
            {
                values = actionContext.Request.Headers.GetValues("APIKey").ToList();
            }
            catch (Exception)
            { }

            if (values.Count == 0)
            {
                return false;
            }
            else if (values[0] != ConfigurationManager.AppSettings["APIKey"])
            {
                return false;
            }

            return true;
        }
    }
}