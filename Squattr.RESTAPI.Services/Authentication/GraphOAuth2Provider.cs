using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Squattr.RESTAPI.Services.Authentication
{
    /// <summary>
    /// A custom Graph API authentication provider for stand-alone tennant based applications.
    /// </summary>
    public class GraphOAuth2Provider : IAuthenticationProvider
    {
        #region Constants

        private const string _tokenServiceUrlTemplate = "https://login.microsoftonline.com/{0}/oauth2/token";
        private const string _resourceUri = "https://graph.microsoft.com";

        #endregion

        #region Local Variables

        private string _tokenServiceUrl;
        private string _clientId;
        private string _clientSecret; 
        private string _tennantId;
        private string _bearerToken;
        private DateTimeOffset _expiration;
        private IHttpProvider _httpProvider;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="GraphOAuth2Provider"/> for authentication in stand-alone tennant applications.
        /// </summary>
        public GraphOAuth2Provider(string ClientId, string ClientSecret, string TennantId, IHttpProvider Provider = null)
        {
            _clientId = ClientId;
            _clientSecret = ClientSecret;
            _tennantId = TennantId;
            _tokenServiceUrl = string.Format(_tokenServiceUrlTemplate, _tennantId);
            _httpProvider = Provider ?? new HttpProvider();
        }

        #endregion

        #region Public Implementation Methods

        /// <summary>
        /// Adds the current access token to the request headers. This method will silently refresh the access
        /// token, if needed.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/> to authenticate.</param>
        /// <returns>The task to await.</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage Request)
        {
            if (!string.IsNullOrEmpty(_bearerToken) && !(_expiration <= DateTimeOffset.Now.UtcDateTime.AddMinutes(5)))
            {
                Request.Headers.Authorization = new AuthenticationHeaderValue("bearer", _bearerToken);
            }
            else
            {
                _bearerToken = null;

                await RefreshAccessTokenAsync();

                if (!string.IsNullOrEmpty(_bearerToken))
                {
                    Request.Headers.Authorization = new AuthenticationHeaderValue("bearer", _bearerToken);
                }
                else
                {
                    throw new ServiceException(
                        new Error
                        {
                            Code = "authenticationRequired",
                            Message = "Please call AuthenticateAsync to prompt the user for authentication.",
                        });
                }
            }
        }

        #endregion

        #region Local Methods

        /// <summary>
        /// Refresh the current access token, if possible.
        /// </summary>
        /// <returns>The task to await.</returns>
        private Task RefreshAccessTokenAsync()
        {
            return SendTokenRequestAsync(GetBearerTokenRequestBody());
        }

        /// <summary>
        /// Implements the authorization flow to retreive a bearer token for future requests.
        /// </summary>
        /// <param name="requestBodyString">The authorization request payload to submit for the authorization request.</param>
        /// <returns>The task to await.</returns>
        private async Task SendTokenRequestAsync(string requestBodyString)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _tokenServiceUrl);
            httpRequestMessage.Content = new StringContent(requestBodyString, Encoding.UTF8, "application/x-www-form-urlencoded");

            using (var authResponse = await this._httpProvider.SendAsync(httpRequestMessage))
            using (var responseStream = await authResponse.Content.ReadAsStreamAsync())
            {
                var responseValues = _httpProvider.Serializer.DeserializeObject<IDictionary<string, string>>(responseStream);

                if (responseValues != null)
                {
                    _bearerToken = responseValues["access_token"];
                    _expiration = DateTimeOffset.UtcNow.Add(new TimeSpan(0, 0, int.Parse(responseValues["expires_in"])));
                }
                else
                {
                    throw new ServiceException(
                        new Error
                        {
                            Code = GraphErrorCode.AuthenticationFailure.ToString(),
                            Message = "Authentication failed. No response values returned from token authentication flow."
                        });
                }
            }
        }

        /// <summary>
        /// Builds out the request body for the authorization action.
        /// </summary>
        /// <returns>A <see cref="string"/> representing the payload of the authorization request.</returns>
        private string GetBearerTokenRequestBody()
        {
            var requestBuilder = new StringBuilder();
            requestBuilder.AppendFormat("{0}={1}", "grant_type", "client_credentials");
            requestBuilder.AppendFormat("&{0}={1}", "client_id", _clientId);
            requestBuilder.AppendFormat("&{0}={1}", "client_secret", _clientSecret);
            requestBuilder.AppendFormat("&{0}={1}", "resource", _resourceUri);

            return requestBuilder.ToString();
        }

        #endregion
    }
}
