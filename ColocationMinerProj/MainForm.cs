/**
 * 主面板
 * Copyright Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ColocationMiner
{
    public partial class MainForm : Form
    {
        //随机生成数据线程，为了便于统一处理，防止多个数据线程打乱输出结果
        //系统只维护一个随机数据生成线程，如果当前线程未完成且出现生成操作请求，中断当前线程并重新启动该线程
        private Thread genRandomDataThread;

        //画图线程，设为全局便于操作，如果设为局部，当频繁生成数据且前一个画图线程没有完成会产生错误
        //系统只维护一个画图线程，如果当前需要刷新画图但是画图线程还没有处理完毕，则杀死该线程重绘
        private Thread drawGraphThread;

        //主体挖掘算法线程，此线程的前置是数据线程已经生成完毕，并且当前无活动的主体线程运行
        private Thread mainThread;

        //获取实时状态的线程
        private Thread curStatusThread;

        //一个三位数代表了结果的过滤信息，百位永远是1，表示显示频繁项；十位为1表示显示规则，为0不显示；个位为1表示
        //打印所有实例信息，为0表示只打印最长频繁项对应的实例集
        private int resultFlag = 110; 

        //配置文件中的4行信息值
        private string[] headLines;

        private bool isNew = true;

        //实例集
        private Dictionary<string, SpaceItem> instances = new Dictionary<string, SpaceItem>();

        //表格
        private string[,] grids;

        public MainForm()
        {
            InitializeComponent();
            DoCreateNew();
        }
        
        private void newFileMItem_Click(object sender, EventArgs e)
        {
            isNew = true;
            DoCreateNew();
        }

        private void randomDataMItem_Click(object sender, EventArgs e)
        {
            isNew = false;
            DoGenRandomData();
        }

        private void fileRTBox_TextChanged(object sender, EventArgs e)
        {
            if (!fileTPage.Text.StartsWith("*"))
            {
                fileTPage.Text = "*" + fileTPage.Text;
            }
        }

        private void saveMItem_Click(object sender, EventArgs e)
        {
            DoSave();
        }

        private void openFileMItem_Click(object sender, EventArgs e)
        {
            DoOpen();
        }

        private void hiddenLabel_TextChanged(object sender, EventArgs e)
        {
            if (hiddenLabel.Text.Equals(Consts.CONSTS_DATA_DRAW_BEGIN))
            {
                DodrawGraph();
            }
            else if (hiddenLabel.Text.Equals(Consts.CONSTS_DATA_DRAW_END))
            {
                centerInsCBox.Enabled = true;
            }
        }

        private void exitMItem_Click(object sender, EventArgs e)
        {
            DoExit();
        }

        private void resultMItem_Click(object sender, EventArgs e)
        {
            DoFilterResult();
        }
     
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (fileTPage.Text.StartsWith("*"))
            {
                if (MessageBox.Show("File has not been saved，exit?", "warn", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {

                    KillAllThreads();
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                KillAllThreads();
            }
        }

        private void fulljoinMItem_Click(object sender, EventArgs e)
        {
            DoRun(0);
        }

        private void joinLessMItem_Click(object sender, EventArgs e)
        {
            DoRun(1);
        }

        private void mainChart_DoubleClick(object sender, EventArgs e)
        {

        }

        private List<SpaceItem> GetJoinList(string instance)
        {
            List<SpaceItem> ret = new List<SpaceItem>();
            SpaceItem item = instances[instance];
            double distance = Double.Parse(confDataGrid.Rows[2].Cells[1].Value.ToString());
            int x = (int)(item.X / distance);
            int y = (int)(item.Y / distance);
            for (int i = x - 1; i <= x + 1; i++)
            {
                if (i < 0 || i >= grids.GetLength(0))
                {
                    continue;
                }
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (j < 0 || j >= grids.GetLength(1))
                    {
                        continue;
                    }
                    if (grids[i, j] == null)
                    {
                        continue;
                    }
                    foreach (string s in grids[i, j].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        SpaceItem item1 = instances[s];
                        if (CanJoin(item, item1, distance))
                        {
                            ret.Add(item1);
                        }
                    }
                }
            }
            return ret;
        }

        private bool CanJoin(SpaceItem item, SpaceItem item1, double distance)
        {
            return Math.Sqrt(Math.Pow(item1.X - item.X, 2) + Math.Pow(item1.Y - item.Y, 2)) <= distance;
        }

        private void mainChart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            if (this.centerInsCBox.Text.Length == 0)
            {
                return;
            }
            SpaceItem baseItem = instances[centerInsCBox.Text];
            float y1 = (float)e.ChartGraphics.GetPositionFromAxis("Default", AxisName.Y, baseItem.Y);
            float x1 = (float)e.ChartGraphics.GetPositionFromAxis("Default", AxisName.X, baseItem.X);
            List<SpaceItem> items = GetJoinList(centerInsCBox.Text);
            foreach (SpaceItem item in items)
            {
                float y2 = (float)e.ChartGraphics.GetPositionFromAxis("Default", AxisName.Y, item.Y);
                float x2 = (float)e.ChartGraphics.GetPositionFromAxis("Default", AxisName.X, item.X);
                Graphics graph = e.ChartGraphics.Graphics;
                PointF point1 = PointF.Empty;
                PointF point2 = PointF.Empty;             
                point1.X = x1;
                point1.Y = y1;
                point2.X = x2;
                point2.Y = y2;
                point1 = e.ChartGraphics.GetAbsolutePoint(point1);
                point2 = e.ChartGraphics.GetAbsolutePoint(point2);
                graph.DrawLine(new Pen(Color.Blue, 1), point1, point2);
            }
        }

        private void confDataGrid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (DoValidate(e.RowIndex, e.ColumnIndex, (string)e.FormattedValue))
            {
                confDataGrid.Rows[e.RowIndex].ErrorText = String.Empty;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void confDataGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!confTPage.Text.StartsWith("*"))
            {
                confTPage.Text = '*' + confTPage.Text;
            }
        }

        private void multiMItem_Click(object sender, EventArgs e)
        {
            DoRun(2);
        }

        private void centerInsCBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DodrawGraph(centerInsCBox.Text);
        }

        private void breakTMenu_Click(object sender, EventArgs e)
        {
            if (DoBreakMain())
            {
                this.progressRTBox.AppendText("Program interrupted!\n");
                this.statusInfoLabel.Text = "Program run finished!";
            }
        }

        private void hlpMItem_Click(object sender, EventArgs e)
        {
            DoShowHelp();
        }

        private void aboutMItem_Click(object sender, EventArgs e)
        {
            DoShowAbout();
        }

    }
}
