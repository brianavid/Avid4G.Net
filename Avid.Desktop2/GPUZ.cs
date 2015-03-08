using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace GPUZ
{
    /// <summary>
    /// Wrapper for the GPUZ shared memory interface to query the state (in particular temperature) of the GPU
    /// </summary>
    class GPUZ
    {
        const String SHMEM_NAME = "GPUZShMem";
        const int MAX_RECORDS = 128;

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct GPUZ_RECORD
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string key;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string value;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct GPUZ_SENSOR_RECORD
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string unit;

            public UInt32 digits;
            public double value;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class GPUZ_SH_MEM
        {
            public UInt32 version; 	 // Version number, 1 for the struct here
            public Int32 busy;	 // Is data being accessed?
            public UInt32 lastUpdate; // GetTickCount() of last update
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_RECORDS)]
            public GPUZ_RECORD[] data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_RECORDS)]
            public GPUZ_SENSOR_RECORD[] sensors;

            public GPUZ_SH_MEM()
            {
                data = new GPUZ_RECORD[MAX_RECORDS];
                sensors = new GPUZ_SENSOR_RECORD[MAX_RECORDS];
            }

        };

        #region Win32 API stuff
        public const int FILE_MAP_READ = 0x0004;

        [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr OpenFileMapping(int dwDesiredAccess,
            bool bInheritHandle, StringBuilder lpName);

        [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr MapViewOfFile(IntPtr hFileMapping,
            int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow,
            int dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll")]
        internal static extern bool UnmapViewOfFile(IntPtr map);

        [DllImport("kernel32.dll")]
        internal static extern bool CloseHandle(IntPtr hObject);
        #endregion

        private bool fileOpen = false;
        private IntPtr map;
        private IntPtr handle;
        GPUZ_SH_MEM data;

        public GPUZ()
        {
            data = new GPUZ_SH_MEM();
        }

        ~GPUZ()
        {
            CloseView();
        }

        public bool OpenView()
        {
            if (!fileOpen)
            {
                try
                {
                    StringBuilder sharedMemFile = new StringBuilder(SHMEM_NAME);
                    handle = OpenFileMapping(FILE_MAP_READ, false, sharedMemFile);
                    if (handle == IntPtr.Zero)
                    {
                        throw new Exception("Unable to open file mapping.");
                    }
                    map = MapViewOfFile(handle, FILE_MAP_READ, 0, 0, Marshal.SizeOf((Type)typeof(GPUZ_SH_MEM)));
                    if (map == IntPtr.Zero)
                    {
                        throw new Exception("Unable to read shared memory.");
                    }
                    fileOpen = true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return fileOpen;
        }

        public void CloseView()
        {
            if (fileOpen)
            {
                try
                {
                    fileOpen = false;
                    UnmapViewOfFile(map);
                    CloseHandle(handle);
                }
                catch (Exception)
                {
                }
            }
        }

        public GPUZ_SH_MEM GetData()
        {
            if (fileOpen)
            {
                data = (GPUZ_SH_MEM)Marshal.PtrToStructure(map, typeof(GPUZ_SH_MEM));
            }

            return data;
        }
    }
}
