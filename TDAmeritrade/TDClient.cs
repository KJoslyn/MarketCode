using Core;
using Core.Model;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using TDAmeritrade.Authentication;
using TDAmeritrade.Model;

namespace TDAmeritrade
{
    public class TDClient : IBrokerClient
    {
        public TDClient(TDAmeritradeConfig config)
        {
            AccountClient = new RestClient("https://api.tdameritrade.com/v1/accounts/" + config.AccountNumber);
            Authenticator = new Authenticator(config.ConsumerKey, config.AuthInfoPath);
        }

        private RestClient AccountClient { get; }
        private AuthInfo AuthInfo => AuthInfo.Read(Authenticator.AuthInfoPath);
        private string AccessToken => AuthInfo.access_token;
        private Authenticator Authenticator { get; }

        public static IRestResponse ExecuteRequest(RestClient client, RestRequest request)
        {
            IRestResponse response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                APIExceptions.ThrowAPIException(response);
            }

            return response;
        }

        public IList<Position> GetPositions()
        {
            RestRequest request = CreateRequest(Method.GET);
            request.AddParameter("fields", "positions");
            IRestResponse response = ExecuteRequest(AccountClient, request);
            Account account = JsonConvert.DeserializeObject<Account>(response.Content);
            return account.SecuritiesAccount.Positions.ToList();
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

