using Squattr.RESTAPI.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Squattr.RESTAPI.Services.Models;
using System.Collections.Concurrent;

namespace Squattr.RESTAPI.Services
{
    /// <summary>
    /// The service which provides hooks into providing messages to the Slack platform.
    /// </summary>
    public class SlackService
    {
        #region Local Variables

        private GraphClient _graphClient;
       
        #endregion

        #region Constructors

        public SlackService(GraphOAuth2Provider AuthProvider)
        {
            _graphClient = new GraphClient(AuthProvider);
           
        }

        /// <summary>
        /// Sends a deferred message response to a Slack channel, user, or IM session. This sends the calendar schedule of a given
        /// conference room back to Slack for a given query invocation.
        /// </summary>
        /// <param name="RoomName">The command parameter that was issued from within Slack.</param>
        /// <param name="ReplyURI">The unique time-limited URL with which to post the calendar response to Slack.</param>
        public void Respond(string Arguments, string ReplyURI)
        {
            string room = string.Empty;
            string day = string.Empty;
            string[] args = Arguments.Split(' ');

            if (args.Length == 1)
            {
                room = Arguments.ToLower();
                if(room == "status")
                {
                    GetOpenRooms(ReplyURI);
                }
                else
                {
                    GetRoom(room,ReplyURI);
                }
            }
            else if (args.Length == 2)
            {
                room = args[0].ToLower();
                day = args[1];
                GetRoom(room, ReplyURI, day);
            }
            else
            {
                var reply = new Models.Slack.Message();
                reply.text = "I didn't understand that, could you try again?";
                SendReply(reply, ReplyURI);
            }
        }

        /// <summary>
        /// Gets the availability of a given room on any given day of the week.
        /// </summary>
        /// <param name="RoomName">The name of the room to query.</param>
        /// <param name="ReplyURI">The unique time-limited URL with which to post the calendar response to Slack.</param>
        /// <param name="DayOfWeek">The name of the day of the week to query for. NOTE: This is the next instance of this day if not today.</param>
        private void GetRoom(string RoomName, string ReplyURI, string DayOfWeek = "")
        {
            List<string> rooms = ConfigurationManager.AppSettings["Rooms"].Split(',').ToList();
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            string day = string.Empty;

            Models.Slack.Message message = new Models.Slack.Message();
            message.mrkdwn = true;

            if(DayOfWeek == string.Empty)
            {
                day = DateTime.Today.DayOfWeek.ToString();
            }
            else
            {
                day = DayOfWeek;
            }
           
            string[] days = { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
            string notFound = string.Empty;

            if (!days.Contains(day.ToLower()) || !rooms.Contains(RoomName.ToLower()))
            {
                message.text = string.Format("Could not find a schedule for *{0}* on *{1}*", RoomName, day);
                SendReply(message, ReplyURI);
                return;
            }

            if (day.ToLower() == DateTime.Now.DayOfWeek.ToString().ToLower())
            {
                message.text = string.Format("Here's *{0}'s* schedule for the rest of today:", RoomName);
                notFound = string.Format("There's nothing on *{0}'s* schedule today.", RoomName);
            }
            else
            {
                while (start.DayOfWeek.ToString().ToLower() != day.ToLower())
                {
                    start = start.AddDays(1);
                }
                start = DateTime.Parse(start.ToShortDateString());
                message.text = string.Format("Here's *{0}'s* schedule for this coming *{1} ({2})*:", RoomName, start.DayOfWeek.ToString(), start.ToString("M/d"));
                notFound = string.Format("There's nothing on *{0}'s* schedule for this coming *{1} ({2})*:", RoomName, start.DayOfWeek.ToString(), start.ToString("M/d"));
            }

            end = DateTime.Parse(start.AddDays(1).ToShortDateString());

            List<CalendarEvent> events = _graphClient.GetConferenceRoomCalendarEvents(RoomName, start.ToUniversalTime(), end.ToUniversalTime(), 10).Result;
            if (events != null && events.Count > 0)
            {
                message.attachments = new List<Models.Slack.Attachment>();

                foreach (var evnt in events.OrderBy(x => x.Start))
                {
                    var attachment = new Models.Slack.Attachment();
                    attachment.fields = new List<Models.Slack.Field>();
                    attachment.color = "#007fe0";

                    var timeField = new Models.Slack.Field();
                    timeField.@short = true;
                    timeField.title = "Reserved";
                    timeField.value = evnt.Start.ToString("h:mm tt") + " - " + evnt.End.ToString("h:mm tt");

                    attachment.fields.Add(timeField);

                    var reservedBy = new Models.Slack.Field();
                    reservedBy.@short = true;
                    reservedBy.title = "Organizer";
                    reservedBy.value = evnt.OrganizerName;

                    attachment.fields.Add(reservedBy);
                    message.attachments.Add(attachment);
                }
            }
            else
            {
                message.text = notFound;
            }

            SendReply(message, ReplyURI);
        }

        /// <summary>
        /// Retrieves a list of the current open/used status of all the conference rooms.
        /// </summary>
        /// <param name="ReplyURI">The unique time-limited URL with which to post the calendar response to Slack.</param>
        private void GetOpenRooms(string ReplyURI)
        {
            List<string> rooms = ConfigurationManager.AppSettings["Rooms"].Split(',').ToList();
            ConcurrentDictionary<string, List<CalendarEvent>> roomEvents = new ConcurrentDictionary<string, List<CalendarEvent>>();
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Parse(start.AddDays(1).ToShortDateString());

            Models.Slack.Message message = new Models.Slack.Message();
            message.attachments = new List<Models.Slack.Attachment>();
            message.mrkdwn = true;

            Parallel.ForEach(rooms, (currentRoom) => {
                List<CalendarEvent> events = _graphClient.GetConferenceRoomCalendarEvents(currentRoom, start.ToUniversalTime(), end.ToUniversalTime(), 20).Result;
                roomEvents.TryAdd(currentRoom, events);
            });

            foreach (var room in roomEvents.OrderBy(x=>x.Key))
            {
                var attachment = new Models.Slack.Attachment();
                attachment.fields = new List<Models.Slack.Field>();
                attachment.title = string.Format("{0}", room.Key.ToUpper());

                // Is the room currently busy? If not, find out when it's next event is.
                var current = roomEvents[room.Key].Where(x => start >= x.Start && start <= x.End).FirstOrDefault();
                if(current == null)
                {
                    current = roomEvents[room.Key].OrderBy(x => x.Start).FirstOrDefault();
                    if (current == null)
                    {
                        attachment.color = "good";
                        attachment.text = "Open until EOD";
                    }
                    else
                    {
                        TimeSpan next = current.Start - start;
                        attachment.color = "good";
                        attachment.text = string.Format("Open until {0} ({1})", current.Start.ToString("h:mm tt"), current.OrganizerName);
                    }
                }
                else
                {
                    bool backToBack = true;
                    var next = new CalendarEvent();

                    while (backToBack)
                    {
                        next = roomEvents[room.Key].Where(x => x.Start >= current.End).OrderBy(x => x.Start).FirstOrDefault();
                        if(next == null)
                        {
                            backToBack = false;
                        }
                        else
                        {
                            TimeSpan ts = (next.Start - current.End);
                            if(ts.TotalMinutes > 0)
                            {
                                backToBack = false;
                            }
                            else
                            {
                                current = next;
                            }
                        }
                    }

                    attachment.color = "danger";
                    attachment.text = string.Format("Busy until {0} ({1})", current.End.ToString("h:mm tt"), current.OrganizerName);
                }

                attachment.mrkdwn_in = new string[] { "text" };
                message.attachments.Add(attachment);
            }

            SendReply(message, ReplyURI);
        }

        /// <summary>
        /// Sends the payload of a request back to the originating Slack channel.
        /// </summary>
        /// <param name="ReplyMessage">The <see cref="Models.Slack.Message"/> to send to the slack channel.</param>
        /// <param name="ReplyURI">The unique time-limited URL with which to post the calendar response to Slack.</param>
        private void SendReply(Models.Slack.Message ReplyMessage, string ReplyURI)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response;
                response = client.PostAsync(ReplyURI, new StringContent(
                        JsonConvert.SerializeObject(ReplyMessage), Encoding.UTF8, "application/json")).Result;
                return;
            }
        }

        /// <summary>
        /// Pretty prints out the hours/minutes of a timespan.
        /// </summary>
        /// <param name="Span">The <see cref="TimeSpan"/> to parse.</param>
        /// <returns></returns>
        private string PrettyPrintRemaining(TimeSpan Span)
        {
            if(Span.Hours != 0)
            {
                return string.Format("{0} hour(s), {1} minute(s)", Span.Hours.ToString(), Span.Minutes.ToString());
            }
            else
            {
                return string.Format("{0} minute(s)", Span.Minutes.ToString());
            }
        }

        #endregion
    }
}
