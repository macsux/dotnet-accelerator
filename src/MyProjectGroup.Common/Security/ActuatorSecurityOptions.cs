using System;

namespace DotnetAccelerator.Security
{
    public class ActuatorSecurityOptions
    {
        private static readonly Lazy<string> _defaultUsername = new(GenerateSecret);
        private static readonly Lazy<string> _defaultPassword = new(GenerateSecret);
        private static string GenerateSecret() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Trim('=');
        public string UserName { get; set; } = _defaultUsername.Value;
        public string Password { get; set; } = _defaultPassword.Value;
    }
}