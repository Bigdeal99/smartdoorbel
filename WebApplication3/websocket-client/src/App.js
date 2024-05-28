import React, { useEffect } from 'react';
import './App.css';
import ImageGallery from './ImageGallery';
import useWebSocket from './useWebSocket';
import useMqtt from './useMqtt';

const App = () => {
    const { isStreaming, startStreaming, stopStreaming, imageRef } = useWebSocket('ws://localhost:8181');
    const { messages } = useMqtt('wss://mqtt.flespi.io', 'iot/notification');

    useEffect(() => {
        if (messages.length > 0) {
            // Fetch the latest images when a new message is received
            console.log('New MQTT message received:', messages[messages.length - 1]);
            // You can trigger a refresh of the image gallery here
        }
    }, [messages]);

    return (
        <div className="App">
            <header className="App-header">
                <h1>Smart Doorbell</h1>
            </header>
            <div className="content">
                <div className="video-container">
                    <img ref={imageRef} alt="Live Stream" className="video" />
                </div>
                <div className="controls">
                    <button onClick={startStreaming} disabled={isStreaming}>Start Streaming</button>
                    <button onClick={stopStreaming} disabled={!isStreaming}>Stop Streaming</button>
                </div>
                <ImageGallery />
            </div>
        </div>
    );
};

export default App;
