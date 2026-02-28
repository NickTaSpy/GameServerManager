using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GameServerManager.Client
{
    public static class JSExtensions
    {
        public static async ValueTask ScrollToEnd(this IJSRuntime js, ElementReference textAreaRef)
        {
            await js.InvokeVoidAsync("scrollToEnd", new object[] { textAreaRef });
        }

        public static async ValueTask DownloadFile(this IJSRuntime js, string fileName, string fileUrl)
        {
            await js.InvokeVoidAsync("triggerFileDownload", fileName, fileUrl);
        }
    }
}
