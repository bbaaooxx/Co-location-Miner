/**
 * 系统所有相关的常亮都在这里定义
 * Copyright Briant@ynu
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColocationMiner

{
    sealed class Consts
    {
        //------------------------首页图片----------------------------------
        public const string CONSTS_IMG_PREFACE = "preface.jpg"; //启动画面

        //------------------------初始化默认数据-----------------------------
        public const string CONSTS_FILE_NEWNAME = "Noname.data"; //新建文件默认名
        public const string CONSTS_CONF_NEWNAME = "Noname.conf";//新建配置文件默认名
        public const double CONSTS_PATICIPATE = 0.25;   //默认参与度
        public const double CONSTS_CONFIDENCE = 0.5;    //默认条件概率
        public const int CONSTS_NEW_FEATURECOUNT = 5; //默认新建的特征数量
        public const int CONSTS_FEATURECOUNT = 10; //随机生成的默认特征数量
        public const int CONSTS_NEW_MAXINSTANCE = 8; //默认新建的特征最大实例数
        public const int CONSTS_MAXINSTANCE = 100; //随机生成的默认特征数量
        public const double CONSTS_DISTANCE = 2.5; //新建的默认距离阈值
        public const string CONSTS_RANGE = "10";  //新建的默认范围X*Y

        public const string CONSTS_DATA_DRAW_BEGIN = "begin";
        public const string CONSTS_DATA_DRAW_END = "end";

    }
}
