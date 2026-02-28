using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameServerManager.Client
{
    public class HubConnectorService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly NavigationManager _navManager;

        public HubConnectorService(ILocalStorageService localStorage, NavigationManager navManager)
        {
            _localStorage = localStorage;
            _navManager = navManager;
        }

        public HubConnection Create(string relativeUri)
        {
            return new HubConnectionBuilder()
                .WithUrl(_navManager.ToAbsoluteUri(relativeUri), options =>
                    options.AccessTokenProvider = async () => (await _localStorage.GetItemAsStringAsync("token")).Replace("\"", ""))
                .Build();
        }
    }
}
