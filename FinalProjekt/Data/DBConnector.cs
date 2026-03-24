namespace FinalProjekt.Data;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

public class DBConnector
{
    private readonly HttpClient _client = new();
    private readonly string? _baseUrl = Environment.GetEnvironmentVariable("PB_BASE_URL");
    private readonly string? _adminEmail = Environment.GetEnvironmentVariable("PB_ADMIN_EMAIL");
    private readonly string? _adminPassword = Environment.GetEnvironmentVariable("PB_ADMIN_PASSWORD");
    private string? _token;
    private string? _recordId;

    private async Task<bool> Authenticate()
    {
        var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/admins/auth-with-password", new
        {
            identity = _adminEmail,
            password = _adminPassword
        });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            _token = result?.Token;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            return true;
        }
        return false;
    }

    public async Task<(double balance, double deposited)> Load()
    {
            if (_token == null) await Authenticate();
            
            var response = await _client.GetAsync($"{_baseUrl}/api/collections/casino_data/records?limit=1");
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

            if (_recordId != null)
            {
                await _client.PatchAsJsonAsync($"{_baseUrl}/api/collections/casino_data/records/{_recordId}", data);
            }
            else 
            {
                var response = await _client.PostAsJsonAsync($"{_baseUrl}/api/collections/casino_data/records", data);
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
