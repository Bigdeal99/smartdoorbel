using System;
using System.Collections.Concurrent;
using Fleck;

public class WebSocketServerManager
{
    private readonly WebSocketServer _server;
    private readonly ConcurrentDictionary<IWebSocketConnection, string> _connections;
    private bool _isStreaming = false;

    public WebSocketServerManager(string location)
    {
        _server = new WebSocketServer(location);
        _connections = new ConcurrentDictionary<IWebSocketConnection, string>();
    }

    public void Start()
    {
        _server.Start(socket =>
        {
            socket.OnOpen = () =>
            {
                try
                {
                    var connectionId = Guid.NewGuid().ToString();
                    _connections[socket] = connectionId;
                    Console.WriteLine($"Connected: {socket.ConnectionInfo.ClientIpAddress}, ID: {connectionId}, Total connections: {_connections.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on connect: {ex.Message}");
                }
            };

            socket.OnClose = () =>
            {
                try
                {
                    _connections.TryRemove(socket, out var connectionId);
                    Console.WriteLine($"Disconnected: {socket.ConnectionInfo.ClientIpAddress}, ID: {connectionId}, Total connections: {_connections.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on disconnect: {ex.Message}");
                }
            };

            socket.OnMessage = message =>
            {
                try
                {
                    if (_connections.TryGetValue(socket, out var connectionId))
                    {
                        Console.WriteLine($"Message Received from {socket.ConnectionInfo.ClientIpAddress}, ID: {connectionId}: {message}");
                    }
                    if (message == "START_STREAM")
                    {
                        _isStreaming = true;
                        Console.WriteLine("Start command received");
                    }
                    else if (message == "STOP_STREAM")
                    {
                        _isStreaming = false;
                        Console.WriteLine("Stop command received");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on message: {ex.Message}");
                }
            };

            socket.OnBinary = binaryData =>
            {
                try
                {
                    if (_isStreaming)
                    {
                        Console.WriteLine("Binary data received.");
                        // Process and broadcast binary data
                        Broadcast(binaryData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on binary data: {ex.Message}");
                }
            };
        });
    }

    public void Broadcast(string message)
    {
        foreach (var socket in _connections.Keys)
        {
            if (socket.IsAvailable)
            {
                socket.Send(message);
            }
        }
    }

    public void Broadcast(byte[] data)
    {
        foreach (var socket in _connections.Keys)
        {
            if (socket.IsAvailable)
            {
                socket.Send(data);
            }
        }
    }
}
