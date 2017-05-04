using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RegistryTools
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
                System.Diagnostics.Debug.WriteLine("Cannot Open Process");
            
            if (!LookupPrivilegeValue(null, SE_RESTORE_NAME, out _restoreLuid))
                System.Diagnostics.Debug.WriteLine("Cannot lookup privilege value");
            
            if (!LookupPrivilegeValue(null, SE_BACKUP_NAME, out _backupLuid))
                System.Diagnostics.Debug.WriteLine("Cannot lookup privilege value");

            _resPriv.Attr = SE_PRIVILEGE_ENABLED;
            _resPriv.luid = _restoreLuid;
            _resPriv.Count = 1;

            _backPriv.Attr = SE_PRIVILEGE_ENABLED;
            _backPriv.luid = _backupLuid;
            _backPriv.Count = 1;

            if (!AdjustTokenPrivileges(_myToken, false, ref _resPriv, 0, IntPtr.Zero, IntPtr.Zero))
                System.Diagnostics.Debug.WriteLine("Error adjusting privileges: " + Marshal.GetLastWin32Error());

            if (!AdjustTokenPrivileges(_myToken, false, ref _backPriv, 0, IntPtr.Zero, IntPtr.Zero))
                System.Diagnostics.Debug.WriteLine("Error adjusting privileges: " + Marshal.GetLastWin32Error());
        }
        /// <summary>
        /// Load the default user hive.
        /// </summary>
        public void LoadDefaultHive()
        {
            int result = RegLoadKey(HKEY_LOCAL_MACHINE, ConfigurationManager.AppSettings["RegistryLoadName"], @"C:\users\default\ntuser.dat");
            if(result != 0)
                System.Diagnostics.Debug.WriteLine("Unable to load hive: " + result);
        }
        /// <summary>
        /// Unload the default user hive.
        /// </summary>
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
            string k = CutRoot(key);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(k);
            return rk.GetSubKeyNames();
        }
        /// <summary>
        /// Get Registry Key values
        /// </summary>
        /// <param name="key">Complete key path.</param>
        /// <returns>An array of registry values.</returns>
        public RegValueData[] GetKeyValues(string key)
        {
            string k = CutRoot(key);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(k);
            string[] valuenames = rk.GetValueNames();
            List<RegValueData> ValueData = new List<RegValueData>();
            foreach (string s in valuenames)
            {
                ValueData.Add(new RegValueData() { Name = s, RegType = rk.GetValueKind(s).ToString(), Value = rk.GetValue(s).ToString() });
            }
            return ValueData.ToArray();
        }
        /// <summary>
        /// Removes the root path since it is known by the Registry class.
        /// </summary>
        /// <param name="keypath">Complete registry key path.</param>
        /// <returns>registry key path without the root of the path.</returns>
        private string CutRoot(string keypath)
        {
            return keypath.Substring(keypath.IndexOf(@"\") + 1);
        }
        /// <summary>
        /// Adds or modifies a registry key value
        /// </summary>
        /// <param name="path">Path to key.</param>
        /// <param name="rvd">Registry value to add or modify.</param>
        public void AddModifyKey(string path, RegValueData rvd)
        {
            string k = CutRoot(path);
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(k);
            rk.SetValue(rvd.Name, rvd.Value);
        }
    }
}
