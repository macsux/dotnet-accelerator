using System;

namespace DotnetAccelerator.Modules.WeatherModule.Domain.Models
{
    public partial class WeatherForecastQuery
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? AirportId { get; set; }
    }
}