using SuperSocket.ClientEngine;

namespace ScClient
{
    public interface IBasicListener
    {
        void OnConnected(Socket socket);
        void OnDisconnected(Socket socket);
        void OnConnectError(Socket socket, ErrorEventArgs e);
        void OnAuthentication(Socket socket, bool status);
        void OnSetAuthToken(string token, Socket socket);
    }
}