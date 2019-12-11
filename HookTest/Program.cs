using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EasyHook;
using InjectedLib;
using Console = System.Console;

namespace HookTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            HeyoVirtualFile.VirtualFileSystem.ReleaseLib();
            int pid = -1;
            //pid = int.Parse(Console.ReadLine());
            while (pid == -1)
            {
                Process[] localByName = Process.GetProcessesByName("java");
                if (localByName.Length > 0)
                    pid = localByName[0].Id;
            }
            

            HeyoVirtualFile.VirtualFileSystem vfs=new HeyoVirtualFile.VirtualFileSystem(pid);
            vfs.Run();
            vfs.AddPuppetFile(new FileMapping(@"D:\Virtual\Test.txt", @"D:\Virtual.txt"));
            vfs.AddPuppetFile(new FileMapping(@"D:\Virtual\Test666.txt", @"D:\Virtual.txt"));
            

            Console.WriteLine("Inject Success");
            Console.WriteLine("现在你可以添加虚拟文件，输入格式： 实际文件路径 虚拟文件路径");
            while (true)
            {
                string actual = Console.ReadLine();
                string vir = Console.ReadLine();
                vfs.AddPuppetFile(new FileMapping(vir,actual));
                Console.WriteLine("添加成功");
            }
            Console.ReadKey();
        }
    }
}
