using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Net.Mime;
using System.Text;
using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Shared;

namespace AzureDeviceSdkDemo.Device
{
    public class VirtualDevice
    {

        private readonly DeviceClient client;

        public VirtualDevice(DeviceClient client)
        {
            this.client = client;
        }
        #region Sending Messages
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
        #endregion Sending Messages
        #region Receiving Messages

        private async Task OnC2dMessageReceivedAsync(Message receivedMessage, object _)
        {
            Console.WriteLine($"\t{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
            PrintMessage(receivedMessage);

            await client.CompleteAsync(receivedMessage);
            Console.WriteLine($"\t{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");

            receivedMessage.Dispose();
        }

        private void PrintMessage(Message receivedMessage)
        {
            string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
            Console.WriteLine($"\t\tReceived message: {messageData}");

            int propCount = 0;
            foreach (var prop in receivedMessage.Properties)
            {
                Console.WriteLine($"\t\tProperty[{propCount++}> Key={prop.Key} : Value={prop.Value}");
            }
        }

        #endregion Receiving Messages
        #region Device Twin
        public async Task UpdateTwinAsync()
        {
            var twin = await client.GetTwinAsync();

            Console.WriteLine($"\nInitial twin value received: \n{JsonConvert.SerializeObject(twin, Formatting.Indented)}");
            Console.WriteLine();

            var reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastAppLaunch"] = DateTime.Now;

            await client.UpdateReportedPropertiesAsync(reportedProperties);
        }

        private async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
        {
            Console.WriteLine($"\tDesired property change:\n\t{JsonConvert.SerializeObject(desiredProperties)}");
            Console.WriteLine("\tSending current time as reported property");
            TwinCollection reportedProperties = new TwinCollection();
            reportedProperties["DateTimeLastDesiredPropertyChangeReceived"] = DateTime.Now;

            await client.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        }
        #endregion Device Twin
        #region Direct Methods

        private async Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = userContext as OpcClient;
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { machineId = default(string) });
            object[] result = opcClient.CallMethod(
                    payload.machineId,
                    payload.machineId + "/EmergencyStop"
                    );
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = userContext as OpcClient;
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { machineId = default(string) });
            object[] result = opcClient.CallMethod(
                    payload.machineId,
                    payload.machineId + "/ResetErrorStatus"
                    );
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> DecreaseProductRateHandler(MethodRequest methodRequest, object userContext)
        {
            string productionRate = "/ProductionRate";
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            var opcClient = userContext as OpcClient;
            var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { machineId = default(string) });
            OpcStatus result = opcClient.WriteNode(payload.machineId + productionRate, (int)opcClient.ReadNode(payload.machineId + productionRate).Value - 10);
            Console.WriteLine(result.ToString());
            return new MethodResponse(0);
        }

        private async Task<MethodResponse> MaintenanceDoneHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD EXECUTED: {methodRequest.Name}");
            //var payload = JsonConvert.DeserializeAnonymousType(methodRequest.DataAsJson, new { nrOfMessages = default(int), delay = default(int) });
            //await SendMessages(payload.nrOfMessages, payload.delay);
            return new MethodResponse(0);
        }

        private static async Task<MethodResponse> DefaultServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\tMETHOD NOT EXIST: {methodRequest.Name}");
            await Task.Delay(1000);
            return new MethodResponse(0);
        }
        #endregion Direct Methods
        public async Task InitializeHandlers(OpcClient opcClient)
        {
            await client.SetReceiveMessageHandlerAsync(OnC2dMessageReceivedAsync, client);

            await client.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, opcClient);
            await client.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, opcClient);
            await client.SetMethodHandlerAsync("DecreaseProductRate", DecreaseProductRateHandler, opcClient);
            await client.SetMethodHandlerAsync("MaintenanceDone", MaintenanceDoneHandler, client);
            await client.SetMethodDefaultHandlerAsync(DefaultServiceHandler, client);

            //await client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, client); //twin
        }
    }
}