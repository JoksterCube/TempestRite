namespace JoksterCube.TempestRite.Common.Networking;

internal static class ZRpcExtensions
{
    internal static string GetPeerHostName(this ZRpc rpc) => rpc.GetSocket().GetHostName();
}