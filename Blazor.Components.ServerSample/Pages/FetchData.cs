using Blazor.Components.ServerSample.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Blazor.Components.ServerSample.Pages
{
    public partial class FetchData
    {
        private readonly WeatherForecastService weatherForecastService;

        public FetchData(WeatherForecastService weatherForecastService)
        {
            this.weatherForecastService = weatherForecastService;
        }
    }
}
