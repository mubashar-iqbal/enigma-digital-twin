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
using System.Linq;

namespace DataIngestor
{
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
                    string classifier = deviceMessage["body"]["classifier"].ToString();
                    string sensorState = "Funcational"; // @TODO define mechanism for sensor state

                    // can bus payload
                    int ts = (int)deviceMessage["body"]["ts"];
                    int id_mf = (int)deviceMessage["body"]["id_mf"];
                    string can_pl_data_bytes = deviceMessage["body"]["can_pl_data_bytes"].ToString();
                    int td_ts_mf = (int)deviceMessage["body"]["td_ts_mf"];
                    int td_id_mf = (int)deviceMessage["body"]["td_id_mf"];
                    int dlc = (int)deviceMessage["body"]["dlc"];
                    
                    var updateTwinData = new JsonPatchDocument();

                    // sensor details                    
                    updateTwinData.AppendAdd("/Id", deviceId);
                    updateTwinData.AppendAdd("/SensorState", sensorState);

                    // can bus payload mapping to speedometer DT
                    updateTwinData.AppendAdd("/CanBusPayload/TS", ts);
                    updateTwinData.AppendAdd("/CanBusPayload/ID_MF", id_mf);
                    updateTwinData.AppendAdd("/CanBusPayload/CAN_PL_DATA_BYTES", can_pl_data_bytes);  // comma separated can bus payload bytes
                    updateTwinData.AppendAdd("/CanBusPayload/TD_TS_MF", td_ts_mf);
                    updateTwinData.AppendAdd("/CanBusPayload/TD_ID_MF", td_id_mf);
                    updateTwinData.AppendAdd("/CanBusPayload/DLC", dlc);

                    // classifier: Attack or N-Attack
                    updateTwinData.AppendAdd("/Classifier", classifier);

                    // update speedometer DT
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
