using System;
using System.Collections.Generic;
using System.Text;

namespace HAWeatherWorker
{
    public class ServiceConfigurations
    {
        public string MQTTServer { get; set; }
        public string StormURL { get; set; }
        public double LatitudeCasa { get; set; }
        public double LongitudeCasa { get; set; }
        public int Intervalo { get; set; }
    }
}
