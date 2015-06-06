using System;
using System.Runtime.InteropServices;

namespace NUIGatewayLibrary_Unity
{
    class MemoryMappedFile : IDisposable
    {
        private IntPtr basemap = IntPtr.Zero;
        private MemoryMappedFileRights rights;

        public static MemoryMappedFile OpenExisting(string mappedName, MemoryMappedFileRights desiredAccessRights = MemoryMappedFileRights.ReadWrite)
        {
            MemoryMappedFile mappedFile = new MemoryMappedFile();
            //mappedFile.map = Win32APIs.OpenFileMapping(desiredAccessRights, false, @"Global\" + mappedName);
            mappedFile.rights = desiredAccessRights;
            mappedFile.basemap = Win32APIs.OpenFileMapping(mappedFile.rights, false, mappedName);

            if (mappedFile.basemap == IntPtr.Zero) //생성 실패
                return null;

            return mappedFile;
        }

        public MemoryMappedViewAccessor CreateViewAccessor()
        {
            if (basemap == IntPtr.Zero)
                return null;

            MemoryMappedViewAccessor viewAccessor = new MemoryMappedViewAccessor(basemap);
            return viewAccessor;
        }

        public void Dispose()
        {
            if (basemap != IntPtr.Zero)
                Win32APIs.UnmapViewOfFile(basemap);
        }
        protected virtual void Dispose(bool disposing)
        {
            Dispose();
        }

    }

    /// <summary>
    /// 메모리 매핑된 파일의 임의 액세스 뷰를 나타냅니다.
    /// </summary>
    unsafe class MemoryMappedViewAccessor : IDisposable
    {
        private IntPtr baseAccessor = IntPtr.Zero;
        private IntPtr baseMap = IntPtr.Zero;
        public MemoryMappedViewAccessor(IntPtr baseMap)
        {
            this.baseMap = baseMap;
            baseAccessor = Win32APIs.MapViewOfFile(this.baseMap, MemoryMappedFileRights.ReadWrite, 0, 0, 0);

            //int* a = (int*)Win32APIs.MapViewOfFile(this.baseMap, MemoryMappedFileRights.Read, 0, 0, 0);
        }

        public void SafeMemoryMappedViewHandle_AcquirePointer(ref byte* MappedPointer)
        {
            MappedPointer = (byte*)baseAccessor;
        }

        public void Write<T>(long position, ref T structure) where T : struct
        {
            IntPtr structurePtr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            Marshal.StructureToPtr(structure, structurePtr, false);
            Win32APIs.memcpy(new IntPtr(baseAccessor.ToInt64() + position), structurePtr, (int)Marshal.SizeOf(default(T)));
            Marshal.FreeHGlobal(structurePtr);
        }

        public void Read<T>(long position, out T structure) where T : struct
        {
            IntPtr dest = Marshal.AllocHGlobal(Marshal.SizeOf(default(T)));
            Win32APIs.memcpy(dest, new IntPtr(baseAccessor.ToInt64() + position), (int)Marshal.SizeOf(default(T)));
            structure = (T)Marshal.PtrToStructure(dest, typeof(T));
        }

        public void WriteInt(long position, ref int structure)
        {
            *(int*)(new IntPtr(baseAccessor.ToInt64() + position)) = structure;
        }

        public void ReadInt(long position, out int structure)
        {
            structure = *(int*)(new IntPtr(baseAccessor.ToInt64() + position));
        }


        public void Dispose()
        {
            if (baseAccessor != IntPtr.Zero)
                Win32APIs.CloseHandle(baseAccessor);
        }
        protected virtual void Dispose(bool disposing)
        {
            Dispose();
        }

    }

    enum FileProtection : uint      // constants from winnt.h
    {
        ReadOnly = 2,
        ReadWrite = 4
    }

    // 요약:
    //     디스크의 파일에 연결되지 않은 메모리 매핑된 파일에 대한 액세스 권한을 지정합니다.
    enum MemoryMappedFileRights
    {
        // 요약:
        //     다른 프로세스에 쓰기 작업이 표시되지 않도록 하는 제한을 사용하여 파일을 읽고 쓸 수 있는 권한입니다.
        CopyOnWrite = 1,
        //
        // 요약:
        //     파일에 데이터를 추가하거나 파일에서 데이터를 제거할 수 있는 권한입니다.
        Write = 2,
        //
        // 요약:
        //     파일을 읽기 전용으로 열고 복사할 수 있는 권한입니다.
        Read = 4,
        //
        // 요약:
        //     파일을 열고 복사할 수 있는 권한 및 파일에 데이터를 추가하거나 파일에서 데이터를 제거할 수 있는 권한입니다.
        ReadWrite = 6,
        //
        // 요약:
        //     응용 프로그램 파일을 실행할 수 있는 권한입니다.
        Execute = 8,
        //
        // 요약:
        //     폴더나 파일을 읽기 전용으로 열고 복사하며, 응용 프로그램 파일을 실행할 수 있는 권한입니다. 이 권한에는 System.IO.MemoryMappedFiles.MemoryMappedFileRights.Read
        //     및 System.IO.MemoryMappedFiles.MemoryMappedFileRights.Execute 권한이 포함됩니다.
        ReadExecute = 12,
        //
        // 요약:
        //     파일을 열고 복사할 수 있는 권한, 파일에 데이터를 추가하거나 파일에서 데이터를 제거할 수 있는 권한 및 응용 프로그램 파일을 실행할
        //     수 있는 권한입니다.
        ReadWriteExecute = 14,
        //
        // 요약:
        //     파일을 삭제할 수 있는 권한입니다.
        Delete = 65536,
        //
        // 요약:
        //     파일에서 액세스 및 감사 규칙을 열고 복사할 수 있는 권한입니다. 여기에는 데이터, 파일 시스템 특성 또는 확장된 파일 시스템 특성을
        //     읽을 수 있는 권한이 포함되지 않습니다.
        ReadPermissions = 131072,
        //
        // 요약:
        //     파일에 연결된 보안 및 감사 규칙을 변경할 수 있는 권한입니다.
        ChangePermissions = 262144,
        //
        // 요약:
        //     파일의 소유자를 변경할 수 있는 권한입니다.
        TakeOwnership = 524288,
        //
        // 요약:
        //     파일에 대한 모든 권한을 실행하고 액세스 제어 및 감사 규칙을 수정할 수 있는 권한입니다. 이 값은 파일에 대해 모든 작업을 할 수
        //     있는 권한을 나타내며 이 열거형의 모든 권한을 조합한 것입니다.
        FullControl = 983055,
        //
        // 요약:
        //     파일의 사용 권한을 가져오거나 설정할 수 있는 권한입니다.
        AccessSystemSecurity = 16777216,
    }

}
