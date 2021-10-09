namespace MyProjectGroup.Common.Security
{
    public class KnownAuthorizationPolicy
    {
        public const string Actuators = "actuators";
#if enableSecurity
        public const string AirportRead = "airport.read";
        public const string WeatherRead = "airport.read";
        public const string WeatherWrite = "airport.read";
#endif
    }
}