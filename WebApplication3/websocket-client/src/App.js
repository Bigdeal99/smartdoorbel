import React from 'react';
import './App.css';
import ImageGallery from './ImageGallery';
import useWebSocket from './useWebSocket';
import { ToastContainer, toast } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

const App = () => {
    const { isStreaming, startStreaming, stopStreaming, imageRef } = useWebSocket('ws://localhost:8181');

    const handleStartStreaming = () => {
        startStreaming();
        toast.success('Streaming started!');
    };

    const handleStopStreaming = () => {
        stopStreaming();
        toast.info('Streaming stopped.');
    };

    return (
        <div className="App">
            <ToastContainer />
            <header className="App-header">
                <h1>Smart Doorbell</h1>
            </header>
            <div className="content">
                <div className="video-container">
                    <img ref={imageRef} alt="Live Stream" className="video" />
                </div>
                <div className="controls">
                    <button onClick={handleStartStreaming} disabled={isStreaming}>Start Streaming</button>
                    <button onClick={handleStopStreaming} disabled={!isStreaming}>Stop Streaming</button>
                </div>
                <ImageGallery />
            </div>
        </div>
    );
};

export default App;
