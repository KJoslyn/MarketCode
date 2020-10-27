using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;

namespace AzureOCR
{
    public class OCRClient
    {
        private ComputerVisionClient _client;

        public OCRClient(OCRConfig config)
        {
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(config.SubscriptionKey))
              { Endpoint = config.Endpoint };
        }

        public async Task<IList<Line>> ExtractLinesFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<ReadResult> textResults;

            if (filePath.EndsWith("json")) {
                using (StreamReader r = new StreamReader(filePath))
                {
                    string json = r.ReadToEnd();
                    textResults = JsonConvert.DeserializeObject<IList<ReadResult>>(json);
                }
            } else
            {
                ReadInStreamHeaders textHeaders = await _client.ReadInStreamAsync(File.OpenRead(filePath), language: "en");
                string operationLocation = textHeaders.OperationLocation;
                //Thread.Sleep(2000); // This is in the github examples

                // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
                // We only need the ID and not the full URL
                const int numberOfCharsInOperationId = 36;
                string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

                ReadOperationResult results;
                do
                {
                    results = await _client.GetReadResultAsync(Guid.Parse(operationId));
                }
                while ((results.Status == OperationStatusCodes.Running ||
                    results.Status == OperationStatusCodes.NotStarted));

                textResults = results.AnalyzeResult.ReadResults;

                if (writeToJsonPath != null)
                {
                    string json = JsonConvert.SerializeObject(textResults, Formatting.Indented);
                    System.IO.File.WriteAllText(writeToJsonPath, json);
                }
            }

            return textResults[0].Lines;
        }
    }
}
