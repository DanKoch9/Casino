using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using System.Web;

namespace FinalProjekt.Data;

public class DBConnector
{
    private readonly HttpClient _client = new();
    private string? _token;
    private string? _userId;
    private string? _recordId;
    public string? UserId => _userId;
    private const string TokenFile = "token.dat";

    private string GetUrl() => Environment.GetEnvironmentVariable("PB_BASE_URL") ?? "https://casino.danykoch.cz";

    public bool IsLoggedIn() => LoadLocalToken();

    public void Logout()
    {
        _token = null;
        _userId = null;
        _recordId = null;
        if (File.Exists(TokenFile)) File.Delete(TokenFile);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> Authenticate()
    {
        if (LoadLocalToken()) return true;

        var url = GetUrl();
        AuthMethods? authMethods;
        try 
        {
            authMethods = await _client.GetFromJsonAsync<AuthMethods>($"{url}/api/collections/users/auth-methods");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DB] Connection Error: {ex.Message}");
            return false;
        }

        var google = authMethods?.AuthProviders?.FirstOrDefault(p => p.Name == "google");
        if (google == null)
        {
            Console.WriteLine("[DB] Error: Google OAuth is not configured in PocketBase.");
            return false;
        }

        string redirectUri = "http://localhost:8123/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        string authUrl = $"{google.AuthUrl}{HttpUtility.UrlEncode(redirectUri)}";
        Console.WriteLine("\n[AUTH] Opening browser for Google Login...");
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

        var context = await listener.GetContextAsync();
        var code = context.Request.QueryString["code"];

        using (var response = context.Response)
        {
            string responseString = "<html><body><h1>Login Successful!</h1><p>You can close this window and return to the game.</p></body></html>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
        listener.Stop();

        var authResponse = await _client.PostAsJsonAsync($"{url}/api/collections/users/auth-with-oauth2", new
        {
            provider = "google",
            code = code,
            codeVerifier = google.CodeVerifier,
            redirectUrl = redirectUri
        });

        if (authResponse.IsSuccessStatusCode)
        {
            var result = await authResponse.Content.ReadFromJsonAsync<AuthResponse>();
            _token = result?.Token;
            _userId = result?.Record?.Id;
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
            
            SaveLocalToken();
            return true;
        }

        return false;
    }

    private void SaveLocalToken()
    {
        if (_token != null && _userId != null)
            File.WriteAllLines(TokenFile, new[] { _token, _userId });
    }

    private bool LoadLocalToken()
    {
        if (!string.IsNullOrEmpty(_token)) return true;
        if (!File.Exists(TokenFile)) return false;
        
        var lines = File.ReadAllLines(TokenFile);
        if (lines.Length < 2) return false;
        
        _token = lines[0];
        _userId = lines[1];
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        return true;
    }
public async Task<(double balance, double deposited)> Load()
{
    if (!await Authenticate()) return (1000, 0);

    var url = GetUrl();
    var response = await _client.GetAsync($"{url}/api/collections/casino/records?filter=(user='{_userId}')&limit=1");

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
    else
        {
            
            var createResponse = await _client.PostAsJsonAsync($"{url}/api/collections/casino/records", new
            {
                balance = 1000,
                deposited = 0,
                user = _userId
            });

            if (createResponse.IsSuccessStatusCode)
            {
                var newRecord = await createResponse.Content.ReadFromJsonAsync<GameRecord>();
                _recordId = newRecord?.Id;
                return (1000, 0);
            }
            else
            {
                var errorBody = await createResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"[DB] Failed to create first record: {createResponse.StatusCode} - {errorBody}");
            }
        }

    return (1000, 0); 
}

    public async Task Save(double balance, double deposited)
    {
        if (!await Authenticate()) return;
        
        if (_recordId == null) await Load();

        var data = new { balance, deposited, user = _userId };
        var url = GetUrl();

        if (_recordId != null)
        {
            await _client.PatchAsJsonAsync($"{url}/api/collections/casino/records/{_recordId}", data);
        }
        else 
        {
            var response = await _client.PostAsJsonAsync($"{url}/api/collections/casino/records", data);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GameRecord>();
                _recordId = result?.Id;
            }
        }
    }

    private class AuthMethods { [JsonPropertyName("authProviders")] public List<AuthProvider>? AuthProviders { get; set; } }
    private class AuthProvider 
    { 
        [JsonPropertyName("name")] public string? Name { get; set; } 
        [JsonPropertyName("authUrl")] public string? AuthUrl { get; set; } 
        [JsonPropertyName("codeVerifier")] public string? CodeVerifier { get; set; } 
    }
    private class AuthResponse 
    { 
        [JsonPropertyName("token")] public string? Token { get; set; } 
        [JsonPropertyName("record")] public UserRecord? Record { get; set; }
    }
    private class UserRecord { [JsonPropertyName("id")] public string? Id { get; set; } }
    private class RecordList { [JsonPropertyName("items")] public GameRecord[]? Items { get; set; } }
    private class GameRecord 
    { 
        [JsonPropertyName("id")] public string? Id { get; set; } 
        [JsonPropertyName("balance")] public double Balance { get; set; } 
        [JsonPropertyName("deposited")] public double Deposited { get; set; } 
    }
}
