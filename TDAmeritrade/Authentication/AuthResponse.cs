#nullable enable

namespace TDAmeritrade.Authentication
{
    internal class AuthResponse
    {
        // Suppress "field is never assigned to" and
        // "non-nullable field not assigned value in constructor" warnings.
        // These values will come from JSON deserialization.
#pragma warning disable 0649, 8618

        public string access_token;
        public string? refresh_token;
        public int expires_in;
        public int refresh_token_expires_in;
    }
}
