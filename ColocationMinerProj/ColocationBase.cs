/**
 *  Colocation两大经典算法的基类，封装了大部分的公共操作,这是一个抽象类，具体实例生成方式需要各自的子类去完成
 *  Copyright:Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    abstract class ColocationBase
    {
        protected double minParticipate; //最小参与度
        protected double minCondition; //最小条件概率
        protected double minDistance; //关系R，欧氏距离
        protected int range;//范围
        protected List<char> features = new List<char>(); //空间特征集
        protected List<List<string>> frequentSet = new List<List<string>>(); //频繁项集
        protected Dictionary<string, SpaceItem> instances = new Dictionary<string, SpaceItem>(); //原始实例集
        protected Dictionary<char, int> countPerFeature = new Dictionary<char, int>(); //每个特征的个数
        protected Dictionary<string, double> participates = new Dictionary<string, double>(); //参与度矩阵
        protected RichTextBox output;
        public RichTextBox FrequentOutput { set; private get; } //频繁项信息输出源
        public RichTextBox InstancesOutput { set; private get; } //实例信息输出源
        public RichTextBox RuleOutput { set; private get; } //规则信息输出源
        private string []contents; //数据内容
        private string[] headLine; //属性内容
        

        //构造方法
         public ColocationBase(RichTextBox output, string[] contents, string []headLine)
         {
             this.output = output;
             this.contents = contents;
             this.headLine = headLine;  
         }

        //抽象方法

        //生成实例和频繁项相关，返回值表示pattern是否为频繁项
        public abstract bool GenInstancesAndFrequent(string pattern);

        //初始化1阶实例集,将实例加入到实例集中
        public abstract void InitInstanceSet(SpaceItem item);

        //返回某个模式下的所有的实例集字符串形式
        public abstract string GetInstanceFormat(string pattern);

        //清空K阶实例的所有内容
        public abstract void ClearInstance(int k);

        //从实例集中计算每个特征下相应的实例个数（‘A’， 100）
        public abstract Dictionary<char, int> CalcInsCountPerFeature();

        protected virtual void ReadHeadLine(string[] headLine)
        {
            minParticipate = Convert.ToDouble(headLine[0]);
            minCondition = Convert.ToDouble(headLine[1]);
            minDistance = Convert.ToDouble(headLine[2]);
            range = Convert.ToInt32(headLine[3]); 
        }

        protected void SetInfo(string text)
        {
            Threads4Control.GetInstance().AppendText(output, text);
        }

        protected void SetFrequent(string text)
        {
            Threads4Control.GetInstance().AppendText(FrequentOutput, text);
        }

        protected void SetInstance(string text)
        {
            Threads4Control.GetInstance().AppendText(InstancesOutput, text);
        }

        protected void SetRule(string text)
        {
            Threads4Control.GetInstance().AppendText(RuleOutput, text);
        }

        protected void setCurrStatus(string text)
        {
            DataCache.statusInfo = text;
        }

        //读数据信息
        public virtual void ReadData(string[] contents)
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
                }
            }
            catch
            {
                throw new Exception("Bad formated data!");
            }
            
        }

        //初始化操作，可以被覆盖
        public virtual void Init()
        {
            countPerFeature = CalcInsCountPerFeature();
            //候选1项集初始化，频繁一项集初始化
            setCurrStatus("Initialize 1-size prevalent patterns:0");
            List<String> hs = new List<String>();
            features.Sort();
            foreach (char c in features)
            {
                hs.Add("" + c);
            }
            frequentSet.Add(hs);
        }

        public SpaceItem GenSpaceItem(string lines)
        {
            SpaceItem item = CommonOpera.GenSpaceItem(lines);
            if (!features.Contains(item.Feature))
            {
                features.Add(item.Feature);
            }
            return item;
        }
        //执行过程，主执行流程
        public virtual void Run()
        {
            try
            {
                SetInfo("Reading data ...\n");
                DateTime dt1 = DateTime.Now;
                ReadHeadLine(headLine);
                ReadData(contents);
                Init();
                DateTime dt2 = DateTime.Now;
                TimeSpan ts = dt2 - dt1;
                Threads4Control.GetInstance().AppendText(output, "Initialization finished,cost:" + Math.Round(ts.TotalSeconds, 2) + "s\n");
                SetInfo("Main mtehod start...,feature count:" + features.Count + ";instance count:" + instances.Count +
                    ";range:" + range + "*" + range + "\n");
                dt1 = DateTime.Now; //这个用来计时的
                int k = 1; //从第二阶开始算，第一阶在Init初始化的时候已经完成了
                while (GenFrequent(++k)) ; //循环过程，当没有频繁项的时候终止
                dt2 = DateTime.Now;
                ts = dt2 - dt1;
                SetInfo("Total cost:" + Math.Round(ts.TotalSeconds, 2) + "s\n");
                PrintMaxInstances();
                SetInfo("Now Printing rules, please wait!\n");
                SetInfo("If you do not need any rule information, Press Ctrl+Shift+C to interrupt current main thread!\n");
                PrintRule();
                SetInfo("Main thread run over !\n");
                DataCache.statusInfo = "Program run over, welcome for using:100";
                DataCache.isEnd = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error while running:" + ex.Message);
            }
        }

        //打印最大频繁项的实例信息
        private void PrintMaxInstances()
        {
            int maxLen = frequentSet.Count - 1;
            SetInstance("" + (maxLen + 1) + "-size prevalents \t Instances\n");
            foreach (string s in frequentSet[maxLen])
            {
                SetInstance(s + '\t' + GetInstanceFormat(s) + '\n');
            }
        }

        //生成K阶频繁项，由k-1阶生成，如果为false表示当前阶无频繁项，亦作为主循环终止的条件
        protected virtual bool GenFrequent(int k)
        {
            SetInfo("Prceeding" + k + "-size prevalent patterns!\n");
            string frequentStr = "";
            //string instanceStr = "";
            if (k - 3 >= 0) 
            {
                ClearInstance(k - 3);
            }
            bool ret = false;
            List<String> hs1 = frequentSet.ElementAt(k - 2);
            List<String> hs2 = frequentSet.ElementAt(k - 2);
            List<string> hs = new List<string>();
            string sh = "";
            if (k == 2)  //k=2时只是简单的合并，需要与>2的情况区别开来
            {
                for (int i = 0; i < hs1.Count - 1; i++)
                {
                    string sh1 = hs1[i];
                    for (int j = i + 1; j < hs2.Count; j++)
                    {
                        string sh2 = hs2[j];
                        setCurrStatus("Joining " + sh1 + "and " + sh2 + ":" + (k - 1) * 100 / features.Count);
                        sh = GenCandidata(sh1, sh2);
                        if (!String.IsNullOrEmpty(sh))
                        {
                            if (GenInstancesAndFrequent(sh))
                            {
                                hs.Add(sh);
                                frequentStr = frequentStr + sh + '\t' + participates[sh] + '\n';
                                //instanceStr = instanceStr + sh + '\t' + GetInstanceFormat(sh) + '\n';
                                ret = true;
                            }
                        }
                    }
                }
            }
            else  //否则。取前k-2项相同的K-1项，进行合并
            {
                for (int i = 0; i < hs1.Count - 1; i ++ )
                {
                    string sh1 = hs1[i];
                    for (int j = i + 1; j < hs2.Count - 1; j++ )
                    {
                        string sh2 = hs2[j];
                        if (!sh1.Substring(0, sh1.Length - 1).Equals(sh2.Substring(0, sh2.Length - 1)))
                        {
                            break;  //按照排序结果，之后的序列肯定不满足要求，提前终止
                        }
                        //如果前k-2项相等且最后一个特征满足相应序号关系 
                        setCurrStatus("Joining " + sh1 + "and " + sh2 + ":" + (k - 1) * 100 / features.Count);
                        sh = GenCandidata(sh1, sh2);
                        if (!String.IsNullOrEmpty(sh))
                        {
                            if (GenInstancesAndFrequent(sh))
                            {
                                hs.Add(sh);
                                frequentStr = frequentStr + sh + '\t' + participates[sh] + '\n';
                                //instanceStr = instanceStr + sh + '\t' + GetInstanceFormat(sh) + '\n';
                                ret = true;
                            }
                        } 
                    }
                }
            }
            if (ret)
            {
                frequentSet.Insert(k - 1, hs);
                SetFrequent(k + "-size prevalent patterns:\n" + frequentStr);
                //SetInstance(k + "阶频繁项 \t 实例 \n" + instanceStr);
            }
            return ret;
        }

        //生成候选项，此候选项如果为K阶，其k-1阶如果不存在则可以提前剪枝，注意返回Empty则表示改候选项被提前剪枝了
        protected string GenCandidata(string featureA, string featureB)
        {
            char cA = featureA.Last();
            char cB = featureB.Last();
            string joinPattern = featureA.Substring(0, featureA.Length - 1) + cA + cB;
            if (IsSubPatternsCircle(joinPattern))
            {
                return joinPattern;
            }
            return String.Empty;
        }
        //打印频繁项
        public virtual void PrintRule()
        {
            setCurrStatus("Welcome, now Printing is on, please wait:100");
            int k = frequentSet.Count;
            if (frequentSet.ElementAt(k - 1) == null)
            {
                k--;
            }
            SetRule("rules \t contidition probability\n");
            for (int i = 1; i < k; i++)
            {
                foreach (String sh in frequentSet.ElementAt(i))
                {
                    GenAndPrintRule(sh);
                }
            }
        }

        //生成关联规则并打印出关联规则和相应的条件概率
        protected void GenAndPrintRule(string sh)
        {
            for (int i = 1; i < (1 << sh.Length) - 1; i++) //全排列个数，去掉前置为0个和后置为0个的两个组合
            {
                String left = "";
                String right = "";
                for (int j = 0; j < sh.Length; j++) //按位与操作，得到全排列
                {
                    if (((1 << j) & i) != 0)
                    {
                        left = left + sh.ElementAt(j);
                    }
                }
                for (int j = 0; j < sh.Length; j++) //得到上面字符之外剩余的排列
                {
                    if (((1 << j) & ~i) != 0)
                    {
                        right = right + sh.ElementAt(j);
                    }
                }
                CalcAndPrintRule(left, right, sh);
            }
        }

        protected virtual void CalcAndPrintRule(string left, string right, string sh)
        {
            Double condition = CalcCondition(left, sh);
            if (condition >= minCondition)
            {
                SetRule(left + "->" + right + "\t" + Math.Round(condition, 2) + "\n");
            }
        }

        //计算条件概率 前置 字符串
        private double CalcCondition(String left, String full)
        {
            //int prevLen = left.Length;
            //int len = full.Length;
            //String instanceStr = GetInstanceFormat(full);
            //String[] instances = instanceStr.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            //HashSet<String> hs = new HashSet<string>();
            //foreach (String ch in instances)
            //{
            //    string[] ss = ch.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
            //    String str = "";
            //    for (int i = 0; i < ss.Length; i++)
            //    {
            //        if (left.Contains(ss[i].ElementAt(0)))
            //        {
            //            str = str + ss[i] + ',';
            //        }
            //    }
            //    str = str.Substring(0, str.Length - 1);
            //    hs.Add(str);
            //}
            //String sortedStr = "";
            //foreach (Char c in full)
            //{
            //    if (left.Contains(c))
            //    {
            //        sortedStr = sortedStr + c;
            //    }
            //}
            //return (Double)hs.Count / GetInstanceFormat(sortedStr).Split(';').Length;
            return left.Length == 1 ? Math.Round(participates[full], 2) : Math.Round(participates[full] / participates[left], 2);
        }

        //计算参与度
        public virtual double CalcParticipate(Dictionary<string, string> dic, string sh)
        {
            if (!dic.ContainsKey(sh))
            {
                return 0;
            }
            String instancesStr = dic[sh];
            double min = 1;
            string[] instances = instancesStr.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
            List<HashSet<String>> lhs = new List<HashSet<string>>();
            for (int i = 0; i < sh.Length; i++)
            {
                HashSet<String> hs = new HashSet<string>();
                lhs.Add(hs);
            }
            foreach (string s in instances)
            {
                String[] ss = s.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < ss.Length; i++)
                {
                    lhs.ElementAt(i).Add(ss[i]);
                }
            }
            for (int i = 0; i < sh.Length; i++)
            {
                double d = (Double)lhs.ElementAt(i).Count / countPerFeature[sh[i]];
                if (d < min)
                {
                    min = d;
                }
            }
            return Math.Round(min, 2);
        }

        //候选项剪枝
        private bool IsSubPatternsCircle(string pattern)
        {
            //后两项无须删，因为已经被组合过了，所以2项以下肯定为团原因就在这里
            for (int i = 0; i < pattern.Length - 2; i++)
            {
                string subPattern = pattern.Remove(i, 1);
                if (!frequentSet[subPattern.Length - 1].Contains(subPattern))
                {
                    return false;
                }
            }
            return true;
        }

        protected int GetDivResult(double x)
        {
            return (int)(x / minDistance);
        }
    }
}
