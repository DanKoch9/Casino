namespace FinalProjekt.Data;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class DBConnector
{
    private readonly HttpClient _client = new();
    private string? _token;
    private string? _recordId;

    private string GetUrl() => Environment.GetEnvironmentVariable("PB_BASE_URL") ?? "";

    private async Task<bool> Authenticate()
    {
        var email = Environment.GetEnvironmentVariable("PB_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("PB_ADMIN_PASSWORD");
        var url = GetUrl();

        var response = await _client.PostAsJsonAsync($"{url}/api/collections/_superusers/auth-with-password", new
        {
            identity = email,
            password = password
        });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            _token = result?.Token;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            return true;
        }

        Console.WriteLine($"[DB] Auth failed: {response.StatusCode}");
        return false;
    }

    public async Task<(double balance, double deposited)> Load()
    {
            if (_token == null) await Authenticate();
            
            var response = await _client.GetAsync($"{GetUrl()}/api/collections/casino_data/records?limit=1");
            if (response.IsSuccessStatusCode)
            {
                var list = await response.Content.ReadFromJsonAsync<RecordList>();
                if (list?.Items != null && list.Items.Length > 0)
                {
                    var record = list.Items[0];
                    _recordId = record.Id;
                    return (record.Balance, record.Deposited);
                }
            }
        
        return (1000, 0); 
    }

    public async Task Save(double balance, double deposited)
    {
            if (_token == null) await Authenticate();
            
            if (_recordId == null) await Load();

            var data = new { balance, deposited };
            var url = GetUrl();

            if (_recordId != null)
            {
                await _client.PatchAsJsonAsync($"{url}/api/collections/casino_data/records/{_recordId}", data);
            }
            else 
            {
                var response = await _client.PostAsJsonAsync($"{url}/api/collections/casino_data/records", data);
                var result = await response.Content.ReadFromJsonAsync<GameRecord>();
                _recordId = result?.Id;
            }
    }
    
    private class AuthResponse { [JsonPropertyName("token")] public string? Token { get; set; } }
    private class RecordList { [JsonPropertyName("items")] public GameRecord[]? Items { get; set; } }
    private class GameRecord 
    { 
        [JsonPropertyName("id")] public string? Id { get; set; } 
        [JsonPropertyName("balance")] public double Balance { get; set; } 
        [JsonPropertyName("deposited")] public double Deposited { get; set; } 
    }
}
