using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core;
using Core.Model;
using TDAmeritrade.Model;
using Newtonsoft.Json;
using RestSharp;
using TDAmeritrade.Authentication;
using Serilog;

namespace TDAmeritrade
{
    public class TDClient : IBrokerClient
    {
        public TDClient(string accountId)
        {
            AccountClient = new RestClient("https://api.tdameritrade.com/v1/accounts/" + accountId);
        }

        private RestClient AccountClient { get; }
        private AuthInfo AuthInfo => AuthInfo.Read();
        private string AccessToken => AuthInfo.access_token;

        public static IRestResponse ExecuteRequest(RestClient client, RestRequest request)
        {
            IRestResponse response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                APIExceptions.ThrowAPIException(response);
            }

            return response;
        }

        public Position[] GetPositions()
        {
            RestRequest request = CreateRequest(Method.GET);
            IRestResponse response = ExecuteRequest(AccountClient, request);

            //Account account = JsonConvert.DeserializeObject<Account>(response.Content);

            Log.Information("Called GetPositions");

            //using (StreamReader r = new StreamReader("C:/Users/Admin/WindowsServices/MarketCode/TDAmeritrade/test_response.json"))
            //{
            //    string json = r.ReadToEnd();
            //    Account account = JsonConvert.DeserializeObject<Account>(json);
            //    Console.WriteLine(account);
            //}

            Log.Error("GetPositions() is not implemented");
            throw new NotImplementedException();
        }

        private RestRequest CreateRequest(Method method)
        {
            Authenticator.Authenticate();
            RestRequest request = new RestRequest(method);
            request.AddHeader("Authorization", "Bearer " + AccessToken);
            return request;
        }
    }
}

