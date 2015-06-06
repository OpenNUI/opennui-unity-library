using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.InteropServices;
using System.IO;

namespace OpenNUI.Unity.Library
{
    unsafe class DepthChannel
    {
        public const int BlockCount = 3;
        int zero = 0;

        public readonly int capacity;

        MemoryMappedFile mappedFile = null;
        MemoryMappedViewAccessor mappedFileAccessor = null;
        byte* MappedPointer;
        int*[] lockDatas;

        public DepthChannel(string mappedName, int width, int height, int bpp)
        {
            capacity = width * height * bpp;

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
        public bool Read(out ushort[] data)
        {
            data = new ushort[0];
            short[] shortdata = new short[0];
            for (int i = 0; i < BlockCount; i++)
            {
                if (Interlocked.CompareExchange(ref *lockDatas[i], 1, 0) == 0)
                {
                    int size = 0;
                    mappedFileAccessor.Read<int>(sizeof(int) * BlockCount + (capacity + sizeof(int)) * i, out size);

                    if (size > 0)
                    {
                        data = new ushort[size / 2];
                        shortdata = new short[size / 2];
                        mappedFileAccessor.Write<int>(sizeof(int) * BlockCount + (capacity + sizeof(int)) * i, ref zero);

                        Marshal.Copy((IntPtr)(MappedPointer + sizeof(int) * BlockCount + (capacity + sizeof(int)) * i + sizeof(int)), shortdata, 0, capacity / 2);

                        System.Buffer.BlockCopy(shortdata, 0, data, 0, (size / 2) * 2);

                    }
                    Interlocked.Exchange(ref *lockDatas[i], 0);
                    break;
                }
            }
            return data.Length > 0;
        }
    }
}
