using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using EasyHook;

namespace InjectedLib
{
    /// <inheritdoc />
    public class Injected : IEntryPoint
    {
        private LocalHook _findFirstFileHook, _findNextFileHook, _openHook, _findCloseHook;

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate int DCreateFileW(string fileName, uint desiredAccess, uint shareMode, uint securityAttributes,
            uint creationDisposition, uint flagsAndAttributes, uint templateFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate IntPtr DFindFirstFileW(string pFileName, ref NativeMethods.Win32FindData pFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate bool DFindNextFileW(IntPtr hndFindFile, ref NativeMethods.Win32FindData lpFindFileData);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate bool DFindClose(IntPtr hndFindFile);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate int DGetFileAttributesW(string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate int DSetFileAttributesW(string path, int dwFileAttributes);

        private readonly HookMonitor _monitor;

        private const int Version = 5;
        public Injected(RemoteHooking.IContext iContext,
            string channel
            )
        {
            _channel = channel;
            _monitor = RemoteHooking.IpcConnectClient<HookMonitor>(channel);
            Console.WriteLine($"VirtualFileSystem Injected Dll Ver.{Version}");
        }

        public HashSet<string> PathWhiteList { get; }=new HashSet<string>();

        private readonly string _channel;
        public void Run(RemoteHooking.IContext iContext,
            string channel)
        {
            //GetPuppetFilesFunc = getPuppetFilesFunc;
            /*获取目标函数的句柄*/
            IntPtr createFileWPtr = LocalHook.GetProcAddress("kernel32.dll", "CreateFileW");
            IntPtr findFirstFileWPtr = LocalHook.GetProcAddress("kernel32.dll", "FindFirstFileW");
            IntPtr findNextFileWPtr = LocalHook.GetProcAddress("kernel32.dll", "FindNextFileW");
            IntPtr findClosePtr = LocalHook.GetProcAddress("kernel32.dll", "FindClose");
            IntPtr getFileAttributesPtr = LocalHook.GetProcAddress("kernel32.dll", "GetFileAttributesW");
            IntPtr setFileAttributesPtr = LocalHook.GetProcAddress("kernel32.dll", "SetFileAttributesW");

            /*创建钩子*/
            _openHook = LocalHook.Create(createFileWPtr, new DCreateFileW(CreateFileCallBack), this);
            _findFirstFileHook = LocalHook.Create(findFirstFileWPtr, new DFindFirstFileW(FindFirstFileCallBack), this);
            _findNextFileHook = LocalHook.Create(findNextFileWPtr, new DFindNextFileW(FindNextFileCallBack), this);
            _findCloseHook = LocalHook.Create(findClosePtr, new DFindClose(FindCloseCallBack), this);
            _getFileAttributesHook = LocalHook.Create(getFileAttributesPtr, new DGetFileAttributesW(GetFileAttributesCallBack), this);
            _setFileAttributesHook = LocalHook.Create(setFileAttributesPtr, new DSetFileAttributesW(SetFileAttributesCallBack), this);

            /*设置ACL，我不知道有什么用*/
            _openHook.ThreadACL.SetExclusiveACL(new int[1]);
            _findFirstFileHook.ThreadACL.SetExclusiveACL(new int[1]);
            _findNextFileHook.ThreadACL.SetExclusiveACL(new int[1]);
            _findCloseHook.ThreadACL.SetExclusiveACL(new int[1]);
            _getFileAttributesHook.ThreadACL.SetInclusiveACL(new int[1]);
            _setFileAttributesHook.ThreadACL.SetInclusiveACL(new int[1]);

            RemoteHooking.WakeUpProcess();

            while (true) Thread.Sleep(500);
        }

        private static int SetFileAttributesCallBack(string path, int dwfileattributes)
        {
            return NativeMethods.SetFileAttributesW(path,dwfileattributes);
        }

        private static int GetFileAttributesCallBack(string path)
        {
            return NativeMethods.GetFileAttributesW(path);
        }

        public Dictionary<string, FileMapping> FileMappingDic { get; private set; } = new Dictionary<string, FileMapping>();

        private IEnumerable<FileMapping> GetActualFiles(string fileName)
        {
            
            NativeMethods.Win32FindData findFileData = new NativeMethods.Win32FindData();
            List<FileMapping> actualFiles = new List<FileMapping>();
            IntPtr hFind = NativeMethods.FindFirstFileW(fileName, ref findFileData);

            if (hFind != new IntPtr(-1))
            {
                do
                {
                    //if (findFileData.cFileName.Equals(@".") || findFileData.cFileName.Equals(@".."))
                    //    continue;
                    //if ((findFileData.dwFileAttributes & 0x10) != 0)
                    //{
                    //    string path = Path.Combine(Path.GetDirectoryName(fileName) ?? throw new InvalidOperationException(), findFileData.cFileName);
                    //    actualFiles.Add(new FileMapping(path, findFileData));
                    //}
                    //else
                    //{
                        string path = Path.Combine(Path.GetDirectoryName(fileName) ?? throw new InvalidOperationException(), findFileData.cFileName);
                        actualFiles.Add(new FileMapping(path, findFileData));
                    //}


                }
                while (NativeMethods.FindNextFileW(hFind, ref findFileData));
            }
            NativeMethods.FindClose(hFind);

            //Console.WriteLine("///////////////////ACTUAL///////////////////");
            //foreach (FileMapping actualFile in actualFiles)
            //{
            //    _monitor.Log($"{actualFile.FindData.cFileName}");
            //}
            //Console.WriteLine("///////////////////ACTUAL///////////////////");
            return actualFiles;
        }

        private readonly Dictionary<IntPtr, Queue<FileMapping>> _searchingFiles = new Dictionary<IntPtr, Queue<FileMapping>>();
        private LocalHook _getFileAttributesHook;
        private LocalHook _setFileAttributesHook;

        private IntPtr FindFirstFileCallBack(string pfilename, ref NativeMethods.Win32FindData pfindfiledata)
        {
            if (PathWhiteList.Count>0 && !PathWhiteList.Contains(Path.GetDirectoryName(pfilename)))
                return NativeMethods.FindFirstFileW(pfilename, ref pfindfiledata);

            FileMappingDic = _monitor.GetFileMappingDic(_channel);

            string matchPath = pfilename.Replace(@"\", @"\\")
                .Replace(".", @"\.")
                .Replace("*", ".*").Replace("?", ".");
            Regex regex = new Regex(matchPath);

            IEnumerable<FileMapping> actualFiles = GetActualFiles(pfilename);
            Queue<FileMapping> matchedFiles = new Queue<FileMapping>(actualFiles);

            if (FileMappingDic != null)
            {
                foreach (string key in FileMappingDic.Keys.Where(key => regex.Match(key).Value == key))
                {
                    matchedFiles.Enqueue(FileMappingDic[key]);
                }
            }

            //没找到返回INVALID_HANDLE_VALUE
            if (matchedFiles.Count == 0) return new IntPtr(-1);

            IntPtr ptr = NativeMethods.FindFirstFileW(matchedFiles.Peek().ActualPath, ref pfindfiledata);
            pfindfiledata = matchedFiles.Peek().FindData;
            matchedFiles.Dequeue();
            _searchingFiles.Add(ptr, matchedFiles);
            _monitor.Log(pfindfiledata.cFileName);
            return ptr;
        }

        private bool FindNextFileCallBack(IntPtr hndfindfile, ref NativeMethods.Win32FindData lpfindfiledata)
        {
            if (!_searchingFiles.ContainsKey(hndfindfile)) 
                return NativeMethods.FindNextFileW(hndfindfile,ref lpfindfiledata);

            
            Queue<FileMapping> queue = _searchingFiles[hndfindfile];

            if (queue.Count == 0) return false;

            lpfindfiledata = queue.Dequeue().FindData;
            _monitor.Log(lpfindfiledata.cFileName);

            /*还有文件的时候，返回true，没了返回False*/
            return true;
        }

        private bool FindCloseCallBack(IntPtr hndfindfile)
        {
            if (_searchingFiles?.ContainsKey(hndfindfile)==true)
            {
                _searchingFiles.Remove(hndfindfile);
            }

            return NativeMethods.FindClose(hndfindfile);
        }

        private int CreateFileCallBack(string filename, uint desiredaccess, uint sharemode,
            uint securityattributes, uint creationdisposition, uint flagsandattributes, uint templatefile)
        {

            if (FileMappingDic?.ContainsKey(filename)==true)
            {
                FileMapping fileMapping = FileMappingDic[filename];
                filename = fileMapping.ActualPath;
            }

            return NativeMethods.CreateFileW(filename, desiredaccess, sharemode, securityattributes,
                creationdisposition, flagsandattributes, templatefile);
        }
    }
}