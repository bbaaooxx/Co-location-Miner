/**
 * 公共操作封装类
 * Copyright:Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColocationMiner
{
    sealed class CommonOpera
    {
        //生成空间实例集合和空间特征集
        public static SpaceItem GenSpaceItem(string lines)
        {
            String[] strs = lines.Split(new char[] { '\t', ' '}, StringSplitOptions.RemoveEmptyEntries);
            SpaceItem item = new SpaceItem();
            item.Feature = Convert.ToChar(strs[1]);
            item.Index = Int32.Parse(strs[0]);
            item.X = Double.Parse(strs[2]);
            item.Y = Double.Parse(strs[3]);
            return item;
        }
    }
}
