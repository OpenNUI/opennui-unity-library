using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.InteropServices;
using System.IO;

namespace OpenNUI.Unity.Library
{
    public unsafe class DepthChannel
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

        private const int BlockCount = 3;

        private MemoryMappedFile _mappedFile = null;
        private MemoryMappedViewAccessor _mappedFileAccessor = null;
        private byte* _mappedPointer;
        private int*[] lockDatas;
        private NuiSensor _sensor;
        private long _stamp;
        public DepthChannel(string mappedName, NuiSensor sensor)
        {
            _sensor = sensor;
            _mappedFile = MemoryMappedFile.OpenExisting(mappedName);
            _mappedFileAccessor = _mappedFile.CreateViewAccessor();
            _mappedFileAccessor.SafeMemoryMappedViewHandle_AcquirePointer(ref _mappedPointer);

            lockDatas = new int*[BlockCount];
            for (int i = 0; i < BlockCount; i++)
                lockDatas[i] = (int*)(sizeof(int) * i + _mappedPointer);
        }
        public void Close()
        {
            _mappedFile.Dispose();
        }
        internal bool Read(out ushort[] data)
        {
            bool result = false;
            data = new ushort[_sensor.DepthInfo.Size / sizeof(short)];
            for (int i = 0; i < BlockCount; i++)
            {
                if (Interlocked.CompareExchange(ref *lockDatas[i], 1, 0) != 0)
                    continue;

                long stamp = *((long*)(_mappedPointer + sizeof(int) * BlockCount + (_sensor.DepthInfo.Size + sizeof(long)) * i));
                if (_stamp < stamp)
                {
                    fixed (ushort* dest = &data[0])
                    {
                        CopyMemory(dest, _mappedPointer + sizeof(int) * BlockCount + (_sensor.DepthInfo.Size + sizeof(long)) * i + sizeof(long),
                             (ulong)_sensor.DepthInfo.Size
                            );
                    }
                    _stamp = stamp;
                    result = true;
                }
                Interlocked.Exchange(ref *lockDatas[i], 0);
                return result;
            }
            return result;

        }
    }
}