/**
 *  欢迎界面，停留2秒之后进入主界面
 *  Copyright Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    public partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();
            pictureBox.ImageLocation = Consts.CONSTS_IMG_PREFACE; //设置加载图片的地址
        }

        private void pictureBox_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Thread.Sleep(2000);  //停留2秒之后隐藏并弹出主窗体
            this.Hide();
            MainForm form = new MainForm();
            form.Show();
            //主窗体关闭时触发欢迎界面被关闭
            form.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CloseAfter); ;
        }

        private void CloseAfter(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }
    }
}
