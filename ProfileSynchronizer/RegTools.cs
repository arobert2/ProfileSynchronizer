using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ProfileSynchronizer
{
    public class RegTools
    {
        /************************
         * Win API structs
         */
        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID pluid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TokPriv1Luid
        {
            public int Count;
            public LUID luid;
            public UInt32 Attr;
        }
        /***********************/

        /************************
         * Registry Access Settings
         */
        private const Int32 ANYSIDE_ARRAY = 1;
        private const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
        private const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const UInt32 TOKEN_QUERY = 0x0008;

        private const UInt32 HKEY_LOCAL_MACHINE = 0x80000002;
        private const string SE_RESTORE_NAME = "SeRestorePrivilege";
        private const string SE_BACKUP_NAME = "SeBackupPrivilege";
        /***********************/

        /************************
         * Process and Privilege
         */
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LookupPrivilegeValue(string systemName, string name, out LUID lpLuid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr htok, bool disableAllPrivileges, ref TokPriv1Luid newState, int len, IntPtr preve, IntPtr relen);
        /***********************/

        /************************
         * Hive load and unload
         */
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegLoadKey(UInt32 hKey, string subKey, string path);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int RegUnLoadKey(UInt32 hKey, string subKey);
        /***********************/

        private IntPtr _myToken;
        private TokPriv1Luid _resPriv = new TokPriv1Luid();
        private TokPriv1Luid _backPriv = new TokPriv1Luid();

        private LUID _restoreLuid;
        private LUID _backupLuid;

        public RegTools()
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out _myToken))
            {
                System.Diagnostics.Debug.WriteLine("Cannot Open Process");
                Application.Current.Shutdown();
            }
            if (!LookupPrivilegeValue(null, SE_RESTORE_NAME, out _restoreLuid))
            {
                System.Diagnostics.Debug.WriteLine("Cannot lookup privilege value");
                Application.Current.Shutdown();
            }
            if (!LookupPrivilegeValue(null, SE_BACKUP_NAME, out _backupLuid))
            {
                System.Diagnostics.Debug.WriteLine("Cannot lookup privilege value");
                Application.Current.Shutdown();
            }

            _resPriv.Attr = SE_PRIVILEGE_ENABLED;
            _resPriv.luid = _restoreLuid;
            _resPriv.Count = 1;

            _backPriv.Attr = SE_PRIVILEGE_ENABLED;
            _backPriv.luid = _backupLuid;
            _backPriv.Count = 1;

            if (!AdjustTokenPrivileges(_myToken, false, ref _resPriv, 0, IntPtr.Zero, IntPtr.Zero))
            {
                System.Diagnostics.Debug.WriteLine("Error adjusting privileges: " + Marshal.GetLastWin32Error());
                Application.Current.Shutdown();
            }
            if (!AdjustTokenPrivileges(_myToken, false, ref _backPriv, 0, IntPtr.Zero, IntPtr.Zero))
            {
                System.Diagnostics.Debug.WriteLine("Error adjusting privileges: " + Marshal.GetLastWin32Error());
                Application.Current.Shutdown();
            }
        }

        public void LoadDefaultHive()
        {
            int result = RegLoadKey(HKEY_LOCAL_MACHINE, ConfigurationManager.AppSettings["RegistryLoadName"], @"C:\users\default\ntuser.dat");
            if(result != 0)
            {
                System.Diagnostics.Debug.WriteLine("Unable to load hive: " + result);
                Application.Current.Shutdown();
            }
        }

        public void UnLoadDefaultHive()
        {
            RegUnLoadKey(HKEY_LOCAL_MACHINE, ConfigurationManager.AppSettings["RegistryLoadName"]);
        }

        /// <summary>
        /// Get children keys of registry key.
        /// </summary>
        /// <param name="key">Key path</param>
        /// <returns>Names of all children keys.</returns>
        public string[] GetChildKeys(string key)
        {
            string k = key.Remove(0, key.IndexOf(@"\") + 1);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(k);
            return rk.GetSubKeyNames();
        }
    }
}
