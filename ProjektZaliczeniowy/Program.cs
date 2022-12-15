using AzureDeviceSdkDemo.Device;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;
using ProjektZaliczeniowy.Properties;
using System.Diagnostics.Metrics;

internal class Program 
{
    private static string NODE_PRODUCT_STATUS = "/ProductionStatus";
    private static string NODE_WORKORDER_ID = "/WorkorderId";
    private static string NODE_GOOD_COUNT = "/GoodCount";
    private static string NODE_BAD_COUNT = "/BadCount";
    private static string NODE_TEMPERATURE = "/Temperature";
    private static string NODE_DEVICE_ERROR = "/DeviceError";
    private static string NODE_PRODUCT_RATE = "/ProductionRate";
    private static string PARAM_MACHINE_ID = "machine_id";
    private static string PARAM_PRODUCT_STATUS = "product_status";
    private static string PARAM_WORKORDER_ID = "workorder_id";
    private static string PARAM_GOOD_COUNT = "good_count";
    private static string PARAM_BAD_COUNT = "bad_count";
    private static string PARAM_TEMPERATURE = "temperature";
    private static string PARAM_DEVICE_ERROR = "device_error";
    private static string PARAM_PRODUCT_RATE = "product_rate";
    private static string STR_DEVICE = "Device";
    private static List<string> LIST_TELEMETRY_PARAMS = new List<string>
    {
        PARAM_MACHINE_ID,
        PARAM_PRODUCT_STATUS,
        PARAM_WORKORDER_ID,
        PARAM_GOOD_COUNT,
        PARAM_BAD_COUNT,
        PARAM_TEMPERATURE
    };

    private static async Task Main(string[] args)  
    {
        try
        {
            List<MachineData> machineDataList = new List<MachineData>();
            using (var client = new OpcClient("opc.tcp://localhost:4840/"))
            {
                client.Connect();
                var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
                findMachinesId(node, machineDataList);

                using var deviceClient = DeviceClient.CreateFromConnectionString(Resources.connectionString, TransportType.Mqtt);
                await deviceClient.OpenAsync();
                var device = new VirtualDevice(deviceClient);
                await device.InitializeHandlers(client);
                //await device.UpdateTwinAsync();
                Console.WriteLine("Successfully connected");

                foreach (MachineData entry in machineDataList)
                {
                    Console.WriteLine(entry.machineId);
                }

                while (true)
                {
                    readNodes(machineDataList, client);
                    await device.SendMessages(filterTelemetry2Send(machineDataList, LIST_TELEMETRY_PARAMS));
                    await Task.Delay(5000);
                }
            }
        }
        catch (OpcException e)
        {
            Console.WriteLine("Connection failed: " + e.Message);
        }

        static void findMachinesId(OpcNodeInfo node, List<MachineData> machineDataList, int level = 0)
        {
            if (level == 1 && node.NodeId.ToString().Contains(STR_DEVICE))
            {
                machineDataList.Add(new MachineData
                {
                    machineId = node.NodeId.ToString()
                });
            }
            level++;
            foreach (var childNode in node.Children())
            {
                findMachinesId(childNode, machineDataList, level);
            }
        }

        static void readNodes(List<MachineData> machineDataList, OpcClient client)
        {
            foreach (MachineData machine in machineDataList)
            {
                machine.productStatus = (int)client.ReadNode(machine.machineId + NODE_PRODUCT_STATUS).Value;
                machine.workorderId = (string)client.ReadNode(machine.machineId + NODE_WORKORDER_ID).Value;
                machine.goodCount = (int)(long)client.ReadNode(machine.machineId + NODE_GOOD_COUNT).Value;
                machine.badCount = (int)(long)client.ReadNode(machine.machineId + NODE_BAD_COUNT).Value;
                machine.temperature = (double)client.ReadNode(machine.machineId + NODE_TEMPERATURE).Value;
                machine.deviceError = (int)client.ReadNode(machine.machineId + NODE_DEVICE_ERROR).Value;
                machine.productRate = (int)client.ReadNode(machine.machineId + NODE_PRODUCT_RATE).Value;
                machine.readTimeStamp = DateTime.Now;
            }
        }

        static List<string> filterTelemetry2Send(List<MachineData> machineDataList, List<string> requiredParamsList)
        {
            List<string> prepData = new List<string>();
            foreach(MachineData machine in machineDataList)
            {
                string data = "{";
                foreach (string param in requiredParamsList)
                {
                    machine.addValueAndParam(ref data,param);
                }
                prepData.Add(data.Remove(data.Length - 1, 1) + "}");
            }
            return prepData;
        }
    }

    public class MachineData
    {
        public string machineId { get; set; }
        public int productStatus { get; set; }
        public string workorderId { get; set; }
        public int goodCount { get; set; }
        public int badCount { get; set; }
        public double temperature { get; set; }
        public int deviceError { get; set; }
        public int productRate { get; set; }
        public DateTime readTimeStamp { get; set; }

        public void addValueAndParam(ref string data, string paramName)
        {
            data += "\"" + paramName + "\":";
            if (paramName == "machine_id")
                data += "\"" + machineId + "\"";
            if (paramName == "product_status")
                data += productStatus;
            if (paramName == "workorder_id")
                data += "\"" + workorderId + "\"";
            if (paramName == "good_count")
                data += goodCount;
            if (paramName == "bad_count")
                data += badCount;
            if (paramName == "temperature")
                data += temperature.ToString().Replace(',','.');
            if (paramName == "device_error")
                data += deviceError;
            if (paramName == "product_rate")
                data += productRate;
            if (paramName == "read_time_stamp")
                data += "\"" + readTimeStamp + "\"";
            data += ",";
        }
    }
}