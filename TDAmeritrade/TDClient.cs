using Core;
using Core.Model;
using Core.Model.Constants;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TDAmeritrade.Authentication;
using TDAmeritrade.Model;
#nullable enable

namespace TDAmeritrade
{
    public class TDClient : IBrokerClient, IMarketDataClient
    {
        public TDClient(TDAmeritradeConfig config)
        {
            AccountNumber = config.AccountNumber;
            Authenticator = new Authenticator(config.ConsumerKey, config.AuthInfoPath);
        }

        private AuthInfo AuthInfo => AuthInfo.Read(Authenticator.AuthInfoPath);
        private string AccessToken => AuthInfo.access_token;
        private Authenticator Authenticator { get; }
        private string AccountNumber { get; }

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
            RestClient client = new RestClient("https://api.tdameritrade.com/v1/accounts/" + AccountNumber);
            RestRequest request = CreateRequest(Method.GET);
            request.AddParameter("fields", "positions");
            IRestResponse response = ExecuteRequest(client, request);
            Account account = JsonConvert.DeserializeObject<Account>(response.Content);
            return account.SecuritiesAccount.Positions.Cast<Position>().ToList();
        }

        public OptionQuote GetQuote(string symbol)
        {
            Regex optionSymbolRegex = new Regex(@"^([A-Z]{1,5})_(\d{6})([CP])(\d+(.\d)?)");
            GroupCollection matchGroups = optionSymbolRegex.Match(symbol).Groups;
            string equitySymbol = matchGroups[1].Value;
            string date = DateTime.ParseExact(matchGroups[2].Value, "yyMMdd", CultureInfo.InvariantCulture)
                .ToString("yyyy-MM-dd");
            string contractType = matchGroups[3].Value == "C"
                ? PutCall.CALL
                : PutCall.PUT;
            string strike = matchGroups[4].Value;

            RestClient client = new RestClient("https://api.tdameritrade.com/v1/marketdata/chains");
            RestRequest request = CreateRequest(Method.GET);
            request.AddQueryParameter("symbol", equitySymbol);
            request.AddQueryParameter("contractType", contractType);
            request.AddQueryParameter("includeQuotes", "TRUE");
            request.AddQueryParameter("strike", strike);
            request.AddQueryParameter("fromDate", date);
            request.AddQueryParameter("toDate", date);
            IRestResponse response = ExecuteRequest(client, request);

            Regex responseRegex = new Regex("({\"putCall\".*})]");
            GroupCollection responseMatchGroups = responseRegex.Match(response.Content).Groups;
            string optionQuoteStr = responseMatchGroups[1].Value;
            OptionQuote quote = JsonConvert.DeserializeObject<OptionQuote>(optionQuoteStr);
            return quote;
        }

        public bool IsMarketOpenToday()
        {
            RestClient client = new RestClient("https://api.tdameritrade.com/v1/marketdata/OPTION/hours");
            RestRequest request = CreateRequest(Method.GET);
            IRestResponse response = ExecuteRequest(client, request);
            return response.Content.Contains("\"isOpen\":true");
        }

        public void PlaceOrder(Order order)
        {
            throw new System.NotImplementedException();
            RestClient client = new RestClient("https://api.tdameritrade.com/v1/accounts/" + AccountNumber + "/orders");
            RestRequest request = CreateRequest(Method.POST);
            string body = CreateOrderBody(order);
            request.AddJsonBody(body);
            IRestResponse response = ExecuteRequest(client, request);
            // TODO: make sure ok
        }

        private RestRequest CreateRequest(Method method)
        {
            Authenticator.Authenticate();
            RestRequest request = new RestRequest(method);
            request.AddHeader("Authorization", "Bearer " + AccessToken);
            return request;
        }

        private string CreateOrderBody(Order order)
        {
            Instrument instrument = new Instrument(order.Symbol, AssetType.OPTION);
            OrderLeg orderLeg = new OrderLeg(order.Instruction, (int)order.Quantity, instrument);
            List<OrderLeg> orderLegCollection = new List<OrderLeg>();
            orderLegCollection.Add(orderLeg);
            string? priceStr = null;
            if (order.OrderType == OrderType.LIMIT)
            {
                double doublePrice = Math.Round(order.Limit, 2);
                priceStr = doublePrice.ToString();
            }

            OrderBody orderBody = new OrderBody(
                "NONE",
                order.OrderType,
                "NORMAL",
                priceStr,
                "DAY",
                "SINGLE",
                orderLegCollection);

            string orderBodyStr = JsonConvert.SerializeObject(orderBody, new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            Log.Information("TDAm Order: {@Order}, string: {OrderStr}, Symbol {Symbol}", orderBody, orderBodyStr, order.Symbol);
            return orderBodyStr;
        }
    }
}

