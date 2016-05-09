﻿/**
#######################################################
期权期货数据整理和存储程序。
利用万德API以及万德TDB数据库，提取期权和期货盘口数据。
并且在这基础上，计算期权盘口价格对应的波动率和delta。
作者：毛衡
时间：2016-03-04
版本：v1.0.0
#######################################################
1、更新了存储50etf数据的部分。
2、修改了tradeDays的读取部分，防止重复从万德数据库中读取
数据。
3、修改了optionInformation的读取部分，防止重复从万德数据
库中读取数据。
作者：毛衡
时间：2016-03-07
版本：v1.0.1
#######################################################
1、从TDB数据库中获取期权数据。
2、根据50etf数据以及期权数据，计算期权对应的希腊值和开仓保证金。
作者：毛衡
时间：2016-03-08
版本：v1.0.2
#######################################################
1、将整理好的TDB期权数据存入本地数据库。
2、修正了duration的数据格式，是的其更贴近现实。
作者：毛衡
时间：2016-03-09
版本：v1.0.3
#######################################################
1、处理了计算波动率的bug，比如遇到期权价格为0或者期权快到期
时候的异常处理。
2、将整理好的IH期货数据存入本地数据库。按合约代码存储，主要
记录当月的数据和下月的数据。
作者：毛衡
时间：2016-03-14
版本：v1.0.4
#######################################################
1、对期权记录数的结构进行更新，添加了标的价格这个字段。
作者：毛衡
时间：2016-04-13
版本：v1.0.5
#######################################################
1、更新商品期货数据存储的类。
2、设计数据库自动生成。
作者：毛衡
时间：2016-04-20
版本：v1.1.0
#######################################################
1、完成将商品期货数据落到本地数据库的工作。
2、记录过程的日志文本。
作者：毛衡
时间：2016-04-21
版本：v1.1.1
#######################################################
1、增加国债期货和股指期货的存储。
2、增加期权的存储。
作者：毛衡
时间：2016-04-22
版本：v1.1.2
#######################################################
1、数据库IP地址的修改。
2、交易日信息从CSV文件中读取。
3、修改数据库结构，字段和索引。
作者：毛衡
时间：2016-04-25
版本：v1.1.3
#######################################################
1、部分bug处理。
作者：毛衡
时间：2016-04-26
版本：v1.1.4
#######################################################
1、修正数据结构格式，并写入目标服务器的IP地址。
作者：毛衡
时间：2016-05-09
版本：v1.1.5
#######################################################
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WAPIWrapperCSharp;
using WindCommon;
using TDBAPI;

namespace myWindAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            CommodityFutures myStore1 = new CommodityFutures("DCE", 20130401, 20130430);
            CommodityFutures myStore2 = new CommodityFutures("CZC", 20130401, 20130430);
            CommodityFutures myStore3 = new CommodityFutures("SHF", 20130401, 20130430);
            TreasuryBondFutures myStore4 = new TreasuryBondFutures("CF", 20130401, 20160430);
            //ETFOption myStore5 = new ETFOption("SH", 20150209, 20151231);
        }
    }
}
