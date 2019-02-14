package c8y.example;

import org.eclipse.paho.client.mqttv3.*;
import java.util.concurrent.*;

public class App {

    public static void main(String[] args) throws Exception {

        // client, user and device details
        final String serverUrl   = "tcp://mqtt.cumulocity.com";     /* ssl://mqtt.cumulocity.com:8883 for a secure connection */
        final String clientId    = "my_mqtt_java_client";
        final String device_name = "My Java MQTT device";
        final String tenant      = "<<tenant>>";
        final String username    = "<<username>>";
        final String password    = "<<password>>";
        
        // MQTT connection options
        final MqttConnectOptions options = new MqttConnectOptions();
        options.setUserName(tenant + "/" + username);
        options.setPassword(password.toCharArray());
        
        // connect the client to Cumulocity
        final MqttClient client = new MqttClient(serverUrl, clientId, null);
        client.connect(options);

        // register a new device
        client.publish("s/us", ("100," + device_name + ",c8y_MQTTDevice").getBytes(), 2, false);
        
        // set device's hardware information
        client.publish("s/us", "110,S123456789,MQTT test model,Rev0.1".getBytes(), 2, false);

        // add restart operation
        client.publish("s/us", "114,c8y_Restart".getBytes(), 2, false);
        
        System.out.println("The device \"" + device_name + "\" has been registered successfully!");

        // listen for operations
        client.subscribe("s/ds", new IMqttMessageListener() {
            public void messageArrived (final String topic, final MqttMessage message) throws Exception {
                final String payload = new String(message.getPayload());
                
                System.out.println("Received operation " + payload);
                if (payload.startsWith("510")) {
                    System.out.println("Simulating device restart...");
                    client.publish("s/us", "501,c8y_Restart".getBytes(), 2, false);
                    System.out.println("...restarting...");
                    Thread.sleep(TimeUnit.SECONDS.toMillis(1));
                    client.publish("s/us", "503,c8y_Restart".getBytes(), 2, false);
                    System.out.println("...done...");
                }
            }
        });

        // generate a random temperature (10º-20º) measurement and send it every 7 seconds
        Executors.newSingleThreadScheduledExecutor().scheduleWithFixedDelay(new Runnable() {
            public void run () {
                try {
                    int temp = (int) (Math.random() * 10 + 10);
                    
                    System.out.println("Sending temperature measurement (" + temp + "º) ...");
                    client.publish("s/us", new MqttMessage(("211," + temp).getBytes()));
                } catch (MqttException e) {
                    e.printStackTrace();
                }
            }
        }, 1, 7, TimeUnit.SECONDS);
    }
}
