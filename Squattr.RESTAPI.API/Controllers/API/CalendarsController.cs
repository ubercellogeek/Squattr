using Squattr.RESTAPI.Services;
using Squattr.RESTAPI.Services.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Collections.Generic;
using Squattr.RESTAPI.API.Filters;

namespace Squattr.RESTAPI.API.Controllers.API
{
    /// <summary>
    /// This controller is responsible for CRUD operations which ultimately interface with
    /// The Microsft Graph API.
    /// </summary>
    [RoutePrefix("api/calendars")]
    [APIKeyAuthorize]
    public class CalendarsController : ApiController
    {
        #region Local Variables

        private CalendarService _service;

        #endregion

        #region Constructors

        public CalendarsController(CalendarService Service)
        {
            _service = Service;
        }

        #endregion

        #region Controller Methods

        /// <summary>
        /// Gets a list of available conference rooms to query from.
        /// </summary>
        /// <returns>An <see cref="List{String}"/> of rooms to base caledar queries from.</returns>
        public List<string> Get()
        {
            return _service.GetRooms();
        }

        /// <summary>
        /// Gets today's list of events for a given conference room.
        /// </summary>
        /// <param name="RoomName">The name of the conference room to retrieve events for.</param>
        /// <returns><see cref="IQueryable{CalendarEvent}"/>. A queryable list of calendar events.</returns>
        [EnableQuery]
        [Route("{RoomName}")]
        [HttpGet]
        public async Task<IQueryable<CalendarEvent>> Get(string RoomName)
        {
            if (!_service.GetRooms().Contains(RoomName))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            return await _service.GetEvents(RoomName);
        }

        /// <summary>
        /// Gets today's list of events for a given conference room.
        /// </summary>
        /// <param name="RoomName">The name of the conference room to retrieve events for.</param>
        /// <returns><see cref="IQueryable{CalendarEvent}"/>. A queryable list of calendar events.</returns>
        [EnableQuery]
        [Route("all/{RoomName}")]
        [HttpGet]
        public async Task<IQueryable<CalendarEvent>> GetAll(string RoomName)
        {
            return await _service.GetEvents(RoomName);
        }

        /// <summary>
        /// Gets list of events for a given conference room based upon a supplied date range.
        /// </summary>
        /// <param name="RoomName">The name of the conference room to retrieve events for.</param>
        /// <param name="Start">The begining date/time range to filter the results to. Format: yyyyMMddTHHmm.</param>
        /// <param name="End">The end date/time range to filter the results to. Format: yyyyMMddTHHmm.</param>
        /// <returns><see cref="IQueryable{CalendarEvent}"/>. A queryable list of calendar events.</returns>
        [EnableQuery]
        [Route("{RoomName}/{Start}/{End}")]
        [HttpGet]
        public async Task<IQueryable<CalendarEvent>> Get(string RoomName, string Start, string End)
        {
            DateTime start;
            DateTime end;

            if (!_service.GetRooms().Contains(RoomName))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            if (!DateTime.TryParseExact(Start, "yyyyMMddTHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out start))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            if (!DateTime.TryParseExact(End, "yyyyMMddTHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out end))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            if (start > end)
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            return await _service.GetEvents(RoomName, start, end);
        }

        /// <summary>
        /// Gets list of events for a given conference room based upon a supplied date range.
        /// </summary>
        /// <param name="RoomName">The name of the conference room to retrieve events for.</param>
        /// <param name="Start">The begining date/time range to filter the results to. Format: yyyyMMddTHHmm.</param>
        /// <param name="End">The end date/time range to filter the results to. Format: yyyyMMddTHHmm.</param>
        /// <returns><see cref="IQueryable{CalendarEvent}"/>. A queryable list of calendar events.</returns>
        [EnableQuery]
        [Route("all/{RoomName}/{Start}/{End}")]
        [HttpGet]
        public async Task<IQueryable<CalendarEvent>> GetAll(string RoomName, string Start, string End)
        {
            DateTime start;
            DateTime end;

            if (!DateTime.TryParseExact(Start, "yyyyMMddTHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out start))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            if (!DateTime.TryParseExact(End, "yyyyMMddTHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out end))
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            if (start > end)
            {
                throw new HttpResponseException(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest));
            }

            return await _service.GetEvents(RoomName, start, end);
        }

        /// <summary>
        /// Gets a list of each conference room's current availablity for today.
        /// </summary>
        /// <returns></returns>
        [Route("status")]
        [HttpGet]
        public async Task<List<ConferenceRoomStatus>> GetStatuses()
        {
            return await _service.GetRoomStatuses();
        }

        /// <summary>
        /// Reserves a conference room based on the supplied model.
        /// </summary>
        /// <param name="Reservation">The details about the reservation to be made.</param>
        /// <returns><see cref="HttpResponseMessage"/>. Will return 201 on success, 409 upon calednar schedule conflicts, and 500 for any errors.</returns>
        [Route("reserve")]
        [HttpPost]
        public async Task<HttpResponseMessage> Reserve(CalendarReservation Reservation)
        {
            bool result;

            try
            {
                result = await _service.ReserveConferenceRoom(Reservation);

                if (result == true)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Created);
                }
                else
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Conflict);
                }
            }
            catch (Exception)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }

        #endregion
    }
}
