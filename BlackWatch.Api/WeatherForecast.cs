using System;

namespace BlackWatch.Api
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int) (TemperatureC * 1.799856012);

        public string Summary { get; set; }
    }
}
