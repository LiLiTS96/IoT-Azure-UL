using AzureDeviceSdkDemo.Device;
using Microsoft.Azure.Devices.Client;
using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Security;
using ProjektZaliczeniowy.Properties;
using System.Resources;
using System.Runtime.Versioning;


internal class Program 
{

    private static async Task Main(string[] args)  
    {
    //exception
    //var client = new OpcClient("opc.tcp://localhost:4840/")
    //client.Connect();
    /*
    OpcReadNode[] commands = new OpcReadNode[]
    {
        new OpcReadNode("ns=2;s=Device 1/ProductionStatus", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/ProductionStatus"),
        new OpcReadNode("ns=2;s=Device 1/ProductionRate", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/ProductionRate"),
        new OpcReadNode("ns=2;s=Device 1/WorkorderId", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/WorkorderId"),
        new OpcReadNode("ns=2;s=Device 1/Temperature", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/Temperature"),
        new OpcReadNode("ns=2;s=Device 1/GoodCount", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/GoodCount"),
        new OpcReadNode("ns=2;s=Device 1/BadCount", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/BadCount"),
        new OpcReadNode("ns=2;s=Device 1/DeviceError", OpcAttribute.DisplayName),
        new OpcReadNode("ns=2;s=Device 1/DeviceError"),
    };

    IEnumerable<OpcValue> job = client.ReadNodes(commands);

    foreach (var item in job)
    {
        Console.WriteLine(item.Value);
    }
    */
        try
        {
            List<string> machinesIdList = new List<string>();

            using (var client = new OpcClient("opc.tcp://localhost:4840/"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                findMachinesId(node, machinesIdList);
                client.Disconnect();

                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);

                Task[] connectionTask = new Task[machinesIdList.Count - 1];
                VirtualDevice[] devices = new VirtualDevice[machinesIdList.Count - 1];
                DeviceClient[] deviceClients = new DeviceClient[machinesIdList.Count - 1];
                OpcClient[] opcClient = new OpcClient[machinesIdList.Count - 1];

                for (int i = 0; i < machinesIdList.Count - 1; i++)
                {
                    opcClient[i] = new OpcClient("opc.tcp://localhost:4840/");
                    opcClient[i].Connect();
                    deviceClients[i] = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                    devices[i] = new VirtualDevice(deviceClients[i]);
                    connectionTask[i] = deviceClients[i].OpenAsync();
                }

                await Task.WhenAll(connectionTask);
                Console.WriteLine("Successfully connected :D");

                while (true)
                {
                    Task[] tasks = new Task[machinesIdList.Count - 1];
                    for (int i = 0; i < machinesIdList.Count - 1; i++)
                    {
                        tasks[i] = taskMachineMethod(machinesIdList[i + 1], opcClient[i], devices[i]);
                    }
                    await Task.WhenAll(tasks);
                    await Task.Delay(5000);
                }
            }
        }
        catch (OpcException e)
        {
            Console.WriteLine("Successfully failed :( " + e.Message);
        }

        static void findMachinesId(OpcNodeInfo node, List<string> machineIdList, int level = 0)
        {
            if (level == 1)
            {
                machineIdList.Add(node.NodeId.ToString());
            }
            level++;
            foreach (var childNode in node.Children())
            {
                findMachinesId(childNode, machineIdList, level);
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

    }

    //Browse(node);
}