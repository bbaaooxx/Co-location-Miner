/**
 *  随机生成数据窗体
 *  Copyright Briant@ynu
 **/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColocationMiner
{
    public partial class RandomDataForm : Form
    {
        public int FeatureCount{get; private set;}   //特征数量属性
        public int MaxInsPerFeature { get; private set; } //每个特征最大实例数
        public RandomDataForm()
        {
            InitializeComponent();
            FeatureCount = Consts.CONSTS_FEATURECOUNT;
            MaxInsPerFeature = Consts.CONSTS_MAXINSTANCE;
            this.featureText.Text = Convert.ToString(FeatureCount);
            this.instanceText.Text = Convert.ToString(MaxInsPerFeature);
            this.submitButton.DialogResult = DialogResult.OK; //响应式
        }

        //设置这种方法是为了防止输入不合法的字符或者是越界的数字
        private void featureText_Leave(object sender, EventArgs e)
        {
            //尝试解析所写的字符串，转化为整型
            int featureCount = 0;
            Int32.TryParse(featureText.Text, out featureCount);
            //如果不满足条件，则重置为默认值
            if (!(featureCount >= 3 && featureCount <= 25))
            {
                FeatureCount = Consts.CONSTS_FEATURECOUNT;
                featureText.Text = Convert.ToString(FeatureCount);
            }
            else
            {
                FeatureCount = featureCount;
            }
        }

        private void instanceText_Leave(object sender, EventArgs e)
        {
            //尝试解析所写的字符串，转化为整型
            int maxInsPerFeature = 0;
            Int32.TryParse(instanceText.Text, out maxInsPerFeature);
            //如果不满足条件，则重置为默认值
            if (!(maxInsPerFeature >= 2 && maxInsPerFeature <= 10000))
            {
                MaxInsPerFeature = Consts.CONSTS_MAXINSTANCE;
                instanceText.Text = Convert.ToString(MaxInsPerFeature);
            }
            else
            {
                MaxInsPerFeature = maxInsPerFeature;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void submitButton_Click(object sender, EventArgs e)
        {

        }

    }
}
