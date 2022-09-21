using System;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;

namespace MessageOutputModule
{
    internal static class Program
    {
        private static void Main()
        {
            Init().Wait();
            
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += ctx => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait(cts.Token);
        }

        private static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        private static async Task Init()
        {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };
            
            var ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            while (true)
            {
                var timeStampString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                await ioTHubModuleClient.SendEventAsync("output1", new Message(Encoding.UTF8.GetBytes($"Hello at [{timeStampString}]")));
                Console.WriteLine($"Message sent at [{timeStampString}]");
                await Task.Delay(5000);
            }
        }
    }
}
