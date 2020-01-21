# heon-virtual-file-system

## Description

 A virtual file system based on .net and easyhook.
 
 Theoretically, it can be used on any program that ued Win32 API to operate file.

 一个基于 .NET 和 [Easyhook](https://github.com/EasyHook/EasyHook) 的虚拟文件系统。

 **理论上对任何使用 Win32 API 来操作文件的程序有效。**
 
 **意识到一个很严重的问题：用Hardlink它不香吗**
 https://docs.microsoft.com/zh-cn/windows/win32/api/winbase/nf-winbase-createhardlinkw
 ## Principle

 It can hook APIs such as `FindFirstFileW FindNextFileW CreateFileW` .
