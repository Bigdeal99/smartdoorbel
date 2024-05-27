import { useEffect, useState, useRef } from 'react';

const useWebSocket = (url) => {
    const [isStreaming, setIsStreaming] = useState(false);
    const [socket, setSocket] = useState(null);
    const [buffer, setBuffer] = useState([]);
    const imageRef = useRef(null);

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

        return () => {
            webSocket.close();
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
            setIsStreaming(true);
        }
    };

    const stopStreaming = () => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send('STOP_STREAM');
            setIsStreaming(false);
        }
    };

    const sendMessage = (message) => {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(message);
        }
    };

    return { isStreaming, startStreaming, stopStreaming, imageRef, sendMessage };
};

export default useWebSocket;
