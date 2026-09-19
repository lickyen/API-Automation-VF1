using System.Runtime.InteropServices;

namespace ProductivityApi.Helpers
{
    public sealed class NetworkConnection : IDisposable
    {
        private readonly string _networkName;

        public NetworkConnection(
            string networkName,
            string userName,
            string password)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplayType.Share,
                RemoteName = networkName
            };

            var result = WNetAddConnection2(
                netResource,
                password,
                userName,
                0);

            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to connect to network share. Error code: {result}");
            }
        }

        public void Dispose()
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(
            NetResource netResource,
            string password,
            string username,
            int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(
            string name,
            int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        private class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplayType DisplayType;
            public int Usage;
            public string? LocalName;
            public string? RemoteName;
            public string? Provider;
        }

        private enum ResourceScope
        {
            GlobalNetwork = 2
        }

        private enum ResourceType
        {
            Disk = 1
        }

        private enum ResourceDisplayType
        {
            Share = 3
        }
    }
}


