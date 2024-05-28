using System;
using System.Net.Http;
using System.Threading.Tasks;

public class IFTTTNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _host;
    private readonly string _privateKey;

    public IFTTTNotificationService(HttpClient httpClient, string host, string privateKey)
    {
        _httpClient = httpClient;
        _host = host;
        _privateKey = privateKey;
    }

    public async Task SendNotificationAsync(string event)
    {
        var url = $"https://{_host}/trigger/{event}/with/key/{_privateKey}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send notification: {response.StatusCode}");
        }
        else
        {
            Console.WriteLine("Notification sent successfully.");
        }
    }
}