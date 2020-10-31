using Core;
using Core.Model;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using TDAmeritrade.Authentication;
using TDAmeritrade.Model;
#nullable enable

namespace TDAmeritrade
{
    public class TDClient : IBrokerClient, IMarketDataClient
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

        public Position? GetPosition(string symbol)
        {
            IList<Position> positions = GetPositions();
            return positions.Where(pos => pos.Symbol == symbol).FirstOrDefault();
        }

        public IList<Position> GetPositions()
        {
            RestRequest request = CreateRequest(Method.GET);
            request.AddParameter("fields", "positions");
            IRestResponse response = ExecuteRequest(AccountClient, request);
            Account account = JsonConvert.DeserializeObject<Account>(response.Content);
            return account.SecuritiesAccount.Positions.Cast<Position>().ToList();
        }

        public OptionQuote GetQuote(string symbol)
        {
            RestClient client = new RestClient("https://api.tdameritrade.com/v1/marketdata/" + symbol + "/quotes");
            RestRequest request = CreateRequest(Method.GET);
            IRestResponse response = ExecuteRequest(client, request);
            OptionQuote quote = JsonConvert.DeserializeObject<OptionQuote>(response.Content);
            return quote;
        }

        public void PlaceOrder(Order order)
        {
            throw new System.NotImplementedException();
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

