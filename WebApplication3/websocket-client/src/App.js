import React from 'react';
import './App.css';
import ImageGallery from './ImageGallery';
import useWebSocket from './useWebSocket';

const App = () => {
  const { isStreaming, startStreaming, stopStreaming, imageRef } = useWebSocket('ws://localhost:8181');

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
