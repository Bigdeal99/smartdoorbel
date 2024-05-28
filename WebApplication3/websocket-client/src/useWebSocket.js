import { useEffect, useState, useRef } from 'react';
import mqtt from 'mqtt';

const useWebSocket = (url) => {
    const [isStreaming, setIsStreaming] = useState(false);
    const [socket, setSocket] = useState(null);
    const [buffer, setBuffer] = useState([]);
    const imageRef = useRef(null);
    const mqttClient = useRef(null);

    useEffect(() => {
        const webSocket = new WebSocket(url);

        webSocket.onopen = () => {
            console.log('Connected to WebSocket server');
        };

        webSocket.onmessage = (event) => {
            if (typeof event.data === 'string') {
                console.log('Text message received:', event.data);
            } else {
                const blob = new Blob([event.data], { type: 'image/jpeg' });
                const url = URL.createObjectURL(blob);
                setBuffer((prevBuffer) => [...prevBuffer, url]);
            }
        };

        webSocket.onclose = () => {
            console.log('Disconnected from WebSocket server');
        };

        setSocket(webSocket);

        mqttClient.current = mqtt.connect('wss://mqtt.flespi.io', {
            clientId: 'web-client',
            username: 'XZdrA3Fg1uvUT0OBDRWrsJMHXGFYFp9XrRg04fl7Z1NYzj3B9joYPAdss1wbmlg3',
            password: 'XZdrA3Fg1uvUT0OBDRWrsJMHXGFYFp9XrRg04fl7Z1NYzj3B9joYPAdss1wbmlg3'
        });

        mqttClient.current.on('connect', () => {
            console.log('Connected to MQTT broker');
        });

        return () => {
            webSocket.close();
            mqttClient.current.end();
        };
    }, [url]);

    useEffect(() => {
        if (buffer.length > 0) {
            const url = buffer[0];
            imageRef.current.src = url;
            imageRef.current.onload = () => {
                URL.revokeObjectURL(url);
                setBuffer((prevBuffer) => prevBuffer.slice(1));
            };
        }
    }, [buffer]);

    const startStreaming = () => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send('START_STREAM');
            mqttClient.current.publish('camera/start', 'START_STREAM');
            setIsStreaming(true);
        }
    };

    const stopStreaming = () => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send('STOP_STREAM');
            mqttClient.current.publish('camera/stop', 'STOP_STREAM');
            setIsStreaming(false);
        }
    };

    return { isStreaming, startStreaming, stopStreaming, imageRef };
};

export default useWebSocket;
