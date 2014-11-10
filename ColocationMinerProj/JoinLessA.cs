using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    class JoinLessA:ColocationBase
    {
        private List<Dictionary<string, string>> instanceSet = new List<Dictionary<string, string>>();
        //邻居矩阵
        protected Dictionary<char, Dictionary<string, string>> neighbourhoods = new Dictionary<char, Dictionary<string, string>>();
        private string[,] grids;

        public JoinLessA(RichTextBox output, string[] contents, string[] headLine)
            : base(output, contents, headLine)
        {

        }

        //初始化1阶实例集,将实例加入到实例集中
        public override void InitInstanceSet(SpaceItem item)
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
                }
            }
            catch
            {
                throw new Exception("Bad formated data!");
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
                            if (neighbourhoods.ContainsKey(item1[0]))
                            {
                                neighbourhoods[item1[0]].Add(item1, str);
                            }
                            else
                            {
                                Dictionary<string,string> dic = new Dictionary<string, string>();
                                dic.Add(item1, str);
                                neighbourhoods.Add(item1[0], dic);
                            }
                            setCurrStatus(item1 + "'s relation finished:0");
                        }
                    }
                }
            }
        }

        public override bool GenInstancesAndFrequent(string pattern)
        {
            Dictionary<String, String> dic = new Dictionary<String, String>();
            //2阶频繁项的生成，需要借助邻接矩阵来完成
            if (pattern.Length == 2)
            {
                dic.Add(pattern, GenIstances42(pattern));
            }
            else //否则，遍历K-1阶取交集
            {
                dic.Add(pattern, GenInstances(pattern));
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
        private string GenInstances(string pattern)
        {
            string ret = "";
             //对每一个K阶模式，取前K-1项，然后利用邻接矩阵依次判断前K-1项与最后一项实例的交集，有交集那必定是其实例
            string basePattern = pattern.Substring(0, pattern.Length - 1);
            string []instances = instanceSet[basePattern.Length - 1][basePattern].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string instance in instances)
            {
                bool flag = false;
                HashSet<string> hs = new HashSet<string>();
                string[] ins = instance.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string s in ins)
                {
                    HashSet<string> hh = new HashSet<string>();
                    string ss = neighbourhoods[s[0]][s];
                    if (!ss.Contains(pattern.Last())) //当前基础实例无法继续扩展，放弃当前基础实例
                    {
                        hs.Clear();
                        break;
                    }
                    MatchCollection collection = Regex.Matches(ss, pattern.Last() + @"\d+");
                    foreach (Match match in collection)
                    {
                        hh.Add(match.Value);
                    }
                    if (!flag)
                    {
                        hs = hh;
                        flag = true;
                    }
                    else
                    {
                        hs.IntersectWith(hh);
                        if (hs.Count == 0)  //交集中出现一次空集，则必不是实例，退出
                        {
                            break;
                        }
                    }
                }
                foreach (string sh in hs)
                {
                    ret = ret + instance + ',' + sh + ';';
                }
            }
            return ret.EndsWith(";") ? ret.Substring(0, ret.Length - 1) : ret;
        }

        //2阶项实例生成
        private string GenIstances42(string pattern)
        {
            string ret = "";
            char mainFeature = pattern[0];
            string []instances = instanceSet[0]["" + mainFeature].Split(new char[]{';'},StringSplitOptions.RemoveEmptyEntries);
            foreach (string sh in instances)
            {
                string relations = neighbourhoods[sh[0]][sh];
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

    }
}
