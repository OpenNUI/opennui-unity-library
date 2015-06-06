using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Threading;


namespace NUIGatewayLibrary_Unity
{
    //데이터를 받기 위한 NamedPipe입니다.
    class NamedPipeClient_In
    {
        NUIApp nuiApp;

        //버퍼 사이즈
        private const int BUFFER_SIZE = 4096;
        string pipeName;
        public string PipeName
        {
            get { return this.pipeName; }
        }

        public NamedPipeClient_In(string pipeName, NUIApp nuiApp)
        {
            this.nuiApp = nuiApp;
            this.pipeName = @"\\.\pipe\" + pipeName;
        }


        #region CreateFile Plag
        public const uint SHARE_READ = (0x00000001);
        public const uint SHARE_WRITE = (0x00000002);
        public const uint GENERIC_READ = (0x80000000);
        public const uint GENERIC_WRITE = (0x40000000);
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = (0x40000000);
        #endregion

        //이벤트입니당
        public delegate void ErrorDelegate();
        public event ErrorDelegate Error;

        public delegate void ServerMessageDelegate(byte[] data);
        public event ServerMessageDelegate ServerMessage;

        private FileStream stream;
        private SafeFileHandle handle;

        public void CLOSE()
        {
            if (stream != null)
                stream.Close();
        }

        //받기 위한 연결시작
        public void Start()
        {
            this.handle =
               Win32APIs.CreateFile(
                  this.pipeName,
                  GENERIC_READ,
                  0,
                  IntPtr.Zero,
                  OPEN_EXISTING,
                  FILE_FLAG_OVERLAPPED,
                  IntPtr.Zero);

            //서버가 동작을 안하고있음
            if (this.handle.IsInvalid)
            {
                if (Error != null)
                    nuiApp.Queue_Event(Error);
                return;
            }

            this.stream = new FileStream(this.handle, FileAccess.Read, BUFFER_SIZE, true);

            WaitForData();
        }


        private void WaitForData()
        {
            WaitForData(new SocketInfo(this.stream, 16));
        }

        private void WaitForData(SocketInfo socketInfo)
        {
            this.stream.BeginRead(socketInfo.Buffer, socketInfo.Index, socketInfo.Buffer.Length - socketInfo.Index, new AsyncCallback(OnDataReceived), socketInfo);
        }

        private void OnDataReceived(IAsyncResult iar)
        {
            //여기서 Unity Main Thread에 작업이 등록되도록 해야 함.
            lock (nuiApp._queueLock)
            {
                nuiApp.Cwork_Queue.Enqueue(new CWORK_STRUCT(1, iar));
            }
        }

        //등록될 작업이라고 하면 이것임.
        public void OnDataReceived_Func(IAsyncResult iar)
        {
            SocketInfo socketInfo = (SocketInfo)iar.AsyncState;

            int received = socketInfo.Stream.EndRead(iar);

            bool a = socketInfo.Stream.CanRead;

            if (received == 0)
            {
                // 에러나면새로받자.
                //WaitForData();
                return;
            }


            socketInfo.Index += received;
            if (socketInfo.Index == socketInfo.Buffer.Length)
            {
                switch (socketInfo.State)
                {
                    case SocketInfo.StateType.Header:
                        // MessageReader headerReader = new MessageReader(socketInfo.Buffer);

                        // headerReader.ReadBytes(2);
                        int packetLength = BitConverter.ToInt32(socketInfo.Buffer, 2);
                        //headerReader.ReadBytes(10); // 16바이트 읽음..
                        socketInfo.State = SocketInfo.StateType.Data;
                        socketInfo.Buffer = new byte[packetLength];
                        socketInfo.Index = 0;
                        WaitForData(socketInfo);
                        break;
                    case SocketInfo.StateType.Data:
                        byte[] data = socketInfo.Buffer;
                        if (data.Length != 0 && ServerMessage != null)
                        {
                            nuiApp.Queue_Event(ServerMessage, data);
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

    }

    //데이터를 보내기 위한 NamedPipe입니다.
    class NamedPipeClient_Out
    {
        NUIApp nuiApp;

        //버퍼 사이즈
        private const int BUFFER_SIZE = 4096;
        string pipeName;
        public string PipeName
        {
            get { return this.pipeName; }
        }

        public NamedPipeClient_Out(string pipeName, NUIApp nuiApp)
        {
            this.pipeName = @"\\.\pipe\" + pipeName;
            this.nuiApp = nuiApp;
        }

        #region CreateFile Plag
        public const uint SHARE_READ = (0x00000001);
        public const uint SHARE_WRITE = (0x00000002);

        public const uint GENERIC_READ = (0x80000000);
        public const uint GENERIC_WRITE = (0x40000000);

        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = (0x40000000);
        #endregion

        //이벤트입니당
        public delegate void ErrorDelegate();
        public event ErrorDelegate Error;

        private FileStream stream;
        private SafeFileHandle handle;

        public void CLOSE()
        {
            if (stream != null)
                stream.Close();
        }

        //보내기 위한 연결시작
        public void Start()
        {
            this.handle =
               Win32APIs.CreateFile(
                  this.pipeName,
                  GENERIC_WRITE,
                  0,
                  IntPtr.Zero,
                  OPEN_EXISTING,
                  FILE_FLAG_OVERLAPPED,
                  IntPtr.Zero);

            //서버가 동작을 안하고있음
            if (this.handle.IsInvalid)
            {
                if (Error != null)
                    nuiApp.Queue_Event(Error);
                return;
            }

            this.stream = new FileStream(this.handle, FileAccess.Write, BUFFER_SIZE, true);
        }

        //메세지 보낸당asd
        public void PushMessage(byte[] message)
        {
            ThreadPool.QueueUserWorkItem((r) =>
            {
                lock (this.stream)
                {
                    this.stream.Write(message, 0, message.Length);
                    this.stream.Flush();
                }
            });
        }
    }
}
