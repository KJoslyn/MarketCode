using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureOCR
{
    public abstract class ImageToModelsClient<T>
    {
        private ComputerVisionClient _client;
        private ModelBuilder<T> _builder;

        public ImageToModelsClient(OCRConfig config, ModelBuilder<T> builder)
        {
            _builder = builder;
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(config.SubscriptionKey))
            { Endpoint = config.Endpoint };
        }

        public async Task<IEnumerable<T>> BuildModelsFromImage(
            string filePath,
            string writeToJsonPath = null)
        {
            IEnumerable<Line> lines = await ExtractLinesFromImage(filePath, writeToJsonPath);
            ValidateOrThrow(lines);
            return _builder.CreateModels(lines).ToList();
        }

        protected abstract void ValidateOrThrow(IEnumerable<Line> lines);

        private async Task<IList<Line>> ExtractLinesFromImage(string filePath, string writeToJsonPath = null)
        {
            IList<ReadResult> textResults;

            if (filePath.EndsWith("json"))
            {
                using (StreamReader r = new StreamReader(filePath))
                {
                    string json = r.ReadToEnd();
                    textResults = JsonConvert.DeserializeObject<IList<ReadResult>>(json);
                }
            }
            else
            {
                ReadInStreamHeaders textHeaders;
                try
                {
                     textHeaders = await _client.ReadInStreamAsync(File.OpenRead(filePath), language: "en");
                } catch (Exception ex)
                {
                    Log.Error(ex, "Error using Azure's ReadInStreamAsync method");
                    return null;
                }
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
