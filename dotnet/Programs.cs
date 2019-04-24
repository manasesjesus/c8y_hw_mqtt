using System;
using System.Threading;
using System.Threading.Tasks;
using Cumulocity.SDK.MQTT.Model;
using Cumulocity.SDK.MQTT.Model.ConnectionOptions;
using Cumulocity.SDK.MQTT.Model.MqttMessage;
using MqttClient = Cumulocity.SDK.MQTT.MqttClient;

namespace hello_mqtt_cs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Task.Run(RunClientAsync);
            new System.Threading.AutoResetEvent(false).WaitOne();
        }

        private static async Task RunClientAsync()
        {
            const string serverUrl = "manga.eu-latest.cumulocity.com/mqtt";
            const string clientId = "my_mqtt_cs_client";
            const string device_name = "My C# MQTT device";
            const string user = "manga/manga@softwareag.com";
            const string password = "zaq12wsx.";

            //connections details
            var cDetails = new ConnectionDetailsBuilder()
                .WithClientId(clientId)
                .WithHost(serverUrl)
                .WithCredentials(user, password)
                .WithCleanSession(true)
                .WithProtocol(TransportType.Tcp)
                .Build();

            MqttClient client = new MqttClient(cDetails);
            client.MessageReceived += Client_MessageReceived;
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
            for (int i = 0; i < 7; i++)
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

        private static void Client_MessageReceived(object sender, IMqttMessageResponse e)
        {
            var content = e.MessageContent;
        }

    }
}
