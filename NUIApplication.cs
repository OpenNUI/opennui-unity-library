using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace OpenNUI.Unity.Library
{

    //Delegats
    public delegate void SensorDelegate(NuiSensor sensor);
    public delegate void ConnectedServiceDelegate(bool success);

    public delegate void HandStatusChangeHandler(JointType type, HandStatus status);
    public delegate void FaceDataChangeHandler(int x, int y, int width, int height);

    //유니티 단에 노출될 Static Class(시스템)
    public static class NUISystem
    {
        internal static NUIApp nuiApp = null;

        public static void __INIT(string appName, Queue<CWORK_STRUCT> Cwork_Queue, object _queueLock)
        {
            if (nuiApp != null)
                return;

            nuiApp = new NUIApp(appName, ref Cwork_Queue, _queueLock);
        }

        public static void __CWORK_WORK(CWORK_STRUCT iars)
        {
            if (nuiApp == null)
                return;

            nuiApp.IAR_WORK(iars);
        }

        public static void __CLOSE()
        {
            nuiApp.CLOSE();
        }
    }

    public static class NUIApplication
    {
        public const int JointCount = 25;

        public static event SensorDelegate OnSensorConnected
        {
            add
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null) NUISystem.nuiApp.OnSensorConnected += value; }
            }

            remove
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null)  NUISystem.nuiApp.OnSensorConnected -= value; }
            }
        }
        public static event SensorDelegate OnSensorDisconnected
        {
            add
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null) NUISystem.nuiApp.OnSensorDisconnected += value; }
            }

            remove
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null)  NUISystem.nuiApp.OnSensorDisconnected -= value; }
            }
        }
        public static event ConnectedServiceDelegate OnConnectedService
        {
            add
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null) NUISystem.nuiApp.OnConnectedService += value; }
            }

            remove
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null)  NUISystem.nuiApp.OnConnectedService -= value; }
            }
        }
        public static event HandStatusChangeHandler OnHandStatusChanged
        {
            add
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null) NUISystem.nuiApp.OnHandStatusChanged += value; }
            }

            remove
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null)  NUISystem.nuiApp.OnHandStatusChanged -= value; }
            }
        }
        public static event FaceDataChangeHandler OnFaceDataChanged
        {
            add
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null) NUISystem.nuiApp.OnFaceDataChanged += value; }
            }

            remove
            {
                lock (NUISystem.nuiApp) { if (NUISystem.nuiApp != null)  NUISystem.nuiApp.OnFaceDataChanged -= value; }
            }
        }

        public static string AppName
        {
            get
            {
                if (NUISystem.nuiApp == null)
                    return "";

                return NUISystem.nuiApp.appName;
            }
        }
        public static bool IsConnected
        {
            get
            {
                if (NUISystem.nuiApp == null)
                    return false;
                return NUISystem.nuiApp.IsConnected;
            }
        }
        private static bool isEnabled
        {
            get
            {
                return (NUISystem.nuiApp != null && NUISystem.nuiApp.IsConnected != false);
            }
        }

        public static bool isInited
        {
            get {return NUISystem.nuiApp != null;}
        }
        public static bool isTryConnectService
        {
            get
            {
                if (NUISystem.nuiApp == null)
                    return false;
                return NUISystem.nuiApp.isTryConnectService;
            }
        }

        public static bool ConnectService()
        {
            if (NUISystem.nuiApp == null)
                return false;

            return NUISystem.nuiApp.ConnectService();
        }

        public static NuiSensor[] GetSensors()
        {
            if (isEnabled == false)
                return null;

            return NUISystem.nuiApp.GetSensors();
        }
    }
}
