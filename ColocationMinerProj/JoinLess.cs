/**
 * 无连接算法实现
 * Copyright:Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    class JoinLess:ColocationBase
    {
        private List<Dictionary<string, List<HashSet<string>>>> instanceSet = new List<Dictionary<string, List<HashSet<string>>>>();
        //邻居矩阵
        protected Dictionary<string, Dictionary<string, string>> neighbourhoods = new Dictionary<string, Dictionary<string, string>>();
        private string[,] grids;

        public JoinLess(RichTextBox output, string[] contents, string[] headLine)
            : base(output, contents, headLine)
        {

        }

        //初始化1阶实例集,将实例加入到实例集中
        public override void InitInstanceSet(SpaceItem item)
        {
            AddInstance("" + item.Feature, item.Feature, item.Format());
        }

        //返回某个模式下的所有的实例集字符串形式
        public override string GetInstanceFormat(string pattern)
        {
            string ret = "";
            List<HashSet<string>> list = instanceSet[pattern.Length - 1][pattern];
            foreach (HashSet<string> hs in list)
            {
                string str = "{";
                foreach (string s in hs)
                {
                    str = str + s + ',';
                }
                if (str.EndsWith(","))
                {
                    str = str.Substring(0, str.Length - 1);
                }
                str = str + '}';
                ret = ret + str + "  ";
            }
            return ret;
        }

        //清空K阶实例的所有内容
        public override void ClearInstance(int k)
        {
            instanceSet[k].Clear();
        }

        //从实例集中计算每个特征下相应的实例个数（‘A’， 100）
        public override Dictionary<char, int> CalcInsCountPerFeature()
        {
            Dictionary<char, int> ret = new Dictionary<char, int>();
            foreach (string key in instanceSet[0].Keys)
            {
                ret.Add(Convert.ToChar(key), instanceSet[0][key][0].Count);
            }
            return ret;
        }

        public override void Init()
        {
            base.Init();
            DateTime dt1 = DateTime.Now;
            GenNeighbourhoods();
            DateTime dt2 = DateTime.Now;
            TimeSpan ts = dt2 - dt1;
            SetInfo("neighbors generated cost:" + Math.Round(ts.TotalSeconds, 2) + "s\n");
        }

        protected override void ReadHeadLine(string[] headLine)
        {
            base.ReadHeadLine(headLine);
            grids = new string[GetDivResult(range) + 1, GetDivResult(range) + 1];
        }

        public override void ReadData(string []contents)
        {
            for (int i = 0; i < contents.Length; i++)
            {
                setCurrStatus("Current string：" + contents[i] + ":0");
                if (contents[i].Trim().Length == 0)
                {
                    continue;
                }
                SpaceItem item = GenSpaceItem(contents[i]);
                instances.Add(item.Format(), item);
                InitInstanceSet(item);
                int x = GetDivResult(item.X);
                int y = GetDivResult(item.Y);
                grids[x, y] += (item.Format() + ',');
            }
        }

        public void GenNeighbourhoods()
        {
            SetInfo("Generating neighbours' relation...\n");
            for (int i = 0; i < grids.GetLength(0); i++)
            {
                for (int j = 0; j < grids.GetLength(1); j++)
                {
                    string instancesStr = grids[i, j];
                    if (instancesStr != null) //如果该格内无值就跳过
                    {
                        string baseStr = instancesStr.Substring(0, instancesStr.Length - 1);
                        string[] baseItems = baseStr.Split(',');
                        //左上
                        if (i - 1 >= 0 && j - 1 >= 0)
                        {
                            instancesStr = instancesStr + (grids[i - 1, j - 1] == null ? "" : grids[i - 1, j - 1]);
                        }
                        //左下
                        if (i + 1 < grids.GetLength(0) && j - 1 >= 0)
                        {
                            instancesStr = instancesStr + (grids[i + 1, j - 1] == null ? "" : grids[i + 1, j - 1]);
                        }
                        //左
                        if (j - 1 >= 0)
                        {
                            instancesStr = instancesStr + (grids[i, j - 1] == null ? "" : grids[i, j - 1]);
                        }
                        //右
                        if (j + 1 < grids.GetLength(0))
                        {
                            instancesStr = instancesStr + (grids[i, j + 1] == null ? "" : grids[i, j + 1]);
                        }
                        //上
                        if (i - 1 >= 0)
                        {
                            instancesStr = instancesStr + (grids[i - 1, j] == null ? "" : grids[i - 1, j]);
                        }
                        //下
                        if (i + 1 < grids.GetLength(0))
                        {
                            instancesStr = instancesStr + (grids[i + 1, j] == null ? "" : grids[i + 1, j]);
                        }
                        //右下
                        if (i + 1 < grids.GetLength(0) && j + 1 < grids.GetLength(1))
                        {
                            instancesStr = instancesStr + (grids[i + 1, j + 1] == null ? "" : grids[i + 1, j + 1]);
                        }
                        //右上
                        if (i - 1 >= 0 && j + 1 < grids.GetLength(1))
                        {
                            instancesStr = instancesStr + (grids[i - 1, j + 1] == null ? "" : grids[i - 1, j + 1]);
                        }
                        instancesStr = instancesStr.Substring(0, instancesStr.Length - 1);
                        string[] instances = instancesStr.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string item1 in baseItems)
                        {
                            String str = item1;
                            foreach (String item2 in instances)
                            {
                                if (item1.ElementAt(0) < item2.ElementAt(0)) //保证按序
                                {
                                    SpaceItem sp1 = base.instances[item1];
                                    SpaceItem sp2 = base.instances[item2];
                                    if (Math.Sqrt(Math.Pow(sp1.X - sp2.X, 2) + Math.Pow(sp1.Y - sp2.Y, 2)) <= minDistance)
                                    {
                                        str = str + ',' + item2;
                                    }

                                }
                            }
                            if (neighbourhoods.ContainsKey(item1.ElementAt(0).ToString()))
                            {
                                neighbourhoods[item1.ElementAt(0).ToString()].Add(item1, str);
                            }
                            else
                            {
                                Dictionary<string,string> dic = new Dictionary<string, string>();
                                dic.Add(item1, str);
                                neighbourhoods.Add(item1.ElementAt(0).ToString(), dic);
                            }
                            setCurrStatus(item1 + "'s relation finished:0");
                        }
                    }
                }
            }
        }

        public override bool GenInstancesAndFrequent(string pattern)
        {
            Dictionary<string, List<HashSet<string>>> dic = new Dictionary<string, List<HashSet<string>>>();
            //2阶频繁项的生成，需要借助邻接矩阵来完成，从pattern[0]特征的邻居矩阵中找pattern[1]的实例
            //如果pattern[0]的实例中有存在pattern[1]的时候，分别加入pattern对应实例的集合中，否则都不加
            if (pattern.Length == 2)
            {
                dic = GenIstances42(pattern);
            }
            else //否则，遍历K-1阶取交集
            {
                dic = GenInstances(pattern);
            }
            if (dic.Count == 0) //无匹配项，必定不频繁
            {
                return false;
            }
            double d = CalcParticipate(dic, pattern);
            if (d >= minParticipate) //是频繁项
            {
                participates.Add(pattern, d);
                if (instanceSet.Count < pattern.Length)
                {
                    instanceSet.Add(dic);
                }
                else
                {
                    instanceSet[pattern.Length - 1].Add(pattern, dic[pattern]);
                }
                return true;
            }
            return false;   
        }

        //>2阶项实例的生成
        private Dictionary<string, List<HashSet<string>>> GenInstances(string pattern)
        {
            Dictionary<string, List<HashSet<string>>> ret = new Dictionary<string, List<HashSet<string>>>();
            Dictionary<char, bool> flags = new Dictionary<char,bool>(); //标志位
            Dictionary<char, HashSet<string>> dic = new Dictionary<char,HashSet<string>>(); //统计Map
            for (int i = 0; i < pattern.Length; i++) //遍历子序列
            {
                string subPattern = pattern.Remove(i, 1);
                //取子序列的每个特征的实例集合进行交运算
                List<HashSet<string>> lhs = instanceSet[subPattern.Length - 1][subPattern];
                for (int j = 0; j < lhs.Count; j++)
                {
                    HashSet<string> hs = lhs[j];
                    if (!flags.ContainsKey(subPattern[j]))
                    {
                        flags.Add(subPattern[j], true);
                        dic.Add(subPattern[j], hs); //第一次为直接赋值，之后为交操作
                    }
                    else //开始交运算
                    {
                        dic[subPattern[j]].IntersectWith(hs);
                        if (dic[subPattern[j]].Count == 0) //交运算结果为空，必不是频繁项，提前退出
                        {
                            return ret;
                        }
                    }
                }
            }
            //存在序列，开始整合ret
            List<HashSet<string>> ls = new List<HashSet<string>>();
            foreach(char feature in pattern){
                ls.Add(dic[feature]);
            }
            ret.Add(pattern, ls);          
            return ret;
        }

        //2阶项实例生成
        private Dictionary<string, List<HashSet<string>>> GenIstances42(string pattern)
        {
            Dictionary<string, List<HashSet<string>>> ret = new Dictionary<string, List<HashSet<string>>>();
            char feature1 = pattern[0];
            char feature2 = pattern[1];
            //遍历feature1的各个键值
            foreach (string key in neighbourhoods["" + feature1].Keys)
            {
                string instanceStr = neighbourhoods["" + feature1][key];
                string matchPattern = feature2 + @"\d+";
                MatchCollection collection = Regex.Matches(instanceStr, matchPattern);
                //有匹配，则key加入feature1对应的集合中
                if (collection.Count > 0)
                {
                    AddInstance(ret, pattern, key[0], key);
                }
                foreach (Match match in collection)
                {
                    string instance = match.Value;
                    AddInstance(ret, pattern, instance[0], instance);
                }
            }
            return ret;
        }

        

        //计算当前模式的参与度
        private double CalcParticipate(Dictionary<string, List<HashSet<string>>> dic, string pattern)
        {
            double min = 1;
            for (int i = 0; i < dic[pattern].Count; i++ )
            {
                int count = dic[pattern][i].Count;
                double d = (double)count / (double)countPerFeature[pattern[i]];
                if (d < min)
                {
                    min = d;
                }
            }
            return min;
        }

        private void AddInstance(string pattern, char feature, string value)
        {
            if (instanceSet.Count < pattern.Length)
            {
                Dictionary<string, List<HashSet<string>>> dic = new Dictionary<string, List<HashSet<string>>>();
                instanceSet.Add(dic);
            }
            AddInstance(instanceSet[pattern.Length - 1], pattern, feature, value);
        }

        private void AddInstance(Dictionary<string, List<HashSet<string>>> dic, string pattern, char feature, string value)
        {
            if (!dic.ContainsKey(pattern))
            {           
                List<HashSet<string>> ls = new List<HashSet<string>>();
                for(int i = 0; i < pattern.Length; i++){
                    HashSet<string> hs = new HashSet<string>();
                    ls.Add(hs);
                }     
                dic.Add(pattern, ls);
            }
            dic[pattern][pattern.IndexOf(feature)].Add(value);
        }

    }
}
