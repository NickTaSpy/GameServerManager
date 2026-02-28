using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace GameServerManager.Client
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly ILocalStorageService _localStorage;
        private readonly HttpClient _http;

        public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
        {
            _localStorage = localStorage;
            _http = http;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var state = AnonymousState;

            string token = await _localStorage.GetItemAsStringAsync("token");
            _http.DefaultRequestHeaders.Authorization = null;

            if (string.IsNullOrEmpty(token))
            {
                return NotifyStateChangedAndReturn(state);
            }

            var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");

            var expiry = identity.Claims.FirstOrDefault(x => x.Type == "exp");
            if (expiry is null)
            {
                return NotifyStateChangedAndReturn(state);
            }

            if (!long.TryParse(expiry.Value, out var expiryParsed))
            {
                return NotifyStateChangedAndReturn(state);
            }

            var datetime = DateTimeOffset.FromUnixTimeSeconds(expiryParsed).UtcDateTime;
            if (datetime <= DateTime.UtcNow)
            {
                return NotifyStateChangedAndReturn(state);
            }

            state = new AuthenticationState(new ClaimsPrincipal(identity));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("\"", ""));

            return NotifyStateChangedAndReturn(state);
        }

        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs is null)
            {
                return Enumerable.Empty<Claim>();
            }

            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private AuthenticationState NotifyStateChangedAndReturn(AuthenticationState state)
        {
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }
    }
}
