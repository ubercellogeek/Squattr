using Microsoft.Graph;
using Newtonsoft.Json;
using Squattr.RESTAPI.Services.Authentication;
using Squattr.RESTAPI.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squattr.RESTAPI.Services
{
    /// <summary>
    /// Wrapper class for calendar and other queries to the Microsoft Graph API.
    /// </summary>
    public class GraphClient
    {
        #region Local Variables

        private GraphOAuth2Provider _authProvider;
        private GraphServiceClient _serviceClient;

        #endregion

        #region Constructors

        public GraphClient(GraphOAuth2Provider AuthProvider)
        {
            _authProvider = AuthProvider;
            _serviceClient = new GraphServiceClient(_authProvider);
        }

        #endregion

        #region "Methods"

        /// <summary>
        /// Executes a query for calendar events against the Microsoft Graph API for a given conference room or person.
        /// </summary>
        /// <param name="RoomName">Conference room name or User name.</param>
        /// <param name="Start">The start datetime of the calendar events to filter by.</param>
        /// <param name="End">The end datetime of the calendar events to filter by.</param>
        /// <param name="Limit">The maximum number of events to return.</param>
        public async Task<List<CalendarEvent>> GetConferenceRoomCalendarEvents(string RoomName, DateTime Start, DateTime End, int Limit = 10)
        {
            // Build out the email for the request.
            string email = string.Format("{0}@springthrough.com", RoomName);

            // Generate the calendar request
            var request = new CalendarCalendarViewCollectionRequestBuilder(string.Format("{0}/users/{1}/calendarView/?startDateTime={2}&endDateTime={3}", _serviceClient.BaseUrl, email, Start.ToString("o"), End.ToString("o")), _serviceClient);

            // Execute the query
            var response = await request.Request().Top(Limit).GetAsync();

            // Dirty mapping. Probably should use AutoMapper or a similar method.
            return response.ToList().Select(x =>
                new CalendarEvent()
                {
                    OrganizerEmail = x.Organizer.EmailAddress.Address,
                    OrganizerName = x.Organizer.EmailAddress.Name,
                    Start = DateTime.Parse(x.Start.DateTime).ToLocalTime(),
                    End = DateTime.Parse(x.End.DateTime).ToLocalTime(),
                    Subject = x.Subject,
                    Recurring = (x.Recurrence != null),
                    ShowAs = x.ShowAs == null ? null : x.ShowAs.Value.ToString()
                }).ToList<CalendarEvent>();
        }

        /// <summary>
        /// Reserves a conference room.
        /// </summary>
        /// <param name="RoomName">The name of the conference room to reserve.</param>
        /// <param name="Username">The username of the meeting requestor.</param>
        /// <param name="Start">The <see cref="DateTime"/> of the start of the meeting. NOTE: This should be in Eastern Standard Time.</param>
        /// <param name="End">The <see cref="DateTime"/> of the end of the meeting. NOTE: This should be in Eastern Standard Time.</param>
        /// <returns><see cref="bool"/> indicating success or conflict with other meetings already on the conference room's calendar.</returns>
        public async Task<bool> ReserveConferenceRoom(string RoomName, string Username, DateTime Start, DateTime End)
        {
            string userEmail = string.Format("{0}@springthrough.com", Username);
            string roomEmail = string.Format("{0}@springthrough.com", RoomName);
            var request = new UserEventsCollectionRequestBuilder($"{_serviceClient.BaseUrl}/users/{userEmail}/calendar/events", _serviceClient);

            List<Attendee> attendees = new List<Attendee>();
            Attendee attendee = new Attendee();
            attendee.EmailAddress = new EmailAddress() { Address = roomEmail };
            attendees.Add(attendee);

            Event evnt = new Event();
            evnt.Subject = "Squattr Meeting Reservation";
            evnt.OriginalStartTimeZone = "Eastern Standard Time";
            evnt.OriginalEndTimeZone = "Eastern Standard Time";
            evnt.Start = new DateTimeTimeZone() { DateTime = Start.ToString("o"), TimeZone = "Eastern Standard Time" };
            evnt.End = new DateTimeTimeZone() { DateTime = End.ToString("o"), TimeZone = "Eastern Standard Time" };
            evnt.Attendees = attendees;
            evnt.Location = new Location() { DisplayName = RoomName };

            // Execute the query
            var response = await request.Request().AddAsync(evnt);

            return true;
        }

        #endregion
    }
}
