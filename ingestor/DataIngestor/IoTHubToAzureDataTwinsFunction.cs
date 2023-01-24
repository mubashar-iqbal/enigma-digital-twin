using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Azure.Identity;
using Azure.DigitalTwins.Core;
using Azure.Core.Pipeline;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;
using System.Threading.Tasks;
using System.Numerics;

namespace DataIngestor
{

    // [FunctionOutput]
    // public class GetTemperatureOutputDTO: IFunctionOutputDTO 
    // {
    //     [Parameter("int256", "minTemp", 1)]
    //     public virtual BigInteger MinTemp { get; set; }
    //     [Parameter("int256", "maxTemp", 2)]
    //     public virtual BigInteger MaxTemp { get; set; }
    // }
    
    public static class IoTHubToAzureDataTwinsFunction
    {
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("IoTHubToADTFunction")]
        public static async Task RunAsync([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());

            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");

            try
            {
                // Authenticate with Digital Twins
                var cred = new ManagedIdentityCredential("https://digitaltwins.azure.net");
                var client = new DigitalTwinsClient(
                    new Uri(adtInstanceUrl),
                    cred,
                    new DigitalTwinsClientOptions { Transport = new HttpClientTransport(httpClient) });
               
                log.LogInformation($"ADT service client connection created.");

                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    log.LogInformation(eventGridEvent.Data.ToString());

                    JObject deviceMessage = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    
                    string deviceId = (string)deviceMessage["systemProperties"]["iothub-connection-device-id"];
                    double reading = (double)deviceMessage["body"]["reading"];
                    string sensorState = "Funcational";
                  
                    log.LogInformation($"Device: {deviceId} Speedometer Reading is: {reading}, Device state is: {sensorState}");

                    // https://docs.microsoft.com/en-us/azure/digital-twins/how-to-manage-twin
                    
                    var updateTwinData = new JsonPatchDocument();
                    updateTwinData.AppendAdd("/Id", deviceId);
                    updateTwinData.AppendAdd("/Reading", reading);
                    updateTwinData.AppendAdd("/SensorState", sensorState);
                    await client.UpdateDigitalTwinAsync(deviceId, updateTwinData).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in ingest function: {ex.Message}");
            }
        }
    }
}
