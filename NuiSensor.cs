using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace OpenNUI.Unity.Library
{
    //한 NUI 센서에 대한 Class
    public class NuiSensor
    {
        private NUIApp nuiApp;

        public readonly string name;
        public readonly string company;
        public readonly int id;

        private SensorState _state;
        internal SensorState state
        {
            get { return _state; }
            set
            {
                _state = value;
                if (OnStatusChanged != null)
                    nuiApp.Queue_Event(OnStatusChanged, this, _state);
            }
        }
        public SensorState State
        {
            get { return _state; }
        }
        private ColorChannel colorChannel = null;
        private DepthChannel depthChannel = null;
        private bodyChannel bodyChannel = null;

        private bool colorFrameAuthority = false;
        private bool depthFrameAuthority = false;
        private bool bodyFrameAuthority = false;
        public bool ColorFrameAuthority
        {
            get { return colorFrameAuthority; }
        }
        public bool DepthFrameAuthority
        {
            get { return depthFrameAuthority; }
        }
        public bool BodyFrameAuthority
        {
            get { return bodyFrameAuthority; }
        }

        private bool colorFrameReady = false;
        private bool depthFrameReady = false;
        private bool bodyFrameReady = false;

        private ImageData lastColorFrame = null;
        private DepthData lastDepthFrame = null;
        private BodyData[] lastBodyFrame = null;

        public ColorInfo colorInfo;
        public DepthInfo depthInfo;

        internal NuiSensor(NUIApp nuiApp, string name, string company, int id, SensorState state,
                        int colorFrameWidth, int colorFrameHeight, int colorbpp, int depthFrameWidth, int depthFrameHeight, int depthbpp, int maxtrackingbody)
        {
            this.nuiApp = nuiApp;

            this.name = name;
            this.company = company;
            this.id = id;
            this._state = state;


            bool EnableCoordinate = false;

            double JointDepthXMult = 0;
            double JointDepthXFix = 0;
            double JointDepthYMult = 0;
            double JointDepthYFix = 0;
            double DepthToJointZMult = 0;

            //나중엔 uID 로 처리하던가 해야함.
            switch(name)
            {
                case "Kinect":
                    EnableCoordinate = true;

                    JointDepthXMult = 1.85;
                    JointDepthXFix = 0;
                    JointDepthYMult = 1.9;
                    JointDepthYFix = 0.13;
                    DepthToJointZMult = 0.00123;
                    break;

                case "Kinect2":
                    EnableCoordinate = true;

                    JointDepthXMult = 1.5;
                    JointDepthXFix = 0;
                    JointDepthYMult = 1.45;
                    JointDepthYFix = 0.085;
                    DepthToJointZMult = 0.00107;
                    break;
            }

            colorInfo = new ColorInfo(colorFrameWidth, colorFrameHeight, colorbpp);

            if (EnableCoordinate)
                depthInfo = new DepthInfo(depthFrameWidth, depthFrameHeight, short.MinValue, short.MaxValue, depthbpp,
                    EnableCoordinate,
                    JointDepthXMult, JointDepthXFix, JointDepthYMult, JointDepthYFix,
                    DepthToJointZMult);
            else
                depthInfo = new DepthInfo(depthFrameWidth, depthFrameHeight, short.MinValue, short.MaxValue, depthbpp);
        }

        public delegate void OpenFrameDelegate(NuiSensor sensor, bool success);
        public event OpenFrameDelegate OnColorFrameOpenComplete;
        public event OpenFrameDelegate OnDepthFrameOpenComplete;
        public event OpenFrameDelegate OnBodyFrameOpenComplete;

        public delegate void StatusChangedDelegate(NuiSensor sensor, SensorState state);
        public event StatusChangedDelegate OnStatusChanged;

        public delegate void DisconnectedDelegate(NuiSensor sensor);
        public event DisconnectedDelegate OnDisconnected;

        internal void DisconnectedCallback()
        {
            if (OnDisconnected != null)
            {
                nuiApp.Queue_Event(OnDisconnected, this);
                Clear();
            }
        }
        private void Clear()
        {
            OnColorFrameOpenComplete = null;
            OnDepthFrameOpenComplete = null;
            OnBodyFrameOpenComplete = null;
            OnStatusChanged = null;
            OnDisconnected = null;
        }

        #region Open Frames
        //Color, Depth, Body SM을 만듬.
        //만들어달라고 요청하는 것임, 실제로 만들어지기까지는 딜레이가 있음.
        public bool OpenColorFrame()
        {
            if (colorFrameReady)
                return false;

            return colorFrameAuthority = nuiApp.OpenColorFrame(this);
        }
        public bool OpenDepthFrame()
        {
            if (colorFrameReady)
                return false;

            return depthFrameAuthority = nuiApp.OpenDepthFrame(this);
        }
        public bool OpenBodyFrame()
        {
            if (colorFrameReady)
                return false;

            return bodyFrameAuthority = nuiApp.OpenBodyFrame(this);
        }
        #endregion

        #region OpenFrameCallback
        internal void OpenColorFrameCallback(bool success, ColorChannel channel)
        {
            colorFrameReady = success;
            colorChannel = channel;

            if (OnColorFrameOpenComplete != null)
                nuiApp.Queue_Event(OnColorFrameOpenComplete, this, success);
        }
        internal void OpenDepthFrameCallback(bool success, DepthChannel channel)
        {
            depthFrameReady = success;
            depthChannel = channel;

            if (OnDepthFrameOpenComplete != null)
                nuiApp.Queue_Event(OnDepthFrameOpenComplete, this, success);
        }
        internal void OpenBodyFrameCallback(bool success, bodyChannel channel)
        {
            bodyFrameReady = success;
            bodyChannel = channel;

            if (OnBodyFrameOpenComplete != null)
                nuiApp.Queue_Event(OnBodyFrameOpenComplete, this, success);
        }
        #endregion

        #region Get Frames
        public ImageData GetColorFrame()
        {
            if (!colorFrameAuthority)
                throw new AuthorityException(AuthorityException.AuthorityTypes.Color, this);
            if (!colorFrameReady)
                return null;

            byte[] bits = null;
            if (colorChannel.Read(out bits) == false) // 쉐어드메모리 읽기 실패
                return lastColorFrame;

            lastColorFrame = new ImageData(bits, colorInfo);
            return lastColorFrame;
        }
        public DepthData GetDepthFrame()
        {
            if (!depthFrameAuthority)
                throw new AuthorityException(AuthorityException.AuthorityTypes.Depth, this);
            if (!depthFrameReady)
                return null;

            ushort[] bits = null;
            if (depthChannel.Read(out bits) == false) // 쉐어드메모리 읽기 실패
                return lastDepthFrame;

            //[수정요망] Min,Max depthValue를 센서에서 받아온 데이터로 바꿔야함.
            lastDepthFrame = new DepthData(bits, depthInfo);
            return lastDepthFrame;
        }
        //private static int alo = -1;
        public BodyData[] GetBodyData()
        {
            if (!bodyFrameAuthority)
                throw new AuthorityException(AuthorityException.AuthorityTypes.Body, this);
            if (!bodyFrameReady)
                return null;

            byte[] buffer = null;
            if (bodyChannel.Read(out buffer) == false) // 쉐어드메모리 읽기 실패
                return lastBodyFrame;

            MemoryStream stream = new MemoryStream(buffer);
            BinaryReader reader = new BinaryReader(stream);
            int bodiesCount = reader.ReadInt32();

            /*
                if (alo != bodiesCount)
                {
                    alo = bodiesCount;
                    Console.WriteLine("YAY : " + alo.ToString());
                }
            */

            BodyData[] result = new BodyData[bodiesCount];
            for (int i = 0; i < bodiesCount; i++)
            {
                Dictionary<JointType, NuiJoint> joints = new Dictionary<JointType, NuiJoint>();
                Dictionary<JointType, NuiJointOrientation> orientations = new Dictionary<JointType, NuiJointOrientation>();

                int bodyId = reader.ReadInt32();
                bool valid = reader.ReadBoolean();
                int jointCount = reader.ReadInt32();
                for (int j = 0; j < jointCount; j++)
                {
                    JointType type = (JointType)reader.ReadInt32();
                    TrackingState state = (TrackingState)reader.ReadInt32();
                    double x = reader.ReadDouble();
                    double y = reader.ReadDouble();
                    double z = reader.ReadDouble();

                    float ox = reader.ReadSingle();
                    float oy = reader.ReadSingle();
                    float oz = reader.ReadSingle();
                    float ow = reader.ReadSingle();

                    NuiJoint joint = new NuiJoint(x, y, z, type, state);
                    NuiJointOrientation orientation = new NuiJointOrientation(type, ox, oy, oz, ow);
                    if (!joints.ContainsKey(type))
                        joints.Add(type, joint);
                    if (!orientations.ContainsKey(type))
                        orientations.Add(type, orientation);
                }
                HandStatus leftHand = (HandStatus)reader.ReadInt16();
                HandStatus rightHand = (HandStatus)reader.ReadInt16();
                result[i] = new BodyData(joints, orientations, valid, bodyId, this, leftHand, rightHand);
                Console.WriteLine(sizeof(int) + i * 2048);
            
                stream.Seek(sizeof(int) + (i + 1) * 2048, SeekOrigin.Begin);
            }
            reader.Close();
            stream.Close();

            lastBodyFrame = result;

            return result;
        }
        #endregion

    }

}
