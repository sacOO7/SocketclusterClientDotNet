using SuperSocket.ClientEngine;

namespace ScClient
{

    public interface BasicListener
    {
        void onConnected(Socket socket);
        void onDisconnected(Socket socket);
        void onConnectError(Socket socket,ErrorEventArgs e);
        void onAuthentication(Socket socket, bool status);
        void onSetAuthToken(string token, Socket socket);

    }
}