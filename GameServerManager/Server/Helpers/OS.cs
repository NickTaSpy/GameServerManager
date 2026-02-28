using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GameServerManager.Server.Helpers
{
    public static class OS
    {
        [SupportedOSPlatformGuard("Windows")]
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [SupportedOSPlatformGuard("Linux")]
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        [SupportedOSPlatformGuard("OSX")]
        public static readonly bool IsOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
