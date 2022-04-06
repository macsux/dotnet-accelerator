namespace MyProjectGroup.Common.Security
{
    public class KnownAuthorizationPolicy
    {
#if enableSecurity
        public const string AirportRead = "airport.read";
        public const string WeatherRead = "airport.read";
        public const string WeatherWrite = "airport.read";
#endif
    }
}