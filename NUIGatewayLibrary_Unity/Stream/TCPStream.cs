using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Text;

namespace NUIGatewayLibrary_Unity
{
    class TCPStream
    {
        NUIApp nuiApp;

        public delegate void MessageReceivedHandler(MessageReader reader);
        public delegate void StreamDisconnectedHandler();

        public event StreamDisconnectedHandler OnClientDisconnected;
        public event MessageReceivedHandler OnMessageReceived;

        private readonly Socket _socket;
        public Socket Socket
        {
            get { return _socket; }
        }

        public TCPStream(Socket pSocket, NUIApp nuiApp)
        {
            this.nuiApp = nuiApp;
            _socket = pSocket;
            WaitForData(new SocketInfo(_socket, 16));
        }

        private void WaitForData()
        {
            WaitForData(new SocketInfo(_socket, 16));
        }

        private void WaitForData(SocketInfo socketInfo)
        {
            try
            {
                _socket.BeginReceive(socketInfo.Buffer,
                    socketInfo.Index,
                    socketInfo.Buffer.Length - socketInfo.Index,
                    SocketFlags.None,
                    new AsyncCallback(OnDataReceived),
                    socketInfo);

            }
            catch (Exception e)
            {
                if (OnClientDisconnected != null)
                {
                    nuiApp.Queue_Event(OnClientDisconnected);
                }
            }
        }

        private void OnDataReceived(IAsyncResult iar)
        {
            //여기서 Unity Main Thread에 작업이 등록되도록 해야 함.
            lock (nuiApp._queueLock)
            {
                nuiApp.Cwork_Queue.Enqueue(new CWORK_STRUCT(0, iar));
            }
        }

        public void OnDataReceived_Func(IAsyncResult iar)
        {
            SocketInfo socketInfo = (SocketInfo)iar.AsyncState;
            try
            {
                int received = socketInfo.Socket.EndReceive(iar);

                if (received == 0)
                {
                    if (OnClientDisconnected != null)
                    {
                        nuiApp.Queue_Event(OnClientDisconnected);
                    }
                    return;
                }

                socketInfo.Index += received;

                if (socketInfo.Index == socketInfo.Buffer.Length)
                {
                    switch (socketInfo.State)
                    {
                        case SocketInfo.StateType.Header:
                            MessageReader headerReader = new MessageReader(socketInfo.Buffer);
                            headerReader.ReadBytes(2);
                            int packetLength = headerReader.ReadInt();
                            headerReader.ReadBytes(10); // 16바이트 읽음..
                            socketInfo.State = SocketInfo.StateType.Data;
                            socketInfo.Buffer = new byte[packetLength];
                            socketInfo.Index = 0;
                            WaitForData(socketInfo);
                            break;
                        case SocketInfo.StateType.Data:
                            byte[] data = socketInfo.Buffer;
                            if (data.Length != 0 && OnMessageReceived != null)
                            {
                                nuiApp.Queue_Event(OnMessageReceived, new MessageReader(data));
                            }
                            WaitForData();
                            break;
                    }
                }
                else
                {
                    WaitForData(socketInfo);
                }
            }
            catch (ObjectDisposedException)
            {
                if (OnClientDisconnected != null)
                {
                    nuiApp.Queue_Event(OnClientDisconnected);
                }
            }
            catch (SocketException se)
            {
                if (se.ErrorCode != 10054)
                {
                }
                if (OnClientDisconnected != null)
                {
                    nuiApp.Queue_Event(OnClientDisconnected);
                }
            }
            catch (Exception e)
            {
                if (OnClientDisconnected != null)
                {
                    nuiApp.Queue_Event(OnClientDisconnected);
                }
            }
        }


        public void Send(MessageWriter message)
        {
            try
            {
                byte[] originBuffer = message.ToArray();
                byte[] sendBuffer = new byte[originBuffer.Length + 16];
                byte[] header = new byte[] { 45, 127 };
                byte[] packetLen = BitConverter.GetBytes(originBuffer.Length);

                System.Buffer.BlockCopy(header, 0, sendBuffer, 0, header.Length);
                System.Buffer.BlockCopy(packetLen, 0, sendBuffer, header.Length, packetLen.Length);
                System.Buffer.BlockCopy(originBuffer, 0, sendBuffer, 16, originBuffer.Length);

                _socket.Send(sendBuffer);
            }
            catch (Exception ex)
            {
                if (OnClientDisconnected != null)
                {
                    nuiApp.Queue_Event(OnClientDisconnected);
                }
            }
        }
    }
}
