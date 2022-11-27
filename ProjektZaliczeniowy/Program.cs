using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Security;

using (var client = new OpcClient("opc.tcp://localhost:4840/"))
{
    //exception
    client.Connect();

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
    var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
    Browse(node);
}

Console.ReadLine();

static void Browse(OpcNodeInfo node, int level = 0)
{
    Console.WriteLine("{0}{1}({2})",
        new string('.', level * 4),
        node.Attribute(OpcAttribute.DisplayName).Value,
        node.NodeId);
    level++;

    foreach (var childNode in node.Children())
        Browse(childNode, level);
}