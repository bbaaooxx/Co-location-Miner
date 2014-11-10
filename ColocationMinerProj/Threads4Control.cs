/**
 *  对组件的跨线程调用的代码都在这里
 *  Copyright:Briant@ynu
 * 
 * **/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ColocationMiner
{
    sealed class Threads4Control //不可继承，单例模式，整个系统只维护一个实例即可
    {
        private static Threads4Control _instance;

        private Threads4Control()
        {

        }

        public static Threads4Control GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Threads4Control();
            }
            return _instance;
        }

        //******************此代理以及Invoke过程为在RichTextBox中AppendText所用

        delegate void AppendTextCallback(RichTextBox rtBox, string text); //委托，线程中用于对组件信息的显示和回调

        public void AppendText(RichTextBox rtBox, String text)
        {
            if (rtBox.InvokeRequired)
            {
                AppendTextCallback s = new AppendTextCallback(AppendText);
                rtBox.Invoke(s, new Object[] {rtBox, text });
            }
            else
            {
                rtBox.AppendText(text);
                rtBox.ScrollToCaret();
            }
        }

        //******************以下代码为跨线程中对RichTextBox设置内容所用
        delegate void SetTextCallback(RichTextBox rtBox, string text); //委托，线程中用于对组件信息的显示和回调

        public void SetText(RichTextBox rtBox, String text)
        {
            if (rtBox.InvokeRequired)
            {
                SetTextCallback s = new SetTextCallback(SetText);
                rtBox.Invoke(s, new Object[] { rtBox, text });
            }
            else
            {
                rtBox.Clear();
                rtBox.AppendText(text);
                rtBox.ScrollToCaret();
            }
        }

        //******************以下代码为跨线程中对hiddenLabel设置内容所用
        delegate void SetHiddenCallback(Label label, string text); //委托，线程中用于对组件信息的显示和回调

        public void SetHidden(Label label, String text)
        {
            if (label.InvokeRequired)
            {
                SetHiddenCallback s = new SetHiddenCallback(SetHidden);
                label.Invoke(s, new Object[] { label, text });
            }
            else
            {
                label.Text = text;
            }
        }

        //******************以下代码为在跨线程中状态栏状态显示的调用
        delegate void SetStatusCallback(StatusStrip status, int value,string info); //委托，线程中用于对组件信息的显示和回调

        public void SetStatus(StatusStrip status, int value,string info)
        {
            if (status.InvokeRequired)
            {
                SetStatusCallback s = new SetStatusCallback(SetStatus);
                status.Invoke(s, new Object[] { status, value, info });
            }
            else
            {
                status.Items[0].Text = "" + value + "%";
                (status.Items[1] as ToolStripProgressBar).Value = value;
                status.Items[2].Text = info;
                status.Refresh();
            }
        }

        //*******************以下代码为画图区组件添加图例时的调用
        delegate void AddLegendCallback(Chart chart, Series series); //委托，线程中用于对组件信息的显示和回调

        public void AddLegend(Chart chart, Series series)
        {
            if (chart.InvokeRequired)
            {
                AddLegendCallback s = new AddLegendCallback(AddLegend);
                chart.Invoke(s, new Object[] { chart, series });
            }
            else
            {
                chart.Series.Add(series);
                chart.Invalidate();
            }
        }


        //*******************以下代码为画图区组件添加点时的调用
        delegate void AddPointCallback(Chart chart, string feature, DataPoint point); //委托，线程中用于对组件信息的显示和回调

        public void AddPoint(Chart chart, string feature, DataPoint point)
        {
            if (chart.InvokeRequired)
            {
                AddPointCallback s = new AddPointCallback(AddPoint);
                chart.Invoke(s, new Object[] { chart, feature, point });
            }
            else
            {
                chart.Series[feature].Points.Add(point);
                chart.Invalidate();
            }
        }

        //*************以下代码为跨线程中对下拉框设置内容时的调用
        delegate void addItemCallBack(ComboBox combo, string text);

        public void AddItem(ComboBox combo, string text)
        {
            if (combo.InvokeRequired)
            {
                addItemCallBack s = new addItemCallBack(AddItem);
                combo.Invoke(s, new Object[] { combo, text });
            }
            else
            {
                combo.Items.Add(text);
            }
        }

    }
}
