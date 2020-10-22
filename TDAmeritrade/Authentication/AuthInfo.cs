using Newtonsoft.Json;
using System;
using System.IO;

namespace TDAmeritrade.Authentication
{
    internal class AuthInfo
    {
        public string access_token;
        public string refresh_token;
        public DateTime access_token_expires_at_date;
        public DateTime refresh_token_expires_at_date;

        public AuthInfo() { } // Used by JsonConvert when reading from the json file

        public AuthInfo(AuthResponse authResponse, AuthInfo oldAuthInfo)
        {
            access_token = authResponse.access_token;
            access_token_expires_at_date = DateTime.Now.AddSeconds(authResponse.expires_in);

            if (authResponse.refresh_token == null)
            {
                refresh_token = oldAuthInfo.refresh_token;
                refresh_token_expires_at_date = oldAuthInfo.refresh_token_expires_at_date;
            }
            else
            {
                refresh_token = authResponse.refresh_token;
                refresh_token_expires_at_date = DateTime.Now.AddSeconds(authResponse.refresh_token_expires_in);
            }
        }

        public static AuthInfo Read()
        {
            using (StreamReader r = new StreamReader(Config.AuthPath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<AuthInfo>(json);
            }
        }

        public static void Write(AuthInfo authInfo)
        {
            string authJson = JsonConvert.SerializeObject(authInfo);
            System.IO.File.WriteAllText(Config.AuthPath, authJson);
        }
    }
}
