using System;
using System.Collections.Generic;
using System.Text;

namespace OpenNUI.Unity.Library
{
    class IPCStream_In
    {
        NUIApp nuiApp;

        public delegate void MessageReceivedHandler(MessageReader reader);
        public delegate void StreamDisconnectedHandler();
        public event StreamDisconnectedHandler OnClientDisconnected;
        public event MessageReceivedHandler OnMessageReceived;

        private NamedPipeClient_In _client;

        public IPCStream_In(NamedPipeClient_In client, NUIApp nuiApp)
        {
            _client = client;
            _client.ServerMessage += client_ServerMessage;
            _client.Error += client_Error;
            this.nuiApp = nuiApp;
        }

        void client_ServerMessage(byte[] data)
        {
            if (data.Length != 0 && OnMessageReceived != null)
            {
                MessageReader reader = new MessageReader(data);

                nuiApp.Queue_Event(OnMessageReceived, reader);
            }
        }

        void client_Error()
        {
            if (OnClientDisconnected != null)
                nuiApp.Queue_Event(OnClientDisconnected);
        }

        void client_Disconnected()
        {
            if (OnClientDisconnected != null)
                nuiApp.Queue_Event(OnClientDisconnected);
        }

    }
    class IPCStream_Out
    {
        NUIApp nuiApp;

        public delegate void StreamDisconnectedHandler();
        public event StreamDisconnectedHandler OnClientDisconnected;

        private NamedPipeClient_Out _client;

        public IPCStream_Out(NamedPipeClient_Out client, NUIApp nuiApp)
        {
            _client = client;
            _client.Error += client_Error;
            this.nuiApp = nuiApp;
        }

        void client_Error()
        {
            if (OnClientDisconnected != null)
                nuiApp.Queue_Event(OnClientDisconnected);
        }

        void client_Disconnected()
        {
            if (OnClientDisconnected != null)
                nuiApp.Queue_Event(OnClientDisconnected);
        }

        public void Send(MessageWriter message)
        {
            byte[] originBuffer = message.ToArray();
            byte[] sendBuffer = new byte[originBuffer.Length + 16];
            byte[] header = new byte[] { 45, 127 };
            byte[] packetLen = BitConverter.GetBytes(originBuffer.Length);

            System.Buffer.BlockCopy(header, 0, sendBuffer, 0, header.Length);
            System.Buffer.BlockCopy(packetLen, 0, sendBuffer, header.Length, packetLen.Length);
            System.Buffer.BlockCopy(originBuffer, 0, sendBuffer, 16, originBuffer.Length);

            if (_client != null)
                _client.PushMessage(sendBuffer);

        }
    }
}
