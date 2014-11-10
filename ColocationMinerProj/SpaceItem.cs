/**
 * 空间实例类
 * Copyright Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColocationMiner
{
    class SpaceItem
    {
        public char Feature { get; set; }  //所属特征
        public int Index { get; set; } //序
        public double X { get; set; } //横坐标
        public double Y { get; set; } //纵坐标
        public string Format() //格式化显示
        {
            return "" + Feature + Index;
        }
    }
}
