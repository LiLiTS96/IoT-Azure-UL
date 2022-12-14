using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
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

        public async Task SendMessages(List<string> dataList)
        {

            foreach(string entry in dataList)
            {
                Message eventMessage = new Message(Encoding.UTF8.GetBytes(entry));
                eventMessage.ContentType = MediaTypeNames.Application.Json;
                eventMessage.ContentEncoding = "utf-8";
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {entry}");

                await client.SendEventAsync(eventMessage);
            }
            Console.WriteLine();
        }

    }
}