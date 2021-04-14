using System;
using System.Collections.Generic;
using System.Text;

namespace HAWeatherWorker
{
    public class Storm
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double LatitudeOrigem { get; set; }
        public double LongitudeOrigem { get; set; }


        public Storm() { }



        public double Distance
        {

            get
            {

                Func<double, double> Radians = x => x * Math.PI / 180;


                double latDistance = Radians(LatitudeOrigem - Latitude);
                double lngDistance = Radians(LongitudeOrigem - Longitude);

                double a = Math.Sin(latDistance / 2) * Math.Sin(latDistance / 2)
                  + Math.Cos(Radians(LatitudeOrigem)) * Math.Cos(Radians(Latitude))
                  * Math.Sin(lngDistance / 2) * Math.Sin(lngDistance / 2);

                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                return Math.Round(6371 * c);
            }


        }
    }
}
