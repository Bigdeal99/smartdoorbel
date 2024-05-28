import { useEffect, useState } from 'react';
import mqtt from 'mqtt';

const useMqtt = (brokerUrl, topic) => {
    const [client, setClient] = useState(null);
    const [messages, setMessages] = useState([]);

    useEffect(() => {
        const client = mqtt.connect(brokerUrl, {
            username: 'XZdrA3Fg1uvUT0OBDRWrsJMHXGFYFp9XrRg04fl7Z1NYzj3B9joYPAdss1wbmlg3',
        });

        client.on('connect', () => {
            console.log('Connected to MQTT broker');
            client.subscribe(topic, (err) => {
                if (err) {
                    console.error('Failed to subscribe to topic', err);
                }
            });
        });

        client.on('message', (topic, message) => {
            setMessages((prevMessages) => [...prevMessages, message.toString()]);
        });

        setClient(client);

        return () => {
            if (client) {
                client.end();
            }
        };
    }, [brokerUrl, topic]);

    return { client, messages };
};

export default useMqtt;
