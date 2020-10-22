#nullable enable

namespace TDAmeritrade.Authentication
{
    internal class AuthResponse
    {
        public string access_token;
        public string? refresh_token;
        public int expires_in;
        public int refresh_token_expires_in;
    }
}
