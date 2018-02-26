using Squattr.RESTAPI.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Squattr.RESTAPI.API.Async;
using System.Configuration;
using Squattr.RESTAPI.Services.Models.Slack;

namespace Squattr.RESTAPI.API.Controllers.API
{
    /// <summary>
    /// Main controller responsible for communication coming from Slack Slash Commands.
    /// </summary>
    [RoutePrefix("api/slack")]
    public class SlackController : ApiController
    {
        #region Local Variables

        private IAsyncRunner _runner;

        #endregion

        #region Constructors

        public SlackController(IAsyncRunner Runner)
        {
            _runner = Runner;
        }

        #endregion

        #region Controller Methods

        /// <summary>
        /// Method which gets called for a calendar schedule search request.
        /// </summary>
        /// <param name="token">The unique Slack Slash Token</param>
        /// <param name="team_id">The Slack team id in which the query originated.</param>
        /// <param name="team_domain">The Slack domain for the team in which the query originated</param>
        /// <param name="channel_id">The Slack channel id in which the query originated</param>
        /// <param name="channel_name">The Slack channel name in which the query originated</param>
        /// <param name="user_id">The Slack user id in which the query originated</param>
        /// <param name="user_name">The Slack user name in which the query originated</param>
        /// <param name="command">The Slack slash command that was executed</param>
        /// <param name="text">The arguments to the executed Slack command.</param>
        /// <param name="response_url">The unique postback URL (provided by Slack)</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<HttpResponseMessage> Search(string token, string team_id, string team_domain, string channel_id, string channel_name, string user_id, string user_name, string command, string text, string response_url)
        {
            Message response = new Services.Models.Slack.Message();

            if(token != ConfigurationManager.AppSettings["SlackSlashToken"].ToString())
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden);
            }
            else
            {
                await _runner.Run<SlackService>(service => service.Respond(text, response_url));
                response.text = string.Format("I'm on it! Give me a sec...", text);
                response.mrkdwn = true;
                return Request.CreateResponse<Message>(System.Net.HttpStatusCode.OK, response);
            }
        }

        #endregion
    }
}
     