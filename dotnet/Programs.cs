using System;
using System.Threading;
using System.Threading.Tasks;
using Cumulocity.SDK.MQTT.Model;
using Cumulocity.SDK.MQTT.Model.ConnectionOptions;
using Cumulocity.SDK.MQTT.Model.MqttMessage;
using MqttClient = Cumulocity.SDK.MQTT.MqttClient;

namespace hello_mqtt
{
    class Program
    {
        static void Main (string[] args)
        {
            Console.WriteLine("The application has started. Press Ctrl-C to stop it.");

            var cSource = new CancellationTokenSource();
            var myTask = Task.Factory.StartNew(() => RunJsonViaMqttClientAsync(cSource.Token), cSource.Token);
            Console.CancelKeyPress += (sender, eventArgs) => cSource.Cancel();
            myTask.Wait(cSource.Token);

            Console.WriteLine("Now shutting down");
        }

        private static async Task RunJsonViaMqttClientAsync (CancellationToken cToken)
        {
            const string serverUrl = "mqtt.cumulocity.com";
            const string clientId = "my_mqtt_cs_client";
            const string device_name = "My CS MQTT device";
            const string user = "<<tenant>>/<<username>>";
            const string password = "<<password>>";

            // connections details
            var cDetails = new ConnectionDetailsBuilder()
                .WithClientId(clientId)
                .WithHost(serverUrl)
                .WithCredentials(user, password)
                .WithCleanSession(true)
                .WithProtocol(TransportType.Tcp)
                .Build();

            MqttClient client = new MqttClient(cDetails);
            client.MessageReceived += Client_MessageReceived;
            client.Connected += Client_Connected;
            client.ConnectionFailed += Client_ConnectionFailed;
            await client.EstablishConnectionAsync();

            string topic = "s/us";
            string payload = $"100,{device_name}, c8y_MQTTDevice";
            var message = new MqttMessageRequestBuilder()
                .WithTopicName(topic)
                .WithQoS(QoS.EXACTLY_ONCE)
                .WithMessageContent(payload)
                .Build();

            await client.PublishAsync(message);

            // set device's hardware information
            var deviceMessage = new MqttMessageRequestBuilder()
                .WithTopicName("s/us")
                .WithQoS(QoS.EXACTLY_ONCE)
                .WithMessageContent("110, S123456789, MQTT test model, Rev0.1")
                .Build();

            await client.PublishAsync(deviceMessage);

            // add restart operation
            await client.SubscribeAsync(new MqttMessageRequest() { TopicName = "s/ds" });
            await client.SubscribeAsync(new MqttMessageRequest() { TopicName = "s/e" });
            await client.PublishAsync(new MqttMessageRequestBuilder()
                .WithTopicName("s/us")
                .WithQoS(QoS.EXACTLY_ONCE)
                .WithMessageContent("114,c8y_Restart")
                .Build());

            // generate a random temperature (10º-20º) measurement and send it every second
            Random rnd = new Random();
            while (!cToken.IsCancellationRequested)
            {
                int temp = rnd.Next(10, 20);
                Console.WriteLine("Sending temperature measurement (" + temp + "º) ...");
                await client.PublishAsync(new MqttMessageRequestBuilder()
                    .WithTopicName("s/us")
                    .WithQoS(QoS.EXACTLY_ONCE)
                    .WithMessageContent("211," + temp)
                    .Build());
                Thread.Sleep(1000);
            }
        }

        private static void Client_ConnectionFailed(object sender, ProcessFailedEventArgs e)
        {
            Console.WriteLine("Connection failed");
        }

        private static void Client_Connected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine("Client connected.");
        }

        private static void Client_MessageReceived(object sender, IMqttMessageResponse e)
        {
            var content = e.MessageContent;
            
            Console.WriteLine("Message received");
        }
    }
}
