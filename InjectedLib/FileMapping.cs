using System;
using System.IO;

namespace InjectedLib
{
    [Serializable]
    public class FileMapping
    {
        private readonly NativeMethods.Win32FindData _findData;

        public FileMapping(string actualPath, NativeMethods.Win32FindData findData)
        {
            PuppetPath = ActualPath = actualPath;
            _findData = findData;
        }

        public FileMapping(string puppetPath, string actualPath)
        {
            PuppetPath = puppetPath;
            ActualPath = actualPath;
            IntPtr handleIntPtr = NativeMethods.FindFirstFileW(actualPath, ref _findData);
            _findData.cFileName = Path.GetFileName(puppetPath);
            NativeMethods.FindClose(handleIntPtr);
        }

        //public IntPtr HandleIntPtr { get; }

        public NativeMethods.Win32FindData FindData => _findData;

        /// <summary>
        /// 傀儡路径
        /// </summary>
        public string PuppetPath { get; set; }

        /// <summary>
        /// 文件实际存放的路径
        /// </summary>
        public string ActualPath { get; set; }
    }
}
