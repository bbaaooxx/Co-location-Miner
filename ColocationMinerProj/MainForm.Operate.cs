/***
 *  主窗体调用方法集合
 *  Copyright Briant@ynu
 * 
 * */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ColocationMiner
{
    partial class MainForm
    {

        //新建文件主过程
        private void DoCreateNew()
        {
            centerInsCBox.Enabled = false;
            centerInsCBox.Items.Clear();
            centerInsCBox.Text = "";
            fileRTBox.Clear();
            confDataGrid.Rows.Clear();
            fileTPage.Text = '*' + Consts.CONSTS_FILE_NEWNAME;
            CreateConf();
            this.headLines = GetHeadLines();
            GenerateData(Consts.CONSTS_NEW_FEATURECOUNT, Consts.CONSTS_NEW_MAXINSTANCE);
        }

        //初始化配置界面
        private void CreateConf()
        {
            confDataGrid.Rows.Add(new string[]{"minimum participate index（0~1）", "" + Consts.CONSTS_PATICIPATE});
            confDataGrid.Rows.Add(new string[] { "minimum condition probability index（0~1）", "" + Consts.CONSTS_CONFIDENCE });
            confDataGrid.Rows.Add(new string[] { "distance thrshold（no more than range*1/3）", "" + Consts.CONSTS_DISTANCE });
            confDataGrid.Rows.Add(new string[] { "range(a square,integer)", "" + Consts.CONSTS_RANGE });
            confTPage.Text = '*' + Consts.CONSTS_CONF_NEWNAME;
        }

        //文件中读取配置信息，如果文件不存在，那么就新建
        private void ReadConf(string fileName)
        {
            confDataGrid.Rows.Clear();
            fileName = fileName.Substring(0, fileName.LastIndexOf('.')) + ".conf";
            if (File.Exists(fileName))
            {
                StreamReader s = File.OpenText(fileName);
                String line = null;
                List<string> ls = new List<string>();
                while ((line = s.ReadLine()) != null)
                {
                    if (!String.IsNullOrWhiteSpace(line))
                    {
                        ls.Add(line);
                    }
                }
                if (ls.Count < 4)
                {
                    throw new Exception("Bad File！");
                }
                confDataGrid.Rows.Add(new string[] { "minimum participate index（0~1）", ls[0] });
                confDataGrid.Rows.Add(new string[] { "minimum condition probability index（0~1）", ls[1] });
                confDataGrid.Rows.Add(new string[] { "distance thrshold（no more than range*1/3）", ls[2] });
                confDataGrid.Rows.Add(new string[] { "range(a square,integer)", ls[3] });
                confTPage.Text = fileName;
            }
            else
            {
                CreateConf();
                confTPage.Text = fileName;
            }
        }

        //随机生成数据
        //!!TODO 实际上坐标可以取小数，需要时可以放开，这里取整数是为了显示和运算的方便
        private void GenerateData(int featureCount, int maxInsCount)
        {
            Threads4Control.GetInstance().SetStatus(statusStrip, 0, "Generating random data");
            int[] range = GetRange();
            //控件本身的刷新速度很慢，如果一条条的输出会拖慢速度，整体一起输出界面会出现卡顿
            //所以设置每100条数据刷新界面1次，可以加快生成速度
            string content = "";
            int index = 0;
            for (int i = 0; i < featureCount; i++)
            {
                char featureRepresent = Convert.ToChar(65 + i);  //生成特征代号
                Random rand = new Random(DateTime.Now.Millisecond + i << 9);
                int instanceCount = rand.Next(3, maxInsCount); //随机生成该特征的实例个数
                for (int j = 0; j < instanceCount; j++)
                {
                    double x = rand.NextDouble() * (range[0]) ;
                    double y = rand.NextDouble() * (range[1]) ;
                    content = content + (j + 1) + '\t' + featureRepresent + '\t' + Math.Round(x, 2) + '\t' + Math.Round(y, 2) + "\n";
                    if ((++index) % 100 == 0)
                    {
                        Threads4Control.GetInstance().AppendText(fileRTBox, content);
                        content = "";
                        index = 0;
                    }
                }
                //设置状态栏的相关信息
                int value = (int)((double)(i + 1) / (double)featureCount * 100);
                string info = featureRepresent + "'s random data generated";
                Threads4Control.GetInstance().SetStatus(statusStrip, value, info);
            }
            if (index > 0)
            {
                Threads4Control.GetInstance().AppendText(fileRTBox, content);
            }
            Threads4Control.GetInstance().SetStatus(statusStrip, 100, "Data generated");
            //触发画图线程
            Threads4Control.GetInstance().SetHidden(hiddenLabel, Consts.CONSTS_DATA_DRAW_BEGIN);
        }

        //为了使用异步方法的参数，请注意参数必须为RandomDataForm类
        private void GenerateData(object o)
        {
            RandomDataForm randForm = o as RandomDataForm;
            GenerateData(randForm.FeatureCount, randForm.MaxInsPerFeature);
        }

        //获取范围信息
        private int[] GetRange()
        {
            int[] ret = new int[2]; //ret[0]为X坐标范围上限 ret[1]为Y坐标范围上限
            string rangeStr = headLines[3].Split(' ')[0]; //第四行为范围字符串
            if (rangeStr.Contains('*'))
            {
                ret[0] = Convert.ToInt32(rangeStr.Split('*')[0]);
                ret[1] = Convert.ToInt32(rangeStr.Split('*')[1]);
            }
            else
            {
                ret[0] = ret[1] = Convert.ToInt32(rangeStr.Trim());
            }
            return ret;
        }

        //生成随机数据操作
        private void DoGenRandomData()
        {
            if (genRandomDataThread != null && genRandomDataThread.IsAlive)
            {
                if (MessageBox.Show("Data is being generated，interruput?", "warn", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    genRandomDataThread.Abort();
                    MessageBox.Show("interrupted!");
                    return;
                }
            }
            RandomDataForm randomForm = new RandomDataForm();
            if (randomForm.ShowDialog(this) == DialogResult.OK)
            {
                headLines = GetHeadLines();
                if (randomForm.FeatureCount * randomForm.MaxInsPerFeature > GetRange()[0] * GetRange()[1])
                {
                    MessageBox.Show("Your range property is too small to generate so much data,please return to modify certain properties!");
                    fileTControl.SelectTab(confTPage);
                    return;
                }
                if (genRandomDataThread != null && genRandomDataThread.IsAlive)
                {
                    genRandomDataThread.Abort(); //杀死还没有运行完毕的随机数据生成线程
                }
                if (drawGraphThread != null && drawGraphThread.IsAlive)
                {
                    drawGraphThread.Abort(); //杀死还没有运行完毕的画图线程
                }
                centerInsCBox.Enabled = false;
                centerInsCBox.Items.Clear();
                centerInsCBox.Text = "";
                headLines = GetHeadLines();
                fileRTBox.Clear();
                ParameterizedThreadStart thStart = new ParameterizedThreadStart(GenerateData);
                genRandomDataThread = new Thread(thStart);
                genRandomDataThread.Start(randomForm);
            }
        }

        //画图开始，这个过程为异步过程，单独开一个线程来处理，因为生成数据或者是在算法运行时，数据处理的速度与
        //图像生成的速度相差太大，所以这部分单独开一个线程来独立完成，不拖主线程的速度
        private void DodrawGraph()
        {
            if (drawGraphThread != null && drawGraphThread.IsAlive)
            {
                drawGraphThread.Abort();
            }
            this.mainChart.Series.Clear();
            this.instances.Clear();
            centerInsCBox.Items.Clear();
            centerInsCBox.Text = "";
            double distance = Convert.ToDouble(confDataGrid.Rows[2].Cells[1].Value);
            int len1 =(int)(GetRange()[0] / distance) + 1;
            int len2 = (int)(GetRange()[1] / distance) + 1;
            this.grids = new string[len1, len2];
            string[] lines = fileRTBox.Lines;
            ParameterizedThreadStart thStart = new ParameterizedThreadStart(DrawGraph);
            drawGraphThread = new Thread(thStart);
            drawGraphThread.Start(lines);
        }

        //线程处理方法，注意o应为string[]，即文本区文本
        private void DrawGraph(object o)
        {
            string[] lines = o as string[];
            int len = lines.Length;  //太长则不予全部显示
            Dictionary<string, DataPoint> dic = new Dictionary<string, DataPoint>(); //均值坐标点
            Dictionary<string, int> countF = new Dictionary<string, int>(); //记录个数
            string str = "";
            for (int i = 0; i < lines.Length; i++)
            {
                //解析每行的字符串
                string[] s = lines[i].Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length == 0) continue; //空串不处理，这里几乎不会触发，注意原始数据的规格化
                SpaceItem item = CommonOpera.GenSpaceItem(lines[i]);
                instances.Add(item.Format(), item); //加入实例集
                Threads4Control.GetInstance().AddItem(centerInsCBox, item.Format()); //加入下拉框
                double distance = Convert.ToDouble(confDataGrid.Rows[2].Cells[1].Value);
                int xx = (int)(item.X / distance);
                int yy = (int)(item.Y / distance);
                grids[xx, yy] += (item.Format() + ',');
                if (!s[1].Equals(str)) //添加图例
                {
                    str = Convert.ToString(s[1]);
                    Series series = new Series(str);
                    series.ChartType = SeriesChartType.Point;
                    series.MarkerSize = 10;
                    Threads4Control.GetInstance().AddLegend(mainChart, series);
                }
                //生成对应的点并添加到图例中
                int index = Int32.Parse(s[0]);
                double x = Double.Parse(s[2]);
                double y = Double.Parse(s[3]);
                if (len <= 100)
                {
                    DataPoint point = new DataPoint(x, y);
                    point.Label = s[1] + '.' + index;
                    Threads4Control.GetInstance().AddPoint(mainChart, s[1], point);
                }
                else
                {
                    if (!dic.ContainsKey(s[1]))
                    {
                        DataPoint point = new DataPoint(x, y);
                        point.Label = s[1];
                        dic.Add(s[1], point);
                    }
                    else
                    {
                        dic[s[1]].XValue += x;
                        dic[s[1]].YValues[0] += y;
                    }
                    if (!countF.ContainsKey(s[1]))
                    {
                        countF.Add(s[1], 1);
                    }
                    else
                    {
                        countF[s[1]]++;
                    }
                }
            }
            foreach (string s in dic.Keys)
            {
                dic[s].XValue = Math.Round(dic[s].XValue / countF[s], 2);
                dic[s].YValues[0] = Math.Round(dic[s].YValues[0] / countF[s], 2);
                Threads4Control.GetInstance().AddPoint(mainChart, s, dic[s]);
            }
            Threads4Control.GetInstance().SetHidden(hiddenLabel, Consts.CONSTS_DATA_DRAW_END);
        }

        //线程处理方法，画中心点的邻接图
        private void DrawCenter(object o)
        {
            string instance = o as string;
            HashSet<char> hs = new HashSet<char>();
            SpaceItem item = instances[instance];
            double distance = Convert.ToDouble(confDataGrid.Rows[2].Cells[1].Value);
            int x =(int) (item.X / distance);
            int y = (int)(item.Y / distance);
            //寻找自身及其他8个方向的点并添加
            for (int i = -1; i <= 1; i++)
            {
                if (x + i < 0 || x + i > grids.GetLength(0) - 1)
                {
                    continue;
                }
                for (int j = -1; j <= 1; j++)
                {
                    if (y + j < 0 || y + j > grids.GetLength(1) - 1)
                    {
                        continue;
                    }
                    string insStrs = grids[x + i, y + j];
                    if (insStrs != null)
                    {
                        foreach (string sh in insStrs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            SpaceItem sp = instances[sh];
                            if (!hs.Contains(sp.Feature)) //添加图例
                            {
                                hs.Add(sp.Feature);
                                Series series = new Series("" + sp.Feature);
                                series.ChartType = SeriesChartType.Point;
                                series.MarkerSize = 10;
                                Threads4Control.GetInstance().AddLegend(mainChart, series);
                            }
                            //生成对应的点并添加到图例中
                            DataPoint point = new DataPoint(sp.X, sp.Y);
                            point.Label = "" + sp.Feature + '.' + sp.Index;
                            Threads4Control.GetInstance().AddPoint(mainChart, "" + sp.Feature, point);
                        }
                    }
                }
            }
        }

        //画以实例instance为中心的邻接关系图
        private void DodrawGraph(string instance)
        {
            this.mainChart.Series.Clear();
            this.mainChart.ChartAreas[0].AxisX.Interval = Double.Parse(GetHeadLines()[2]);
            this.mainChart.ChartAreas[0].AxisY.Interval = Double.Parse(GetHeadLines()[2]);
            ParameterizedThreadStart thStart = new ParameterizedThreadStart(DrawCenter);
            drawGraphThread = new Thread(thStart);
            drawGraphThread.Start(instance);
        }

        private string[] GetHeadLines()
        {
            string[] ret = new string[4];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = (string)confDataGrid.Rows[i].Cells[1].Value;
            }
            return ret;
        }


        //保存文件方法，如果当前的文件名是没有命名的，则弹出框要求输入文件名并保存；如果是已经保存过的或者
        //是刚打开的文件，只需要保存即可(消除*)
        private void DoSave()
        {
            if (genRandomDataThread != null && genRandomDataThread.IsAlive)
            {
                MessageBox.Show("Data is being generated，please wait until it finished!");
                return;
            }
          
            if (!File.Exists(fileTPage.Text.StartsWith("*") ? fileTPage.Text.Substring(1) : fileTPage.Text))  //弹框
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileRTBox.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.PlainText);
                    SaveConf(saveFileDialog.FileName);
                    fileTPage.Text = saveFileDialog.FileName;
                }
            }
            else
            {
                if (fileTPage.Text.StartsWith("*"))  //没有发生内容改变,若配置信息有变，只存配置信息
                {
                    fileRTBox.SaveFile(fileTPage.Text.Substring(1), RichTextBoxStreamType.PlainText);
                    SaveConf(saveFileDialog.FileName);
                    fileTPage.Text = fileTPage.Text.Substring(1);
                }
                else
                {
                    if (confTPage.Text.StartsWith("*"))
                    {
                        SaveConf(confTPage.Text.Substring(1));
                    }
                }
                

            }
        }

        private void SaveConf(string fileName)
        {
            fileName = fileName.Substring(0, fileName.LastIndexOf('.')) + ".conf";
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            foreach (string sh in GetHeadLines())
            {
                sw.WriteLine(sh + '\n');
            }
            sw.Close();
            fs.Close();
            confTPage.Text = fileName;
        }

        //打开文件操作，如果当前正在生成数据，则不允许打开文件，打开文件之后，图像会随之刷新
        //如果打开的文件不存在对应的配置文件，则采用默认格式
        private void DoOpen()
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (genRandomDataThread != null && genRandomDataThread.IsAlive)
                {
                    MessageBox.Show("Data is being generated，please wait until it finished!");
                    return;
                }
                if (drawGraphThread != null && drawGraphThread.IsAlive)
                {
                    drawGraphThread.Abort();
                }
                fileTPage.Text = openFileDialog.FileName;
                fileRTBox.LoadFile(openFileDialog.FileName, RichTextBoxStreamType.PlainText);
                fileTPage.Text = openFileDialog.FileName;
                ReadConf(openFileDialog.FileName);
                this.headLines = GetHeadLines();
                DodrawGraph();

            }
        }

        //当前有任何线程在运行时都会被要求终止，如果有未被保存的文件，提示是否保存
        private void DoExit()
        {
            Close();
        }

        //结果过滤
        private void DoFilterResult()
        {
           
        }

        //停止所有读线程
        private void StopReadThreads()
        {
            if ( curStatusThread != null && curStatusThread.IsAlive)
            {
                curStatusThread.Abort();
            }
        }

        //结果区重置
        private void ClearResults()
        {
            progressRTBox.Clear();
            frequentRTBox.Clear();
            instanceRTBox.Clear();
            ruleRTBox.Clear();
        }

        //运行方法，index为0为全连接，为1为无连接, 为2为多分辨全连接
        private void DoRun(int index)
        {
            isNew = false;
            if (genRandomDataThread != null && genRandomDataThread.IsAlive)
            {
                MessageBox.Show("Data is being generated，please wait until it finished!");
                return;
            }
            if (mainThread != null && mainThread.IsAlive)
            {
                MessageBox.Show("Data is being generated，please wait until it finished!");
                return;
            }
            StopReadThreads();
            DataCache.Init();
            DataCache.isEnd = false;
            ColocationBase runClass;
            if (index == 0)
            {
                runClass = new FullJoin(progressRTBox, fileRTBox.Lines, GetHeadLines());
            }
            else if (index == 1)
            {
                runClass = new JoinLessA(progressRTBox, fileRTBox.Lines, GetHeadLines());
            }
            else
            {
                runClass = new FullJoin(progressRTBox, fileRTBox.Lines, GetHeadLines());
                (runClass as FullJoin).IsMultiPrunning = true;
            }

            runClass.FrequentOutput = frequentRTBox;
            runClass.InstancesOutput = instanceRTBox;
            runClass.RuleOutput = ruleRTBox;

            //读状态线程同时并发
            curStatusThread = new Thread(new ThreadStart(ReadCurStatus));
            curStatusThread.Start();

            ClearResults();
            mainThread = new Thread(new ThreadStart(runClass.Run));
            mainThread.Start();
        }

        //读取当前状态
        private void ReadCurStatus()
        {
            while (!DataCache.isEnd)
            {
                if (DataCache.statusInfo != null)
                {
                    int index = DataCache.statusInfo.LastIndexOf(':');
                    string info = DataCache.statusInfo.Substring(0, index);
                    int value = Convert.ToInt32(DataCache.statusInfo.Substring(index + 1));
                    Threads4Control.GetInstance().SetStatus(statusStrip, value, info);
                }
                Thread.Sleep(1000);
            }
            Threads4Control.GetInstance().SetStatus(statusStrip, 100, "Main method finished");
        }

        //杀死所有线程
        private void KillAllThreads()
        {
            if (genRandomDataThread != null && genRandomDataThread.IsAlive)
            {
                genRandomDataThread.Abort();
            }
            if (drawGraphThread != null && drawGraphThread.IsAlive)
            {
                drawGraphThread.Abort();
            }
            if (mainThread != null && mainThread.IsAlive)
            {
                mainThread.Abort();
            }
            StopReadThreads();
        }

        //对配置界面的验证方法
        private bool DoValidate(int row, int col, string value)
        {
            if (col != 1)
            {
                return true;
            }
            if (String.IsNullOrWhiteSpace(value))
            {
                confDataGrid.Rows[row].ErrorText = "Should not empty!";
                return false;
            }
            switch (row)
            {

                case 0:
                case 1:
                    double d = 0;
                    if (Double.TryParse(value, out d))
                    {
                        if (d > 0 && d <= 1)
                        {
                            return true;
                        }
                    }
                    break;
                case 2:
                    double d1 = 0;
                    if (Double.TryParse(value, out d1))
                    {
                        if (d1 > 0)
                        {
                            return true;
                        }
                    }
                    break;
                case 3:
                    int i = 0;
                    if (Int32.TryParse(value, out i))
                    {
                        if (i > 0)
                        {
                            return true;
                        }
                    }
                    break;
            }
            confDataGrid.Rows[row].ErrorText = "Error property";
            return false;
        }

        private double[] GetXY(string line)
        {
            string[] positionStr = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            double xi = Convert.ToDouble(positionStr[2]);
            double yi = Convert.ToDouble(positionStr[3]);
            return new double[] { xi, yi };
        }

        //中断主线程
        private bool DoBreakMain()
        {
            if (mainThread != null && mainThread.IsAlive)
            {
                mainThread.Abort();
                StopReadThreads();
            }
            else
            {
                return false;
            }
            return true;
        }

        private void DoShowHelp()
        {
            System.Diagnostics.Process.Start("UserGuide.docx");
        }

        private void DoShowAbout()
        {
            AboutForm box = new AboutForm();
            box.ShowDialog(this);
        }

    }

}
