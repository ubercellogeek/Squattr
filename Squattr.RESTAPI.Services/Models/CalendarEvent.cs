using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squattr.RESTAPI.Services.Models
{
    /// <summary>
    /// Represents a calendar meeting or invite for a given conference room.
    /// </summary>
    public class CalendarEvent
    {
        public string OrganizerName { get; set; }
        public string OrganizerEmail { get; set; }
        public string Subject { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool Recurring { get; set; }
        public string ShowAs { get; set; }
    }
}
