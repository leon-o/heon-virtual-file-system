using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InjectedLib
{
    public class HookMonitor:MarshalByRefObject
    {
        public static Dictionary<string,Dictionary<string,FileMapping>> FileMappingDictionaryPool=new Dictionary<string, Dictionary<string, FileMapping>>();

        public Dictionary<string, FileMapping> GetFileMappingDic(string channelName)
        {
            if(!FileMappingDictionaryPool.ContainsKey(channelName))
                FileMappingDictionaryPool.Add(channelName,new Dictionary<string, FileMapping>());
            return FileMappingDictionaryPool[channelName];
        }

        public void Log(string log)
        {
            Console.WriteLine(log);
        }

        internal void LogException(Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
