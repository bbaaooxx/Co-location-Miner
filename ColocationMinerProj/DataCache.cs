/**
 * 缓存数据，这里主要是为了处理多线程速度不一致的问题而设置，写入程序速度太快而读取显示线程速度太慢
 * 为了缓和这个不平衡不耽误写入线程的运行时间，中间加入一个缓冲区，这里主要有规则缓冲区，频繁项缓冲区以及实例项缓冲区
 * 另外再加一个进度条信息，这个不需要缓存，且是瞬时的，在客户端按时间间隔取当前状态
 * Copyright Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColocationMiner
{
    sealed class DataCache
    {
        public static bool isEnd = true; //主体算法是否结束的标识
        public static string statusInfo ;

        public static void Init()
        {
            statusInfo = null;
        }
    }
}
