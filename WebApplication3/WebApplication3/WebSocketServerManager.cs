using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Fleck;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

public class WebSocketServerManager
{
    private readonly WebSocketServer _server;
    private readonly ConcurrentDictionary<IWebSocketConnection, string> _connections;
    private bool _isStreaming = false;
    private IMqttClient _mqttClient;
    private readonly DatabaseManager _databaseManager;

    public WebSocketServerManager(string location)
    {
        _server = new WebSocketServer(location);
        _connections = new ConcurrentDictionary<IWebSocketConnection, string>();
        _databaseManager = new DatabaseManager();
        InitializeMqttClient().GetAwaiter().GetResult();
    }

    public void Start()
    {
        _server.Start(socket =>
        {
            socket.OnOpen = () => HandleOpen(socket);
            socket.OnClose = () => HandleClose(socket);
            socket.OnMessage = message => HandleMessage(socket, message);
            socket.OnBinary = binaryData => HandleBinaryData(socket, binaryData);
        });
    }

    private async Task InitializeMqttClient()
    {
        var options = new MqttClientOptionsBuilder()
            .WithClientId("WebSocketServer")
            .WithTcpServer("mqtt.flespi.io", 1883)
            .WithCredentials("XZdrA3Fg1uvUT0OBDRWrsJMHXGFYFp9XrRg04fl7Z1NYzj3B9joYPAdss1wbmlg3", "XZdrA3Fg1uvUT0OBDRWrsJMHXGFYFp9XrRg04fl7Z1NYzj3B9joYPAdss1wbmlg3")
            .Build();

        _mqttClient = new MqttFactory().CreateMqttClient();
        _mqttClient.UseConnectedHandler(async e =>
        {
            Console.WriteLine("Connected to MQTT Broker");
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("camera/start").Build());
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("camera/stop").Build());
        });

        _mqttClient.UseApplicationMessageReceivedHandler(e =>
        {
            var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Console.WriteLine($"MQTT Message Received: {e.ApplicationMessage.Topic} - {message}");
            HandleMqttMessage(e.ApplicationMessage.Topic, message);
        });

        await _mqttClient.ConnectAsync(options);
    }

    private void HandleMqttMessage(string topic, string message)
    {
        switch (topic)
        {
            case "camera/start":
                _isStreaming = true;
                _databaseManager.SaveMessage("START_STREAM");
                Broadcast("START_STREAM");
                Console.WriteLine("Start streaming command received from MQTT");
                break;
            case "camera/stop":
                _isStreaming = false;
                _databaseManager.SaveMessage("STOP_STREAM");
                Broadcast("STOP_STREAM");
                Console.WriteLine("Stop streaming command received from MQTT");
                break;
            default:
                Console.WriteLine("Unknown MQTT command received");
                break;
        }
    }

    private void HandleOpen(IWebSocketConnection socket)
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
    }

    private void HandleClose(IWebSocketConnection socket)
    {
        try
        {
            if (_connections.TryRemove(socket, out var connectionId))
            {
                Console.WriteLine($"Disconnected: {socket.ConnectionInfo.ClientIpAddress}, ID: {connectionId}, Total connections: {_connections.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on disconnect: {ex.Message}");
        }
    }

    private void HandleMessage(IWebSocketConnection socket, string message)
    {
        try
        {
            if (_connections.TryGetValue(socket, out var connectionId))
            {
                Console.WriteLine($"Message Received from {socket.ConnectionInfo.ClientIpAddress}, ID: {connectionId}: {message}");
            }

            switch (message)
            {
                case "START_STREAM":
                    _isStreaming = true;
                    _databaseManager.SaveMessage("START_STREAM");
                    Console.WriteLine("Start command received");
                    _mqttClient.PublishAsync("camera/start", "START_STREAM");
                    break;
                case "STOP_STREAM":
                    _isStreaming = false;
                    _databaseManager.SaveMessage("STOP_STREAM");
                    Console.WriteLine("Stop command received");
                    _mqttClient.PublishAsync("camera/stop", "STOP_STREAM");
                    break;
                default:
                    Console.WriteLine("Unknown command received");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on message: {ex.Message}");
        }
    }

    private void HandleBinaryData(IWebSocketConnection socket, byte[] binaryData)
    {
        try
        {
            if (_isStreaming)
            {
                Console.WriteLine("Binary data received.");
                Broadcast(binaryData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on binary data: {ex.Message}");
        }
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
