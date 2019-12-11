using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using EasyHook;
using InjectedLib;

namespace HeyoVirtualFile
{
    [Serializable]
    public class VirtualFileSystem
    {
        private static  string InjectedLibPath = "InjectedLib.dll";

        public static void ReleaseLib()
        {
            FileStream fs;
            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook.dll"), FileMode.Create, FileAccess.Write);
            byte[] easyHookDll = Properties.Resources.EasyHookDll;
            fs.Write(easyHookDll, 0, easyHookDll.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook.xml"), FileMode.Create, FileAccess.Write);
            byte[] easyHookXml = Encoding.UTF8.GetBytes(Properties.Resources.EasyHookXml);
            fs.Write(easyHookXml, 0, easyHookXml.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook32.dll"), FileMode.Create, FileAccess.Write);
            byte[] easyHook32 = Properties.Resources.EasyHook32;
            fs.Write(easyHook32, 0, easyHook32.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook32Svc.exe"), FileMode.Create, FileAccess.Write);
            byte[] easyHook32Svc = Properties.Resources.EasyHook32Svc;
            fs.Write(easyHook32Svc, 0, easyHook32Svc.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook64.dll"), FileMode.Create, FileAccess.Write);
            byte[] easyHook64 = Properties.Resources.EasyHook64;
            fs.Write(easyHook64, 0, easyHook64.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyHook64Svc.exe"), FileMode.Create, FileAccess.Write);
            byte[] easyHook64Svc = Properties.Resources.EasyHook64Svc;
            fs.Write(easyHook64Svc, 0, easyHook64Svc.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyLoad32.dll"), FileMode.Create, FileAccess.Write);
            byte[] easyLoad32 = Properties.Resources.EasyLoad32;
            fs.Write(easyLoad32, 0, easyLoad32.Length);
            fs.Close();

            fs = new FileStream(
                Path.Combine(Environment.CurrentDirectory, "EasyLoad64.dll"), FileMode.Create, FileAccess.Write);
            byte[] easyLoad64 = Properties.Resources.EasyLoad64;
            fs.Write(easyLoad64, 0, easyLoad64.Length);
            fs.Close();

            InjectedLibPath = Path.Combine(Environment.CurrentDirectory, "InjectedLib.dll");
            if (File.Exists(InjectedLibPath)) return;

            fs = new FileStream(Path.Combine(Environment.CurrentDirectory, "InjectedLib.dll"), FileMode.Create, FileAccess.Write);
            byte[] injectedLib = Properties.Resources.InjectedLib;
            fs.Write(injectedLib, 0, injectedLib.Length);
            fs.Close();

        }

        static VirtualFileSystem()
        {
            ReleaseLib();
        }
        public VirtualFileSystem(int targetPid)
        {
            if(!File.Exists(Path.Combine(Environment.CurrentDirectory, "InjectedLib.dll")))
                throw new Exception("请先调用VirtualFileSystem.ReleaseLib");

            TargetPid = targetPid;
            ipcServer = RemoteHooking.IpcCreateServer<HookMonitor>(ref _channel, WellKnownObjectMode.Singleton);
        }

        public VirtualFileSystem(string targetExePath, string commandLine)
        {
            if (!File.Exists(Path.Combine(Environment.CurrentDirectory, "InjectedLib.dll")))
                throw new Exception("请先调用VirtualFileSystem.ReleaseLib");

            TargetExePath = targetExePath;
            CommandLine = commandLine;
            ipcServer = RemoteHooking.IpcCreateServer<HookMonitor>(ref _channel, WellKnownObjectMode.Singleton);
        }

        public string CommandLine { get; }
        public int TargetPid { get; private set; } = -1;
        public string TargetExePath { get; }

        public Dictionary<string, FileMapping> PuppetFiles { get; } = new Dictionary<string, FileMapping>();

        public void AddPuppetFile(FileMapping puppet)
        {
            PuppetFiles.Add(puppet.PuppetPath,puppet);
            if (!HookMonitor.FileMappingDictionaryPool.ContainsKey(_channel))
            {
                HookMonitor.FileMappingDictionaryPool.Add(_channel,new Dictionary<string, FileMapping>());
            }
            HookMonitor.FileMappingDictionaryPool[_channel].Add(puppet.PuppetPath, puppet);
        }

        private readonly IpcServerChannel ipcServer;
        private readonly string _channel;
        public bool Run()
        {

            if (TargetExePath != null)
            {
                RemoteHooking.CreateAndInject(TargetExePath, CommandLine,
                    0,
                    InjectedLibPath,
                    InjectedLibPath,
                    out int pid,
                    new Func<Dictionary<string, FileMapping>>(() => PuppetFiles));
                TargetPid = pid;
            }
            else if (TargetPid > 0)
            {
                RemoteHooking.Inject(TargetPid,
                    InjectedLibPath,
                    InjectedLibPath,
                    _channel);
            }

            return false;
        }

        
    }
}