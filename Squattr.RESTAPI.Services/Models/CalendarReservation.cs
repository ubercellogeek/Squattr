using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squattr.RESTAPI.Services.Models
{
    public class CalendarReservation
    {
        public string Username { get; set; }
        public string RoomName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
