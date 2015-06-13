using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace OpenNUI.Unity.Library
{
    public struct CWORK_STRUCT
    {
        public int type; //0 : TCP, 1 : IPC, 2 : Event
        public IAsyncResult iar;

        public KeyValuePair<Delegate, object[]> Event;

        public CWORK_STRUCT(int type, IAsyncResult iar)
        {
            this.type = type;
            this.iar = iar;
            this.Event = new KeyValuePair<Delegate, object[]>();
        }
        public CWORK_STRUCT(KeyValuePair<Delegate, object[]> Event)
        {
            this.type = 2;
            this.iar = null;
            this.Event = Event;
        }
    }

    class NUIApp
    {
        #region IARs
        public Queue<CWORK_STRUCT> Cwork_Queue = null;

        public object _queueLock = null;
        //유니티에서 외부 코루틴 호출을 통해 데이터를 수집하는 부분
        public void IAR_WORK(CWORK_STRUCT cWork)
        {
            if (cWork.type == 0) // TCP
                lifeStream.OnDataReceived_Func(cWork.iar);

            else if (cWork.type == 1) //IPC
                pipeClient_stc.OnDataReceived_Func(cWork.iar);

            //Event Callback
            else if (cWork.type == 2)
            {
              KeyValuePair<Delegate, object[]> events = cWork.Event;
               events.Key.DynamicInvoke(events.Value);
            }
        }

        internal void Queue_Event(Delegate del, params object[] arg)
        {
            if (Cwork_Queue == null)
                return;
           // del.DynamicInvoke(arg);
             Cwork_Queue.Enqueue(new CWORK_STRUCT(new KeyValuePair<Delegate, object[]>(del, arg)));
        }
        #endregion

        public readonly string appName; //사용자가 설정할 이 앱의 이름입니다.
        private int sessionId;          //Service로부터 할당받을 세션 ID

        //제대로 연결되었는지 여부.
        private bool isConnected = false;
        public bool IsConnected
        {
            get { return isConnected; }
        }

        //한번이라도 연결을 시도했었는지 여부.
        internal bool isTryConnectService = false;

        #region values for Data Send, Recive
        //TCP Socket, Streams
        private Socket lifeSocket;
        private TCPStream lifeStream;
        //IPC Pipe, Streams
        private NamedPipeClient_In pipeClient_stc; //Server to Client
        private NamedPipeClient_Out pipeClient_cts; //Client to Server
        private IPCStream_In pipeStream_Get; //Server to Client
        private IPCStream_Out pipeStream_Send; //Client to Server
        #endregion

        //Default Sensor ID
        private int defaultSensorID = -1;
        public int DefaultSensorID
        {
            get { return defaultSensorID; }
            set { defaultSensorID = value; }
        }

        //Realtime Shared values
        private Dictionary<int, NuiSensor> connectedSensors = new Dictionary<int, NuiSensor>(); //Sensors <ID, SensorData>

        public NUIApp(string appName, ref Queue<CWORK_STRUCT> Cwork_Queue, object _queueLock)
        {
            this.Cwork_Queue = Cwork_Queue;
            this._queueLock = _queueLock;
            this.lifeSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.appName = appName;
        }

        #region Message Parssing
        //TCP
        void lifeStream_OnMessageReceived(MessageReader reader)
        {
            //서버로부터 받는 헤더를 읽습니다.
            STCHeader header = (STCHeader)reader.ReadShort();

            switch (header)
            {
                case STCHeader.REQUEST_DEVICE_KIND_TCP:
                    sessionId = reader.ReadInt();

                    //서버로 Device ID를 보냅니다.
                    MessageWriter message = new MessageWriter(CTSHeader.SEND_DEVICE_KIND_TCP);
                    message.WriteInt(0);
                    lifeStream.Send(message);
                    break;

                case STCHeader.SEND_PIPE_CONNECT_TCP:
                    try
                    {
                        //IPC 파이프의 이름을 받아옵니다.
                        string pipeName = reader.ReadString();
                        pipeClient_stc = new NamedPipeClient_In(pipeName + "_stc", this); //Server to Client 파이프를 만듬
                        pipeClient_cts = new NamedPipeClient_Out(pipeName + "_cts", this); //Client to Server 파이프를 만듬

                        //메세지를 보내고 받기 위한 스트림을 생성.
                        pipeStream_Get = new IPCStream_In(pipeClient_stc, this);
                        pipeStream_Send = new IPCStream_Out(pipeClient_cts, this);

                        //IPC 메세지를 받았을 때 이벤트 등록
                        pipeStream_Get.OnMessageReceived += pipeStream_Get_OnMessageReceived;

                        pipeClient_stc.Start(); //데이터 받기 시작
                        pipeClient_cts.Start(); //데이터 보내기 시작

                        //파이프 연결 성공했다고 보냄
                        pipeStream_Send.Send(new MessageWriter(CTSHeader.SEND_PIPE_CONNECT_COMPLETE));

                        //연결되었다고 해줌.
                        isConnected = true;
                    }
                    catch (Exception e) { Console.WriteLine(e.Message); }
                    break;
            }

        }
        //IPC
        void pipeStream_Get_OnMessageReceived(MessageReader reader)
        {
            int sensorID;
            bool isCreated;
            string mappedName;
            NuiSensor sensor = null;

            STCHeader header = (STCHeader)reader.ReadShort();
            switch (header)
            {
                #region Channel Making
                case STCHeader.SEND_COLORFRAME_SM_NAME:
                    sensorID = reader.ReadInt();
                    isCreated = reader.ReadBool();

                    if (connectedSensors.ContainsKey(sensorID) == false) return;

                    sensor = connectedSensors[sensorID];
                    if (!isCreated)//생성실패
                    {
                        sensor.OpenColorFrameCallback(false, null);
                        return;
                    }

                    mappedName = reader.ReadString();
                    //[수정요망] 맨 뒤쪽 channel 지금은 4로 강제해놓음.
                    sensor.OpenColorFrameCallback(true, new ColorChannel(mappedName, sensor.colorInfo.Width, sensor.colorInfo.Height, sensor.colorInfo.BytePerPixel));
                    break;

                case STCHeader.SEND_DEPTHFRAME_SM_NAME:
                    sensorID = reader.ReadInt();
                    isCreated = reader.ReadBool();

                    if (connectedSensors.ContainsKey(sensorID) == false) return;

                    sensor = connectedSensors[sensorID];
                    if (!isCreated)//생성실패
                    {
                        sensor.OpenDepthFrameCallback(false, null);
                        return;
                    }

                    mappedName = reader.ReadString();
                    sensor.OpenDepthFrameCallback(true, new DepthChannel(mappedName, sensor.depthInfo.Width, sensor.depthInfo.Height, sensor.depthInfo.BytePerPixel) );
                    break;

                case STCHeader.SEND_BODY_SM_NAME:
                    sensorID = reader.ReadInt();
                    isCreated = reader.ReadBool();

                    if (connectedSensors.ContainsKey(sensorID) == false) return;

                    sensor = connectedSensors[sensorID];
                    if (!isCreated)//생성실패
                    {
                        sensor.OpenColorFrameCallback(false, null);
                        return;
                    }

                    mappedName = reader.ReadString();
                    sensor.OpenBodyFrameCallback(true, new bodyChannel(mappedName));
                    break;
                #endregion
                #region Sensor Datas
                //모든 센서 정보 보냄(처음에)
                case STCHeader.SEND_ALL_SENSOR_INFO:
                    int sensorCount = reader.ReadInt();
                    for (int i = 0; i < sensorCount; i++)
                        sensor = MakeSensor(reader);
                    break;

                //새로 연결된 센서 정보 보냄
                case STCHeader.SEND_NEW_SENSOR_INFO:
                    sensor = MakeSensor(reader);
                    break;

                //state가 바뀐 센서 정보 보냄(여기서 삭제도 함.
                case STCHeader.SEND_SENSOR_STATE:
                    int id = reader.ReadInt();
                    SensorState state = (SensorState)reader.ReadInt();
                    if (connectedSensors.ContainsKey(id))
                    {
                        sensor = connectedSensors[id];
                        sensor.state = state;
                        if (state == SensorState.UNKNOWN)
                        {
                            sensor.DisconnectedCallback();
                            connectedSensors.Remove(id);

                            if (OnSensorDisconnected != null)
                                Queue_Event(OnSensorDisconnected, sensor);
                        }
                    }
                    
                    break;
                case STCHeader.SEND_TRIGGER_EVENT_DATA:
                    ReceiveTriggerEvent(reader);
                    break;
                #endregion
            }
        }

        private void ReceiveTriggerEvent(MessageReader reader)
        {
            byte[] rawEvent = reader.ReadBytes(EventData.EVENT_DATA_SIZE);
            EventData e = EventData.ToEvent(rawEvent);

            switch (e.EventType)
            {
                case EventType.HandStatusChange:
                    JointType type = (JointType)BitConverter.ToInt32(e.Data, 0);
                    HandStatus status = (HandStatus)BitConverter.ToInt16(e.Data, 4);
                    if (OnHandStatusChanged != null)
                        Queue_Event(OnHandStatusChanged,type, status);
                    break;
                case EventType.FaceDataChange:
                    if (OnFaceDataChanged != null)
                        OnFaceDataChanged(BitConverter.ToInt32(e.Data, 0),
                            BitConverter.ToInt32(e.Data, 4),
                            BitConverter.ToInt32(e.Data, 8),
                            BitConverter.ToInt32(e.Data, 12));
                    break;

            }
        }

        private NuiSensor MakeSensor(MessageReader reader)
        {
            int id = reader.ReadInt();
            string name = reader.ReadString();
            string company = reader.ReadString();
            SensorState state = (SensorState)reader.ReadInt();
            int colorFrameWidth = reader.ReadInt();
            int colorFrameHeight = reader.ReadInt();
            int colorFramebpp = reader.ReadInt();
            int depthFrameWidth = reader.ReadInt();
            int depthfreamHeight = reader.ReadInt();
            int depthFramebpp = reader.ReadInt();
            int maxTrackingBody = reader.ReadInt();


            NuiSensor sensor = new NuiSensor(this, name, company, id, state, colorFrameWidth, colorFrameHeight, colorFramebpp, depthFrameWidth, depthfreamHeight, depthFramebpp, maxTrackingBody);
            connectedSensors.Add(sensor.id, sensor);

            if (OnSensorConnected != null)
                Queue_Event(OnSensorConnected, sensor);

            return sensor;
        }
        #endregion

        #region Open Color, Depth, Body Frame
        public bool OpenColorFrame(NuiSensor sensor)
        {
            if (connectedSensors.ContainsValue(sensor) == false)
                return false;

            //컬러 쉐어드메모리를 생성해달라고 요청합니다.
            MessageWriter sm_message = new MessageWriter(CTSHeader.REQUEST_COLORFRAME_SM_NAME);
            sm_message.WriteInt(sensor.id);
            pipeStream_Send.Send(sm_message);
            return true;
        }
        public bool OpenDepthFrame(NuiSensor sensor)
        {
            if (connectedSensors.ContainsValue(sensor) == false)
                return false;

            //뎁스 쉐어드메모리를 생성해달라고 요청합니다.
            MessageWriter sm_message = new MessageWriter(CTSHeader.REQUEST_DEPTHFRAME_SM_NAME);
            sm_message.WriteInt(sensor.id);
            pipeStream_Send.Send(sm_message);
            return true;
        }
        public bool OpenBodyFrame(NuiSensor sensor)
        {
            if (connectedSensors.ContainsValue(sensor) == false)
                return false;

            //스켈레톤 쉐어드메모리를 생성해달라고 요청합니다.
            MessageWriter sm_message = new MessageWriter(CTSHeader.REQUEST_BODY_SM_NAME);
            sm_message.WriteInt(sensor.id);
            pipeStream_Send.Send(sm_message);
            return true;
        }
        #endregion


        //NUI GATEWAY LIBRARY와의 연결을 종료.
        public void CLOSE()
        {
            if (lifeSocket != null)
                lifeSocket.Close();
            if (pipeClient_cts != null)
                pipeClient_cts.CLOSE();
            if (pipeClient_stc != null)
                pipeClient_stc.CLOSE();
        }

        //유저단 이벤트추가
        public event SensorDelegate OnSensorConnected;
        public event SensorDelegate OnSensorDisconnected;
        public event ConnectedServiceDelegate OnConnectedService;
        public event HandStatusChangeHandler OnHandStatusChanged;
        public event FaceDataChangeHandler OnFaceDataChanged;

        //유저단 함수 추가

        public bool ConnectService()
        {
            //연결 실패했는데 또 연결하려고하면 암것도안함.;
            if (isTryConnectService)
                throw new MessageExeption("'ConnectService()' is Must be run once.");

            isTryConnectService = true;

            try
            {
                //TCP life 소켓에 연결하고 스트림을 생성합니다.
                lifeSocket.Connect(IPAddress.Loopback, 8000);
                lifeStream = new TCPStream(lifeSocket, this);

                //메세지가 왔을 때의 이벤트를 등록합니다.
                lifeStream.OnMessageReceived += lifeStream_OnMessageReceived;

                if (OnConnectedService != null)
                    Queue_Event(OnConnectedService, true);
                return true;
            }
            catch
            {
                if (OnConnectedService != null)
                    Queue_Event(OnConnectedService, false);
                return false;
            }
        }

        public NuiSensor[] GetSensors()
        {
            if (!isConnected)
                throw new NotConnectedExeption();

            NuiSensor[] array = new NuiSensor[connectedSensors.Count];
            connectedSensors.Values.CopyTo(array, 0);
            return array;
        }
    }
}
