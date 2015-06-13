using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.InteropServices;
using System.IO;

namespace OpenNUI.Unity.Library
{
    unsafe class bodyChannel
    {
        public const int BlockCount = 3;
        int zero = 0;

        public const int Capacity = 2048 * 6;

        MemoryMappedFile mappedFile = null;
        MemoryMappedViewAccessor mappedFileAccessor = null;
        byte* MappedPointer;
        int*[] lockDatas;

        public bodyChannel(string mappedName)
        {
            int l = mappedName.Length;
            mappedFile = MemoryMappedFile.OpenExisting(mappedName);
            mappedFileAccessor = mappedFile.CreateViewAccessor();
            mappedFileAccessor.SafeMemoryMappedViewHandle_AcquirePointer(ref MappedPointer);

            lockDatas = new int*[BlockCount];
            for (int i = 0; i < BlockCount; i++)
            {
                lockDatas[i] = (int*)(sizeof(int) * i + MappedPointer);
            }

        }
        public bool Read(out byte[] data)
        {
            data = new byte[0];
            for (int i = 0; i < BlockCount; i++)
            {
                if (Interlocked.CompareExchange(ref *lockDatas[i], 1, 0) == 0)
                {
                    int size = 0;
                    mappedFileAccessor.Read<int>(sizeof(int) * BlockCount + (Capacity + sizeof(int)) * i, out size);

                    if (size > 0)
                    {
                        data = new byte[size];
                        mappedFileAccessor.Write<int>(sizeof(int) * BlockCount + (Capacity + sizeof(int)) * i, ref zero);

                        Marshal.Copy((IntPtr)(MappedPointer + sizeof(int) * BlockCount + (Capacity + sizeof(int)) * i + sizeof(int)), data, 0, Capacity);
                    }
                    Interlocked.Exchange(ref *lockDatas[i], 0);
                    break;
                }
            }
            return data.Length > 0;
        }
    }
}
