using Opc.UaFx;
using Opc.UaFx.Client;

using (var client = new OpcClient("opc.tcp://localhost:4840/"))
{
    //exception
    client.Connect();
}

Console.ReadLine();