using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.InteropServices;
using System.IO;

namespace OpenNUI.Unity.Library
{
    public unsafe class BodyChannel
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static unsafe extern void* CopyMemory(void* dest, void* src, ulong count);

        private int BlockCount = 3;
        private MemoryMappedFile _mappedFile = null;
        private MemoryMappedViewAccessor mappedFileAccessor = null;
        private byte* _mappedPointer;
        private int*[] lockDatas;
        private NuiSensor _sensor;
        private long _stamp;
        public BodyChannel(string mappedName, NuiSensor sensor)
        {
            _sensor = sensor;
            _mappedFile = MemoryMappedFile.OpenExisting(mappedName);
            mappedFileAccessor = _mappedFile.CreateViewAccessor();
            mappedFileAccessor.SafeMemoryMappedViewHandle_AcquirePointer(ref _mappedPointer);

            lockDatas = new int*[BlockCount];
            for (int i = 0; i < BlockCount; i++)
                lockDatas[i] = (int*)(sizeof(int) * i + _mappedPointer);

        }

        public void Close()
        {
            _mappedFile.Dispose();
        }
        public bool Read(out byte[] data)
        {
            bool result = false;
            data = new byte[8196 * 2];
            for (int i = 0; i < BlockCount; i++)
            {
                if (Interlocked.CompareExchange(ref *lockDatas[i], 1, 0) != 0)
                    continue;

                long stamp = *((long*)(_mappedPointer + sizeof(int) * BlockCount + (data.Length + sizeof(long)) * i));
                if (_stamp < stamp)
                {
                    fixed (byte* dest = &data[0])
                    {
                        CopyMemory(dest, _mappedPointer + sizeof(int) * BlockCount + (data.Length + sizeof(long)) * i + sizeof(long),
                             (ulong)data.Length
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