import React, { useEffect, useState, useRef } from 'react';
import './App.css';
import ImageGallery from './ImageGallery';

const App = () => {
  const [isStreaming, setIsStreaming] = useState(false);
  const [socket, setSocket] = useState(null);
  const [buffer, setBuffer] = useState([]);
  const imageRef = useRef(null);

  useEffect(() => {
    const webSocket = new WebSocket('ws://localhost:8181');

    webSocket.onopen = () => {
      console.log('Connected to WebSocket server');
    };

    webSocket.onmessage = (event) => {
      if (typeof event.data === 'string') {
        console.log('Text message received:', event.data);
      } else {
        // Handle binary data (JPEG frames) and add to buffer
        const blob = new Blob([event.data], { type: 'image/jpeg' });
        const url = URL.createObjectURL(blob);
        setBuffer((prevBuffer) => [...prevBuffer, url]);
      }
    };

    webSocket.onclose = () => {
      console.log('Disconnected from WebSocket server');
    };

    setSocket(webSocket);

    // Clean up the WebSocket connection when the component unmounts
    return () => {
      webSocket.close();
    };
  }, []);

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
          <ImageGallery /> {/* Add ImageGallery component here */}
        </div>
      </div>
  );
};

export default App;
