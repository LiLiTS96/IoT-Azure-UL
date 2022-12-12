using AzureDeviceSdkDemo.Device;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;
using ProjektZaliczeniowy.Properties;

internal class Program 
{
    private static async Task Main(string[] args)  
    {
        try
        {
            Dictionary<string,string> machinesId2TelemetryMap = new Dictionary<string, string>();
            using (var client = new OpcClient("opc.tcp://localhost:4840/"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                findMachinesId(node, machinesId2TelemetryMap);
                //client.Disconnect();

                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);
                int machineIdListSize = machinesId2TelemetryMap.Count - 1;

                Task[] connectionTask = new Task[machineIdListSize];
                VirtualDevice[] devices = new VirtualDevice[machineIdListSize];
                DeviceClient[] deviceClients = new DeviceClient[machineIdListSize];
                OpcClient[] opcClient = new OpcClient[machineIdListSize];

                for (int i = 0; i < machineIdListSize; i++)
                {
                    opcClient[i] = new OpcClient("opc.tcp://localhost:4840/");
                    opcClient[i].Connect();
                    deviceClients[i] = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                    devices[i] = new VirtualDevice(deviceClients[i]);
                    connectionTask[i] = deviceClients[i].OpenAsync();
                }

                await Task.WhenAll(connectionTask);
                Console.WriteLine("Successfully connected");
                while (true)
                {
                    machinesTelemetryDataPrep(machinesId2TelemetryMap, client);
                    await device.SendMessages(machinesId2TelemetryMap);
                    await Task.Delay(5000);
                }
            }
        }
        catch (OpcException e)
        {
            Console.WriteLine("Successfully failed :( " + e.Message);
        }

        static void findMachinesId(OpcNodeInfo node, Dictionary<string, string> machinesId2TelemetryMap, int level = 0)
        {
            if (level == 1 && node.NodeId.ToString().Contains("Device"))
            {
                machinesId2TelemetryMap.Add(node.NodeId.ToString(),null);
            }
            level++;
            foreach (var childNode in node.Children())
            {
                findMachinesId(childNode, machinesId2TelemetryMap, level);
            }
        }

        static OpcReadNode[] readNode(string machineId)
        {
            OpcReadNode[] nodes = new OpcReadNode[]
            {
                new OpcReadNode(machineId+"/ProductionStatus"),
                new OpcReadNode(machineId+"/ProductionRate"),
                new OpcReadNode(machineId+"/WorkorderId"),
                new OpcReadNode(machineId+"/Temperature"),
                new OpcReadNode(machineId+"/GoodCount"),
                new OpcReadNode(machineId+"/BadCount"),
                new OpcReadNode(machineId+"/DeviceError"),
            };
            return nodes;
        }

        async Task taskMachineMethod(string machineId, OpcClient client, VirtualDevice device)
        {
            Console.WriteLine("Successfully connected with {0}", machineId);
            OpcValue[] telemetryValues = new OpcValue[5];
            //odczyt telemetrycznych danych

            telemetryValues[0] = client.ReadNode(machineId + "/ProductionStatus");
            telemetryValues[1] = client.ReadNode(machineId + "/WorkorderId");
            telemetryValues[2] = client.ReadNode(machineId + "/GoodCount");
            telemetryValues[3] = client.ReadNode(machineId + "/BadCount");
            telemetryValues[4] = client.ReadNode(machineId + "/Temperature");

            //test
            await device.sendTelemetryValues(telemetryValues, machineId);
        }

        static void machinesTelemetryDataPrep(Dictionary<string, string> machinesId2TelemetryMap, OpcClient client)
        {
            foreach (KeyValuePair<string, string> entry in machinesId2TelemetryMap)
            {
                    var data = new
                    {
                        product_status = client.ReadNode(entry.Key + "/ProductionStatus").Value,
                        workorder_id = client.ReadNode(entry.Key + "/WorkorderId").Value,
                        good_count = client.ReadNode(entry.Key + "/GoodCount").Value,
                        bad_count = client.ReadNode(entry.Key + "/BadCount").Value,
                        temperature = client.ReadNode(entry.Key + "/Temperature").Value,
                        time_stamp = DateTime.Now.ToLocalTime(),
                    };
                    var dataString = JsonConvert.SerializeObject(data);
                    Console.WriteLine("maszyna "+ entry.Key);
                    machinesId2TelemetryMap[entry.Key] = dataString;
            }
        }
    }
}