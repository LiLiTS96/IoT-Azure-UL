using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using Opc.UaFx;
using Opc.UaFx.Client;

namespace AzureDeviceSdkDemo.Device
{
    public class VirtualDevice
    {

        private readonly DeviceClient client;

        public VirtualDevice(DeviceClient client)
        {
            this.client = client;
        }

        public async Task sendTelemetryValues(OpcValue[] telemetryValues, string machineId)
        {
            var data = new
            {
                product_status = telemetryValues[0].Value,
                workorder_id = telemetryValues[1].Value,
                good_count = telemetryValues[2].Value,
                bad_count = telemetryValues[3].Value,
                temperature = telemetryValues[4].Value,
            };

            var dataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";

            await client.SendEventAsync(eventMessage);

            Console.WriteLine($"\t {DateTime.Now.ToLocalTime()} z mazyny {machineId}");
        }
        
    }
}