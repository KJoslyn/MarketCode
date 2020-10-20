using Newtonsoft.Json;
using RestSharp;
using Serilog;
using System;

namespace TDAmeritrade.Authentication
{
    internal static class Authenticator
    {
        static readonly int nMinutesBeforeAccessTokenUpdate = 1;
        static readonly int nDaysBeforeRefreshTokenUpdate = 5;

        public static void Authenticate()
        {
            AuthInfo _authInfo = AuthInfo.Read();
            if (AccessTokenNeedsUpdate(_authInfo) || RefreshTokenNeedsUpdate(_authInfo))
            {
                AuthResponse response = RequestAuthTokens(_authInfo);
                AuthInfo newAuthInfo = new AuthInfo(response, _authInfo);
                AuthInfo.Write(newAuthInfo);
            }
        }

        private static Boolean AccessTokenNeedsUpdate(AuthInfo authInfo)
        {
            return DateTime.Compare(authInfo.access_token_expires_at_date,
                DateTime.Now.AddMinutes(nMinutesBeforeAccessTokenUpdate)) < 0;
        }

        private static Boolean RefreshTokenNeedsUpdate(AuthInfo authInfo)
        {
            return DateTime.Compare(authInfo.refresh_token_expires_at_date,
                DateTime.Now.AddDays(nDaysBeforeRefreshTokenUpdate)) < 0;
        }

        private static AuthResponse RequestAuthTokens(AuthInfo currentAuthInfo)
        {
            RestClient authClient = new RestClient("https://api.tdameritrade.com/v1/oauth2/token");
            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", currentAuthInfo.refresh_token);
            request.AddParameter("client_id", Config.ConsumerKey);

            if (RefreshTokenNeedsUpdate(currentAuthInfo))
            {
                Log.Information("Requesting new REFRESH token");
                request.AddParameter("access_type", "offline");
            }
            else
            {
                Log.Information("Requesting new access token only");
            }
            IRestResponse response = TDClient.ExecuteRequest(authClient, request);

            return JsonConvert.DeserializeObject<AuthResponse>(response.Content);
        }
    }
}
