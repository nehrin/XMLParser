using System;

namespace XMLParser
{
    public class Reservation
    {
        public int id { get; set; }
        public string vendor { get; set; }
        public string description { get; set; }
        public DateTime reservationTime { get; set; }
    }
}
