using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
