using System;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace DesiredPropertiesModule
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
            
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, null);
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdate, null);
        }

        private static Task DesiredPropertyUpdate(TwinCollection desiredProperties, object _)
        {
            Console.WriteLine($"Desired property change: {JsonConvert.SerializeObject(desiredProperties)}");
            return Task.CompletedTask;
        }

        private static Task<MessageResponse> PipeMessage(Message message, object _)
        {
            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            var timeStampString = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"Received message: \"{messageString}\" | Received message at: {timeStampString}");
            return Task.FromResult(MessageResponse.Completed);
        }
    }
}
