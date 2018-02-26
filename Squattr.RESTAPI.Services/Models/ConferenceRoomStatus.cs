using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Squattr.RESTAPI.Services.Models
{
    /// <summary>
    /// Represents the current status of a conference room's availability.
    /// </summary>
    public class ConferenceRoomStatus
    {
        public string RoomName { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsInUse { get; set; }
        public string OrganizerName { get; set; }
    }
}
