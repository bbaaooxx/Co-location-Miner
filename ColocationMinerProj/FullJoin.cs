/**
 * 全连接算法实现
 * Copyright:Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    class FullJoin:ColocationBase
    {
        private List<Dictionary<string, string>> instanceSet = new List<Dictionary<string, string>>();
        private Dictionary<string, string> p2pRelations = new Dictionary<string, string>(); //保存某实例点的所有的满足邻接关系的实例点
        public bool IsMultiPrunning { private get; set; } //多分辨剪枝
        private string[,] grids; //网格，用在2阶实例生成的时候可以明显加快速度
        private List<Dictionary<string, string>> coarseSets = new List<Dictionary<string, string>>(); //粗糙表实例集
        //每个特征在每个方格里的个数
        private Dictionary<char, Dictionary<string, int>> countInPoints = new Dictionary<char, Dictionary<string, int>>();
       
        public FullJoin(RichTextBox output, string[] contents, string[] headLine)
            : base(output, contents, headLine)
        {
            
            IsMultiPrunning = false;
        }

        //初始化1阶实例集,将实例加入到实例集中
        public override void  InitInstanceSet(SpaceItem item)
        {
            if (instanceSet.Count == 0)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                instanceSet.Add(dic);
            }
            if (instanceSet[0].ContainsKey("" + item.Feature))
            {
                instanceSet[0]["" + item.Feature] = instanceSet[0]["" + item.Feature] + ";" + item.Format();
            }
            else
            {
                instanceSet[0].Add("" + item.Feature, "" + item.Format());
            }
        }

        //返回某个模式下的所有的实例集字符串形式
        public override string GetInstanceFormat(string pattern)
        {
            return instanceSet[pattern.Length - 1][pattern];
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
                ret.Add(Convert.ToChar(key), instanceSet[0][key].Split(';').Length);
            }
            return ret;
        }

        protected override void ReadHeadLine(string[] headLine)
        {
            base.ReadHeadLine(headLine);
            grids = new string[GetDivResult(range) + 1, GetDivResult(range) + 1];
        }

        public override void ReadData(string[] contents)
        {
            try
            {
                for (int i = 0; i < contents.Length; i++)
                {
                    setCurrStatus("Current string:" + contents[i] + ":0");
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
                    if (IsMultiPrunning) //多分辨剪枝，1阶加入
                    {
                        string key = "" + item.Feature;
                        string value = "" + x + ',' + y;
                        if (!countInPoints.ContainsKey(item.Feature))
                        {
                            countInPoints.Add(item.Feature, new Dictionary<string, int>());
                        }
                        if (countInPoints[item.Feature].ContainsKey(value))
                        {
                            countInPoints[item.Feature][value]++;
                        }
                        else
                        {
                            countInPoints[item.Feature].Add(value, 1);
                        }
                        if (coarseSets.Count == 0)
                        {
                            coarseSets.Add(new Dictionary<string, string>());
                        }
                        if (!coarseSets[0].ContainsKey(key))
                        {
                            coarseSets[0].Add(key, value);
                        }
                        else
                        {
                            if (!coarseSets[0][key].Contains(value))
                            {
                                coarseSets[0][key] += ('|' + value);
                            }
                        }
                    }
                }
            }
            catch
            {
                throw new Exception("Bad formated data!");
            }
        }
        
        //生成K阶实例集
        public override bool GenInstancesAndFrequent(string pattern)
        {
            if (IsMultiPrunning)
            {
                if (!MultiPrunning(pattern))
                {
                    return false;
                }
            }
            Dictionary<String, String> dic = new Dictionary<String, String>(); 
            if (pattern.Length == 2) //2阶模式，利用网格处理
            {
                dic.Add(pattern, GenInstancesAndFrequent42(pattern));
            }
            else
            {
                if (p2pRelations.Count > 0)
                {
                    p2pRelations.Clear();  //已无用，删之
                    grids = null;  //释放空间
                }
                HashSet<String> hs = new HashSet<string>();
                string pattern1 = pattern.Substring(0, pattern.Length - 2) + pattern[pattern.Length - 2];
                string pattern2 = pattern.Substring(0, pattern.Length - 2) + pattern[pattern.Length - 1];
                String joinString1 = instanceSet.ElementAt(pattern1.Length - 1)[pattern1];
                String joinString2 = instanceSet.ElementAt(pattern2.Length - 1)[pattern2];
                String[] instances1 = joinString1.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
                //遍历连接，并对后两项进行距离计算
                foreach (String sh1 in instances1)
                {
                    int pos = sh1.LastIndexOf(',');
                    string prev = sh1.Substring(0, pos);
                    string join1 = sh1.Substring(pos + 1);
                    MatchCollection collection = Regex.Matches(joinString2, prev + ',' + @"\w\d+");
                    foreach (Match match in collection)
                    {
                        string sh2 = match.Value;
                        pos = sh2.LastIndexOf(',');
                        string join2 = sh2.Substring(pos + 1);                 
                        if (CanJoinInstance(join1, join2))
                        {
                            if (dic.ContainsKey(pattern))
                            {
                                dic[pattern] = dic[pattern] + ';' + sh1 + ',' + join2;
                            }
                            else
                            {
                                dic.Add(pattern, sh1 + ',' + join2);
                            }
                        }
                    }
                }
            }
            //计算当前对应候选项的实例的参与度，过滤并完成频繁项的生成
            double d = CalcParticipate(dic, pattern);
            if (d >= minParticipate)
            {
                if (instanceSet.Count < pattern.Length)
                {
                    instanceSet.Add(dic);
                }
                else
                {
                    instanceSet[pattern.Length - 1].Add(pattern, dic[pattern]);
                }
                participates.Add(pattern, d);
                return true;
            }
            return false;
        }

        //多分辨剪枝，返回该模式是否满足粗表的参与度
        private bool MultiPrunning(string pattern)
        {
            string values = "";
            if (pattern.Length == 2) //2阶直接找相邻
            {
                string join1 = "" + pattern[0];
                string join2 = "" + pattern[1];
                string[] points = coarseSets[0][join1].Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string point in points)
                {
                    int index =  point.IndexOf(',');
                    int x = Convert.ToInt32(point.Substring(0, index));
                    int y = Convert.ToInt32(point.Substring(index + 1));
                    //判断(x,y)为中心包括(x,y)的9个格子是否在join2对应的坐标里
                    string str = JoinPoints("", x, y, coarseSets[0][join2]);
                    values = values + (str.Length == 0? str : (str + '|'));
                }
            }
            else  //2阶以上模式的处理方式
            {
                if (pattern.Length - 3 >= 0)
                {
                    coarseSets[pattern.Length - 3].Clear();
                }
                //取需要连接的两个模式
                string pattern1 = pattern.Substring(0, pattern.Length - 1);
                string pattern2 = pattern.Substring(0, pattern.Length - 2) + pattern.Last();
                string []points = coarseSets[pattern1.Length - 1][pattern1].Split(new char[]{'|'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string point in points)
                {
                    int index = point.LastIndexOf(';');
                    string lastPoint = point.Substring(index + 1);
                    string prefix = point.Substring(0, index);
                    index = lastPoint.IndexOf(',');
                    int x = Convert.ToInt32(lastPoint.Substring(0, index));
                    int y = Convert.ToInt32(lastPoint.Substring(index + 1));
                    //判断(x,y)为中心包括(x,y)的9个格子是否在pattern2对应的坐标里
                    string str = JoinPoints(prefix, x, y, coarseSets[pattern2.Length - 1][pattern2]);
                    values = values + (str.Length == 0 ? str : (str + '|'));
                }
            }
            //无可连接项，必定不频繁
            if (values.Length == 0)
            {
                return false;
            }
            //统计模式中每个特征的坐标列表
            Dictionary<char, HashSet<string>> dic = new Dictionary<char, HashSet<string>>();
            foreach (char c in pattern)
            {
                dic.Add(c, new HashSet<string>());
            }
            string[] strs = values.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in strs)
            {
                string[] ss = str.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < pattern.Length; i++)
                {
                    dic[pattern[i]].Add(ss[i]);
                }
            }
            //根据上面的统计坐标，依次计算每个特征的最小值，并拿到当前模式的最小粗参与度并返回剪枝结果
            double min = 1;
            foreach (char c in pattern)
            {
                int count = 0;
                double roughParticipate = 0;
                foreach (string pos in dic[c])
                {
                    count += countInPoints[c][pos];
                }
                roughParticipate = (double)(count) / (double)countPerFeature[c];
                if (roughParticipate < min)
                {
                    min = roughParticipate;
                }
            }
            if (min >= minParticipate)
            {
                if(coarseSets.Count < pattern.Length){
                    coarseSets.Add(new Dictionary<string,string>());
                }
                coarseSets[pattern.Length - 1].Add(pattern, values);
                return true;
            }
            return false;
        }

        //从referStr中找到满足(x,y)相邻点的所有点对的字符串形式
        private string JoinPoints(string prefix, int i, int j, string referStr)
        {
            prefix = prefix.Length == 0 ? prefix : prefix + ';';
            string ret = "";
            string newPrefix = prefix + i + ',' + j + ';';
            //自己本身
            string position = "" + i + ',' + j;
            string baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //左上
            position = "" + (i - 1) + ',' + (j - 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //左下
            position = "" + (i + 1) + ',' + (j - 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //左
            position = "" + i + ',' + (j - 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //右
            position = "" + i + ',' + (j + 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //上
            position = "" + (i - 1) + ',' + j;
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //下
            position = "" + (i + 1) + ',' + j;
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //右下
            position = "" + (i + 1) + ',' + (j + 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            //右上
            position = "" + (i - 1) + ',' + (j + 1);
            baseStr = prefix + position;
            if (referStr.Contains(baseStr))
            {
                ret = ret + newPrefix + position + '|';
            }
            return ret.EndsWith("|") ? ret.Substring(0, ret.Length - 1) : ret;
        }

        //2频繁项及实例生成，寻找2阶pattern的所有实例集
        private string GenInstancesAndFrequent42(string pattern)
        {
            string ret = "";
            char mainFeature = pattern[0];
            string []instances = instanceSet[0]["" + mainFeature].Split(new char[]{';'},StringSplitOptions.RemoveEmptyEntries);
            foreach (string sh in instances)
            {
                if (!p2pRelations.ContainsKey(sh))
                {
                    GenP2PRelations(sh);
                }
                string relations = p2pRelations[sh];
                if (!relations.Contains(pattern[1])) //无此实例
                {
                    continue;
                }
                string searchPattern = pattern[1] + @"\d+" ;
                MatchCollection collection = Regex.Matches(relations, searchPattern);
                foreach (Match next in collection)
                {
                    ret = ret + sh + ',' + next.Value + ';';
                }
            }
            return ret.Equals("") ? ret : ret.Substring(0, ret.Length - 1);
        }

        private void GenP2PRelations(string sh)
        {
            int i = GetDivResult(this.instances[sh].X);
            int j = GetDivResult(this.instances[sh].Y);
            //找到当前实例所在格子中的所有实例
            string instancesStr = grids[i, j];
            //以下是将周围8个格子的所有实例全部收集起来
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
            string[] instances = instancesStr.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries); //分解
            //开始进行连接
            string connStrs = "";
            foreach (string s in instances)
            {
                if (sh.ElementAt(0) < s.ElementAt(0)) //保证按序，只与排序在后面的实例相连
                {
                    SpaceItem sp1 = base.instances[sh];
                    SpaceItem sp2 = base.instances[s];
                    if (Math.Sqrt(Math.Pow(sp1.X - sp2.X, 2) + Math.Pow(sp1.Y - sp2.Y, 2)) <= minDistance)
                    {
                        connStrs = connStrs  + s + ',';
                    }
                }
            }
            if (connStrs.EndsWith(","))
            {
                connStrs = connStrs.Substring(0, connStrs.Length - 1);
            }
            p2pRelations.Add(sh, connStrs);
        }

        // 判断实例是否可以组合(距离约束)
        //  sh1 sh2 待组合实例序列最后一项
        private bool CanJoinInstance(String sh1, String sh2)
        {
            SpaceItem item1 = instances[sh1];
            SpaceItem item2 = instances[sh2];  //取实例对象
            double distance;
            //计算距离
            distance = Math.Sqrt(Math.Pow(item1.X - item2.X, 2) + Math.Pow(item1.Y - item2.Y, 2));
            if (distance <= minDistance)
            {
                return true;
            }
            return false;
        }
    }
}
