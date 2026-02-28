using GameServerManager.Client.Pages;
using GameServerManager.Shared;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GameServerManager.Client
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly IDialogService _dialogService;
        private readonly IJSRuntime _js;

        public ApiService(HttpClient httpClient, IDialogService dialogService, IJSRuntime js)
        {
            _http = httpClient;
            _dialogService = dialogService;
            _js = js;
        }

        public async Task<string> Login(UserLoginDto user)
        {
            var res = await _http.PostAsJsonAsync("api/auth/login", user);
            await HandleErrors(res);
            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> CreateShortTermToken()
        {
            var res = await _http.PostAsync("api/auth/createShortTermToken", null);
            await HandleErrors(res);
            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> ServerStatus(Guid serverId)
        {
            var res = await _http.GetAsync("api/server/status/" + serverId);
            await HandleErrors(res);
            return await res.Content.ReadAsStringAsync();
        }

        public async Task<List<ServerInfo>> ServerList()
        {
            var res = await _http.GetAsync("api/server/list");
            await HandleErrors(res);
            return await res.Content.ReadFromJsonAsync<List<ServerInfo>>() ?? new List<ServerInfo>();
        }

        public async Task<ServerDetails> ServerDetails(Guid serverId)
        {
            var res = await _http.GetAsync($"api/server/{serverId}/details");
            await HandleErrors(res);
            return await res.Content.ReadFromJsonAsync<ServerDetails>() ?? new ServerDetails();
        }

        public async Task StartServer(Guid serverId)
        {
            var res = await _http.PostAsJsonAsync("api/server/start", new ServerIdRequest { ServerId = serverId });
            await HandleErrors(res);
        }

        public async Task RestartServer(Guid serverId)
        {
            var res = await _http.PostAsJsonAsync("api/server/restart", new ServerIdRequest { ServerId = serverId });
            await HandleErrors(res);
        }

        public async Task StopServer(Guid serverId)
        {
            var res = await _http.PostAsJsonAsync("api/server/stop", new ServerIdRequest { ServerId = serverId });
            await HandleErrors(res);
        }

        public async Task ExecuteCommandServer(Guid serverId, string command)
        {
            var res = await _http.PostAsJsonAsync("api/server/executeCommand", new ExecuteCommandRequest { ServerId = serverId, Command = command });
            await HandleErrors(res);
        }

        public async Task<List<FileDetails>> GetServerFiles(Guid serverId, string path)
        {
            var res = await _http.GetAsync($"api/server/{serverId}/files/{path}");
            await HandleErrors(res);
            return await res.Content.ReadFromJsonAsync<List<FileDetails>>() ?? new List<FileDetails>();
        }

        public async Task RenameFile(RenameFileRequest request)
        {
            var res = await _http.PutAsJsonAsync("api/server/renameFile", request);
            await HandleErrors(res);
        }

        public async Task DeleteFile(Guid serverId, string path)
        {
            var res = await _http.DeleteAsync($"api/server/{serverId}/files/{path}");
            await HandleErrors(res);
        }

        public async Task UploadFile(Guid serverId, string path, IEnumerable<IBrowserFile> files)
        {
            using var filesContent = CreateContentFromFiles(files);
            var res = await _http.PostAsync($"api/server/{serverId}/files/{path}", filesContent);
            await HandleErrors(res);
        }

        public async Task CreateServer(CreateServerRequest request)
        {
            var res = await _http.PostAsJsonAsync("api/server", request);
            await HandleErrors(res);
        }

        public ValueTask DownloadFile(Guid serverId, string token, string path)
        {
            return _js.DownloadFile(Path.GetFileName(path), $"api/server/{serverId}/files/download/{token}/{path}");
        }

        public async Task<List<UserInfo>> UserList()
        {
            var res = await _http.GetAsync("api/user/list");
            await HandleErrors(res);
            return await res.Content.ReadFromJsonAsync<List<UserInfo>>() ?? new List<UserInfo>();
        }

        private static MultipartFormDataContent CreateContentFromFiles(IEnumerable<IBrowserFile> files)
        {
            var content = new MultipartFormDataContent();

            foreach (var file in files)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);
                fileContent.Headers.ContentLength = file.Size;
                content.Add(fileContent, "files", file.Name);
            }

            return content;
        }

        private async Task HandleErrors(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var errorData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

            if (errorData is null || !errorData.TryGetValue("detail", out var errorDetail))
            {
                errorDetail = "Oops! I have an issue :(";
            }

            _ = _dialogService.ShowMessageBox("Error", errorDetail.ToString());

            response.EnsureSuccessStatusCode();
        }
    }
}
