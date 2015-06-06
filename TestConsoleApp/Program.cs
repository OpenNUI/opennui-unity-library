using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using NUIGatewayLibrary_Unity;

namespace TestConsoleApp
{
    public class NUIUnitySystem
    {
        public readonly string AppName;
        private static Queue<CWORK_STRUCT> IAR_Queue = new Queue<CWORK_STRUCT>();
        private static object iar_queueLock = new object();

        public NUIUnitySystem(string AppName)
        {
            this.AppName = AppName;
            NUISystem.__INIT(AppName, IAR_Queue, iar_queueLock);
        }

        public void Update()
        {
            lock (iar_queueLock)
            {
                while (IAR_Queue.Count > 0)
                    NUISystem.__CWORK_WORK(IAR_Queue.Dequeue());
            }
        }
        public void OnApplicationQuit()
        {
            NUISystem.__CLOSE();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            NUIUnitySystem unity = new NUIUnitySystem("TestConsoleApp");

            NUIApplication.OnConnectedService += NUIApplication_OnConnectedService;
            NUIApplication.OnSensorConnected += NUIApplication_OnSensorConnected;
            NUIApplication.OnSensorDisconnected += NUIApplication_OnSensorDisconnected;

            NUIApplication.ConnectService();

            while (true)
            {
                unity.Update();
                Thread.Sleep(0);
                
            }

            unity.OnApplicationQuit();
        }

        static void NUIApplication_OnConnectedService(bool success)
        {
            if (success)
                Console.WriteLine("success to connect service");
            else
                Console.WriteLine("failed to connect service");
        }

        static void NUIApplication_OnSensorDisconnected(NuiSensor sensor)
        {
            Console.WriteLine("Disconnected : " + sensor.name + "(" + sensor.id + ")");
        }

        static void NUIApplication_OnSensorConnected(NuiSensor sensor)
        {
            Console.WriteLine("Connected : " + sensor.name + "(" + sensor.id + ")");

            sensor.OpenSkeletonFrame();
            sensor.OpenColorFrame();
            sensor.OpenDepthFrame();

            new Thread(() =>
            {
                int a = -1;
                while (true)
                {
                    BodyData[] d = sensor.GetBodyData();
                    if (d != null)
                    {


                    }
                }

            }).Start();
        }
    }
}
