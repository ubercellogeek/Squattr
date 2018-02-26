using Squattr.RESTAPI.Services.Authentication;
using Squattr.RESTAPI.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Concurrent;

namespace Squattr.RESTAPI.Services
{
    /// <summary>
    /// Service used to execute CRUD actions via the Graph API for calendar events.
    /// </summary>
    public class CalendarService
    {
        #region Local Variables

        private GraphClient _graphClient;

        #endregion

        #region Constructors

        public CalendarService(GraphOAuth2Provider AuthProvider)
        {
            _graphClient = new GraphClient(AuthProvider);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves a list availbale conference rooms to query against.
        /// </summary>
        /// <returns></returns>
        public List<string> GetRooms()
        {
            return ConfigurationManager.AppSettings["Rooms"].Split(',').ToList();
        }

        /// <summary>
        /// Retrieves a list of calendar events for a given conference room.
        /// </summary>
        /// <param name="RoomName">The name of the conference room.</param>
        /// <param name="Start">The begining of the DateTime filter.</param>
        /// <param name="End">The end of the DateTime filter.</param>
        /// <returns>A <see cref="Task(IQueryable{CalendarEvent}"/> for the the specified conference room/timestpan arguments.</returns>
        public async Task<IQueryable<CalendarEvent>> GetEvents(string RoomName, DateTime? Start = null, DateTime? End = null)
        {
            // Convert to UTC for the request. We assume all incoming requests are in EST for now.
            DateTime start = DateTime.Parse(DateTime.Now.ToShortDateString()).ToUniversalTime();
            DateTime end = DateTime.Parse(DateTime.Now.AddDays(1).ToShortDateString()).ToUniversalTime();

            if (Start != null && End != null)
            {
                start = Start.Value.ToUniversalTime();
                end = End.Value.ToUniversalTime();
            }

            List<CalendarEvent> events = await _graphClient.GetConferenceRoomCalendarEvents(RoomName, start, end, 30);
            return events.OrderBy(x => x.Start).AsQueryable();
        }

        /// <summary>
        /// Reserves a conference room based on the supplied reservation details.
        /// </summary>
        /// <param name="Reservation">The reservation detaills with which to reserve the room.</param>
        /// <returns><see cref="bool"/> indicating success or conflict with other meetings already on the conference room's calendar.</returns>
        public async Task<bool> ReserveConferenceRoom(CalendarReservation Reservation)
        {
            // First, determine if the room is free during the specified time period.
            DateTime start = Reservation.StartDate;
            DateTime end = Reservation.EndDate;
            List<CalendarEvent> events = await _graphClient.GetConferenceRoomCalendarEvents(Reservation.RoomName, start, end, 30);
            CalendarEvent existing = null;

            existing = events.Where(x => (Reservation.StartDate >= x.Start && Reservation.StartDate < x.End) || (Reservation.EndDate > x.Start && Reservation.EndDate <= x.End)).FirstOrDefault();

            if(existing != null)
            {
                return false;
            }
            else
            {
                return await _graphClient.ReserveConferenceRoom(Reservation.RoomName, Reservation.Username, Reservation.StartDate, Reservation.EndDate);
            }
        }

        /// <summary>
        /// Retrieves the current (today's) availability status of each conference room.
        /// </summary>
        public async Task<List<ConferenceRoomStatus>> GetRoomStatuses()
        {
            List<string> rooms = ConfigurationManager.AppSettings["Rooms"].Split(',').ToList();
            List<ConferenceRoomStatus> roomStatuses = new List<ConferenceRoomStatus>();
            ConcurrentDictionary<string, List<CalendarEvent>> roomEvents = new ConcurrentDictionary<string, List<CalendarEvent>>();
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Parse(start.AddDays(1).ToShortDateString());

            // Wrap the parallel operation in a task to avoid a deadlock situation.
            await Task.Run(() => {
                Parallel.ForEach(rooms, (currentRoom) =>
                {
                    List<CalendarEvent> events = _graphClient.GetConferenceRoomCalendarEvents(currentRoom, start.ToUniversalTime(), end.ToUniversalTime(), 20).Result;
                    roomEvents.TryAdd(currentRoom, events);
                });
            });

            foreach (var room in roomEvents.OrderBy(x => x.Key))
            {
                // Is the room currently busy? If not, find out when it's next event is.
                var current = roomEvents[room.Key].Where(x => start >= x.Start && start <= x.End).FirstOrDefault();
                if (current == null)
                {
                    current = roomEvents[room.Key].OrderBy(x => x.Start).FirstOrDefault();
                    if (current == null)
                    {
                        roomStatuses.Add(new ConferenceRoomStatus() { RoomName = room.Key, EndTime = null, IsInUse = false });
                    }
                    else
                    {
                        roomStatuses.Add(new ConferenceRoomStatus() { RoomName = room.Key, EndTime = current.Start, OrganizerName = current.OrganizerName, IsInUse = false });
                    }
                }
                else
                {
                    bool backToBack = true;
                    var next = new CalendarEvent();

                    while (backToBack)
                    {
                        next = roomEvents[room.Key].Where(x => x.Start >= current.End).OrderBy(x => x.Start).FirstOrDefault();
                        if (next == null)
                        {
                            backToBack = false;
                        }
                        else
                        {
                            TimeSpan ts = (next.Start - current.End);
                            if (ts.TotalMinutes > 0)
                            {
                                backToBack = false;
                            }
                            else
                            {
                                current = next;
                            }
                        }
                    }

                    roomStatuses.Add(new ConferenceRoomStatus() { RoomName = room.Key, EndTime = current.End, IsInUse = true });
                }
            }

            return roomStatuses;
        }

        #endregion
    }
}
