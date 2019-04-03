// MQTT dependency https://github.com/mqttjs/MQTT.js
const mqtt = require("mqtt");

// client, user and device details
const serverUrl   = "tcp://mqtt.cumulocity.com";
const clientId    = "my_mqtt_nodejs_client";
const device_name = "My Node.js MQTT device";
const tenant      = "<<tenant>>";
const username    = "<<username>>";
const password    = "<<password>>";

var temperature   = 25;

// connect the client to Cumulocity 
const client = mqtt.connect(serverUrl, {
    username: tenant + "/" + username,
    password: password,
    clientId: clientId
});

// once connected...
client.on("connect", function () {
    // ...register a new device with restart operation
    client.publish("s/us", "100," + device_name + ",c8y_MQTTDevice", function() {
        client.publish("s/us", "114,c8y_Restart", function() { 
            console.log("Device registered with restart operation support");
        });

        // listen for operations
        client.subscribe("s/ds");

        // send a temperature measurement every 3 seconds
        setInterval(function() {
            console.log("Sending temperature measurement: " + temperature + "º");
            client.publish("s/us", "211," + temperature);
            temperature += 0.5 - Math.random();
        }, 3000);
    });
    
    console.log("\nUpdating hardware information...");
    client.publish("s/us", "110,S123456789,MQTT test model,Rev0.1");
});

// display all incoming messages
client.on("message", function (topic, message) {
    console.log('Received operation "' + message + '"');
    if (message.toString().indexOf("510") === 0) {
        console.log("Simulating device restart...");
        client.publish("s/us", "501,c8y_Restart");
        console.log("...restarting...");
        setTimeout(function() {
            client.publish("s/us", "503,c8y_Restart");
        }, 1000);
        console.log("...done...");
    }
});
