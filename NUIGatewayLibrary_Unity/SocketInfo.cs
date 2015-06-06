using System.Net.Sockets;
using System.IO;

namespace NUIGatewayLibrary_Unity
{
    class SocketInfo
    {
        public readonly Socket Socket;
        public readonly FileStream Stream;
        public StateType State;
        public byte[] Buffer;
        public int Index;
        public enum StateType { Header, Data }

        public SocketInfo(Socket socket, short headerLength)
        {
            Socket = socket;
            Stream = null;
            State = StateType.Header;
            Buffer = new byte[headerLength];
            Index = 0;
        }
        public SocketInfo(FileStream stream, short headerLength)
        {
            Socket = null;
            Stream = stream;
            State = StateType.Header;
            Buffer = new byte[headerLength];
            Index = 0;
        }
    }
}
