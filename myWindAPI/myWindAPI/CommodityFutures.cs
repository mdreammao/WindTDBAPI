﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDBAPI;
using System.Data.SqlClient;
using System.Data;

namespace myWindAPI
{
    class CommodityFutures
    {
        /// <summary>
        /// 未指定数据库名称的连接字符串
        /// </summary>
        //public string orignalConnectString = "server=(local);database=;Integrated Security=true;";
        //public string orignalConnectString = "server=192.168.38.217;database=;uid =sa;pwd=maoheng0;";
        public string orignalConnectString = "server=145.146.30.152;uid =spqhhqld;pwd=spqhhqld;";
        /// <summary>
        /// 连接万德商品期货TDB数据库的参数。
        /// </summary>
        public TDBsource mySource = new TDBsource("114.80.154.34", "10060", "TD5928909014", "13305104");
        /// <summary>
        /// 记录全市场商品期货信息的列表。
        /// </summary>
        public List<commodityFormat> commodityList = new List<commodityFormat>();
        /// <summary>
        /// 市场
        /// </summary>
        public string market;
        /// <summary>
        /// 开始时间
        /// </summary>
        public int startDate;
        /// <summary>
        /// 结束时间
        /// </summary>
        public int endDate;
        /// <summary>
        /// 生成日志的时间。
        /// </summary>
        public string logName;

        /// <summary>
        /// TDB数据接口类。
        /// </summary>
        private TDBDataSource tdbSource;

        /// <summary>
        /// 记录交易日信息的类。
        /// </summary>
        public TradeDays myTradeDays;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="market">市场</param>
        /// <param name="startDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        public CommodityFutures(string market,int startDate,int endDate=0)
        {
            this.market = market.ToUpper();
            this.startDate = startDate;
            if (endDate==0)
            {
                endDate = startDate;
            }
            this.endDate = endDate;
            //对接口类进行初始化。
            tdbSource = new TDBDataSource(mySource.IP, mySource.port, mySource.account, mySource.password);
            if (CheckConnection())
            {
                Console.WriteLine("Connect Success!");
                myTradeDays = new TradeDays(startDate, endDate);
                Console.WriteLine("Tradedays Collect!");
                commodityList = GetCommodityList(market);
                Console.WriteLine("CommodityList Collect!");
                logName = DateTime.Now.ToString()+"_log.txt";
                logName = logName.Replace("/", "_");
                logName = logName.Replace(" ", "_");
                logName = logName.Replace(":", "_");
                StoreData();
            }
            else
            {
                Console.WriteLine("Please Input Valid Parameters!");
            }

            //工作完毕之后，关闭万德TDB数据库的连接。
            //关闭连接
            tdbSource.DisConnect();
        }

        /// <summary>
        /// 核心存储函数。根据市场信息等存储商品期货数据。
        /// </summary>
        public void StoreData()
        {
            foreach (var commodity in commodityList)
            {
                string tableName = "MarketData_" + commodity.code + "_" + commodity.market;
                int maxRecordDate = 0;
                foreach (int today in myTradeDays.myTradeDays)
                {
                    if (today>commodity.endDate)
                        //如果在期货交割之后，不再进行记录
                    {
                        break;
                    }
                    int yesterday = TradeDays.GetPreviousTradeDay(today);
                    string todayDataBase = "TradeMarket" + (today / 100).ToString();
                    //string todayConnectString = "server=(local);database=" + todayDataBase + ";Integrated Security=true;";
                     //string todayConnectString = "server=192.168.38.217;database=" + todayDataBase + ";uid =sa;pwd=maoheng0;";
                    string todayConnectString = "server=145.146.30.152;database=" + todayDataBase + ";uid =spqhhqld;pwd=spqhhqld;";
                    if (SqlApplication.CheckDataBaseExist(todayDataBase,orignalConnectString)==false)
                    {
                        maxRecordDate = 0;
                    }
                    else if (yesterday / 100 != today / 100 || today == startDate || maxRecordDate==0)
                    {
                        if (SqlApplication.CheckExist(todayDataBase,tableName,todayConnectString)==true)
                        {
                            maxRecordDate = MaxRecordDate(tableName, todayConnectString);
                        }
                    }
                    if (maxRecordDate < today)
                    //若没有记录数据，需要重新记录
                    //若数据存在，存储数据,否则需要跳过
                    { 
                        TDBReqFuture reqFuture = new TDBReqFuture(commodity.contractName, today, today);
                        TDBFutureAB[] futureABArr;
                        TDBErrNo nErrInner = tdbSource.GetFutureAB(reqFuture, out futureABArr);
                        if (futureABArr.Length==0)
                        {
                            continue;
                        }
                        //string yesterdayDataBase = "TradeMarket"+(yesterday/100).ToString();
                        //string yesterdayConnectString = "server=(local);database=" + yesterdayDataBase + ";Integrated Security=true;";
                        //string yesterdayConnectString = "server=192.168.38.217;database=" + yesterdayDataBase + ";uid =sa;pwd=maoheng0;";
                        if (SqlApplication.CheckDataBaseExist(todayDataBase,orignalConnectString)==false)
                            //检测当日对应的数据库是否存在
                        {
                            CreateDataBase(todayDataBase, orignalConnectString);
                        }
                        if (SqlApplication.CheckExist(todayDataBase, tableName, orignalConnectString)==false)
                        {
                            CreateTable(tableName, todayConnectString);
                        }
                        //if (yesterdayDataBase!=todayDataBase)
                        //    //如果前一交易日所在月份和当日不同，检测上一个月的数据库是否存在
                        //{
                        //    if (SqlApplication.CheckDataBaseExist(yesterdayDataBase, orignalConnectString) == false)
                        //    {
                        //        CreateDataBase(yesterdayDataBase, orignalConnectString);
                        //    }
                        //    if (SqlApplication.CheckExist(yesterdayDataBase, tableName, orignalConnectString) == false)
                        //    {
                        //        CreateTable(tableName, yesterdayConnectString);
                        //    }
                        //}
                        //判断数据是否已经存储，若数据存在，默认已经记录，仅记录数据条数，写入日志文档，靠人工来校对
                        // int alreadyRecord = CountRecordNumber(tableName, todayConnectString, today);
                        commodityShot[] dataList= ModifyData(futureABArr, commodity.contractName, today);
                        StoreDataDaily(tableName, todayConnectString, dataList);
                        Console.WriteLine("Date:{0}, table:{1}, MaxRecordDate:{2}, Wind:{3}", today, tableName,maxRecordDate, futureABArr.Length);
                        string log = "Date:" + today.ToString() + ", table:" + tableName + ", MaxRecordDate:" + maxRecordDate.ToString() + ", Wind:" + futureABArr.Length.ToString();
                        MyApplication.TxtWrite(logName, log);
                        maxRecordDate = today;
                    }
                    else
                    {
                        Console.WriteLine("Date:{0}, table:{1}, MaxRecordDate:{2}", today, tableName, maxRecordDate);
                        string log = "Date:" + today.ToString() + ", table:" + tableName + ", MaxRecordDate:" + maxRecordDate.ToString();
                        MyApplication.TxtWrite(logName, log);
                    }
                }
            }
            
        }

        /// <summary>
        /// 逐日逐品种存储数据。
        /// </summary>
        public void StoreDataDaily(string tableName,string connectString, commodityShot[] data)
        {
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();
                DataTable todayData = new DataTable();
                #region DataTable的列名的建立
                todayData.Columns.Add("id", typeof(int));
                todayData.Columns.Add("stkcd", typeof(string));
                todayData.Columns.Add("tdate", typeof(string));
                todayData.Columns.Add("ndate", typeof(string));
                todayData.Columns.Add("ttime", typeof(string));
                todayData.Columns.Add("cp", typeof(double));
                todayData.Columns.Add("S1", typeof(double));
                todayData.Columns.Add("S2", typeof(double));
                todayData.Columns.Add("S3", typeof(double));
                todayData.Columns.Add("S4", typeof(double));
                todayData.Columns.Add("S5", typeof(double));
                todayData.Columns.Add("S6", typeof(double));
                todayData.Columns.Add("S7", typeof(double));
                todayData.Columns.Add("S8", typeof(double));
                todayData.Columns.Add("S9", typeof(double));
                todayData.Columns.Add("S10", typeof(double));
                todayData.Columns.Add("B1", typeof(double));
                todayData.Columns.Add("B2", typeof(double));
                todayData.Columns.Add("B3", typeof(double));
                todayData.Columns.Add("B4", typeof(double));
                todayData.Columns.Add("B5", typeof(double));
                todayData.Columns.Add("B6", typeof(double));
                todayData.Columns.Add("B7", typeof(double));
                todayData.Columns.Add("B8", typeof(double));
                todayData.Columns.Add("B9", typeof(double));
                todayData.Columns.Add("B10", typeof(double));
                todayData.Columns.Add("SV1", typeof(double));
                todayData.Columns.Add("SV2", typeof(double));
                todayData.Columns.Add("SV3", typeof(double));
                todayData.Columns.Add("SV4", typeof(double));
                todayData.Columns.Add("SV5", typeof(double));
                todayData.Columns.Add("SV6", typeof(double));
                todayData.Columns.Add("SV7", typeof(double));
                todayData.Columns.Add("SV8", typeof(double));
                todayData.Columns.Add("SV9", typeof(double));
                todayData.Columns.Add("SV10", typeof(double));
                todayData.Columns.Add("BV1", typeof(double));
                todayData.Columns.Add("BV2", typeof(double));
                todayData.Columns.Add("BV3", typeof(double));
                todayData.Columns.Add("BV4", typeof(double));
                todayData.Columns.Add("BV5", typeof(double));
                todayData.Columns.Add("BV6", typeof(double));
                todayData.Columns.Add("BV7", typeof(double));
                todayData.Columns.Add("BV8", typeof(double));
                todayData.Columns.Add("BV9", typeof(double));
                todayData.Columns.Add("BV10", typeof(double));
                todayData.Columns.Add("hp", typeof(double));
                todayData.Columns.Add("lp", typeof(double));
                todayData.Columns.Add("HighLimit", typeof(double));
                todayData.Columns.Add("LowLimit", typeof(double));
                todayData.Columns.Add("ts", typeof(double));
                todayData.Columns.Add("tt", typeof(double));
                todayData.Columns.Add("OPNPRC", typeof(double));
                todayData.Columns.Add("PRECLOSE", typeof(double));
                todayData.Columns.Add("Settle", typeof(double));
                todayData.Columns.Add("PrevSettle", typeof(double));
                todayData.Columns.Add("CurrDelta", typeof(int));
                todayData.Columns.Add("PreDelta", typeof(int));
                todayData.Columns.Add("OpenInterest", typeof(int));
                todayData.Columns.Add("PreOpenInterest", typeof(int));
                todayData.Columns.Add("LocalRecTime", typeof(string));
                todayData.Columns.Add("TradeStatus", typeof(string));


                #endregion
                foreach (commodityShot f in data)
                {
                    #region 将数据写入每一行中。
                    DataRow r = todayData.NewRow();
                    r["id"] = f.id;
                    r["stkcd"] = f.stkcd;
                    r["tdate"] = f.tdate;
                    r["ndate"] = f.ndate;
                    r["ttime"] = f.ttime;
                    r["cp"] = f.cp;
                    r["S1"] = f.S1;
                    r["SV1"] = f.SV1;
                    r["B1"] = f.B1;
                    r["BV1"] = f.BV1;
                    r["ts"] = f.ts;
                    r["tt"] = f.tt;
                    r["OPNPRC"] = f.OPNPRC;
                    r["PRECLOSE"] = f.PRECLOSE;
                    r["settle"] = f.Settle;
                    r["PrevSettle"] = f.PrevSettle;
                    r["CurrDelta"] = f.CurrDelta;
                    r["hp"] = f.hp;
                    r["lp"] = f.lp;
                    r["OpenInterest"] = f.OpenInterest;
                    r["PreOpenInterest"] = f.PreOpenInterest;
                    r["TradeStatus"] = f.TradeStatus;
                    todayData.Rows.Add(r);
                    #endregion
                }

                using (SqlBulkCopy bulk = new SqlBulkCopy(connectString))
                {
                    try
                    {
                        bulk.BatchSize = 100000;
                        bulk.DestinationTableName = tableName;
                        #region 依次建立数据的映射。
                        bulk.ColumnMappings.Add("id", "id");
                        bulk.ColumnMappings.Add("stkcd", "stkcd");
                        bulk.ColumnMappings.Add("tdate", "tdate");
                        bulk.ColumnMappings.Add("ndate", "ndate");
                        bulk.ColumnMappings.Add("ttime", "ttime");
                        bulk.ColumnMappings.Add("cp", "cp");
                        bulk.ColumnMappings.Add("S1", "S1");
                        bulk.ColumnMappings.Add("S2", "S2");
                        bulk.ColumnMappings.Add("S3", "S3");
                        bulk.ColumnMappings.Add("S4", "S4");
                        bulk.ColumnMappings.Add("S5", "S5");
                        bulk.ColumnMappings.Add("S6", "S6");
                        bulk.ColumnMappings.Add("S7", "S7");
                        bulk.ColumnMappings.Add("S8", "S8");
                        bulk.ColumnMappings.Add("S9", "S9");
                        bulk.ColumnMappings.Add("S10", "S10");
                        bulk.ColumnMappings.Add("B1", "B1");
                        bulk.ColumnMappings.Add("B2", "B2");
                        bulk.ColumnMappings.Add("B3", "B3");
                        bulk.ColumnMappings.Add("B4", "B4");
                        bulk.ColumnMappings.Add("B5", "B5");
                        bulk.ColumnMappings.Add("B6", "B6");
                        bulk.ColumnMappings.Add("B7", "B7");
                        bulk.ColumnMappings.Add("B8", "B8");
                        bulk.ColumnMappings.Add("B9", "B9");
                        bulk.ColumnMappings.Add("B10", "B10");
                        bulk.ColumnMappings.Add("SV1", "SV1");
                        bulk.ColumnMappings.Add("SV2", "SV2");
                        bulk.ColumnMappings.Add("SV3", "SV3");
                        bulk.ColumnMappings.Add("SV4", "SV4");
                        bulk.ColumnMappings.Add("SV5", "SV5");
                        bulk.ColumnMappings.Add("SV6", "SV6");
                        bulk.ColumnMappings.Add("SV7", "SV7");
                        bulk.ColumnMappings.Add("SV8", "SV8");
                        bulk.ColumnMappings.Add("SV9", "SV9");
                        bulk.ColumnMappings.Add("SV10","SV10");
                        bulk.ColumnMappings.Add("BV1", "BV1");
                        bulk.ColumnMappings.Add("BV2", "BV2");
                        bulk.ColumnMappings.Add("BV3", "BV3");
                        bulk.ColumnMappings.Add("BV4", "BV4");
                        bulk.ColumnMappings.Add("BV5", "BV5");
                        bulk.ColumnMappings.Add("BV6", "BV6");
                        bulk.ColumnMappings.Add("BV7", "BV7");
                        bulk.ColumnMappings.Add("BV8", "BV8");
                        bulk.ColumnMappings.Add("BV9", "BV9");
                        bulk.ColumnMappings.Add("BV10", "BV10");
                        bulk.ColumnMappings.Add("hp", "hp");
                        bulk.ColumnMappings.Add("lp", "lp");
                        bulk.ColumnMappings.Add("HighLimit", "HighLimit");
                        bulk.ColumnMappings.Add("LowLimit", "LowLimit");
                        bulk.ColumnMappings.Add("ts", "ts");
                        bulk.ColumnMappings.Add("tt", "tt");
                        bulk.ColumnMappings.Add("OPNPRC", "OPNPRC");
                        bulk.ColumnMappings.Add("PRECLOSE", "PRECLOSE");
                        bulk.ColumnMappings.Add("Settle", "Settle");
                        bulk.ColumnMappings.Add("PrevSettle", "PrevSettle");
                        bulk.ColumnMappings.Add("CurrDelta", "CurrDelta");
                        bulk.ColumnMappings.Add("PreDelta", "PreDelta");
                        bulk.ColumnMappings.Add("OpenInterest", "OpenInterest");
                        bulk.ColumnMappings.Add("PreOpenInterest", "PreOpenInterest");
                        bulk.ColumnMappings.Add("LocalRecTime", "LocalRecTime");
                        bulk.ColumnMappings.Add("TradeStatus", "TradeStatus"); 
                        #endregion
                        bulk.WriteToServer(todayData);
                    }
                    catch (Exception myerror)
                    {
                        //记录错误信息
                        System.Console.WriteLine(myerror.Message);
                        MyApplication.TxtWrite(logName, myerror.Message);
                    }
                }
                conn.Close();
            }
        }

        /// <summary>
        /// 将数据格式从万德格式调整为本地数据库格式的函数。
        /// </summary>
        /// <param name="futureABArr">万德数据</param>
        /// <param name="code">商品代码</param>
        /// <param name="todayData">今日数据</param>
        /// <param name="yesterdayData">昨日数据</param>
        public commodityShot[] ModifyData(TDBFutureAB[] futureABArr,string code,int today)
        {
            commodityShot[] myShotList = new commodityShot[futureABArr.Length];
            int id = 0;
            for (int i = 0; i < futureABArr.Length; i++)
            {
                TDBFutureAB future = futureABArr[i];
                //时间需要做具体的处理，分别记录交易日和自然日信息。
                int time = future.m_nTime;
                int date = today;
                id += 1;
                myShotList[i].id = id;
                myShotList[i].stkcd = code;
                myShotList[i].tdate = date.ToString();
                myShotList[i].ndate = future.m_nDate.ToString();
                myShotList[i].ttime = time.ToString().PadLeft(9, '0');
                myShotList[i].cp = future.m_nPrice / 10000.0;
                myShotList[i].S1 = future.m_nAskPrice[0] / 10000.0;
                myShotList[i].SV1 = future.m_nAskVolume[0];
                myShotList[i].B1 = future.m_nBidPrice[0] / 10000.0;
                myShotList[i].BV1 = future.m_nBidVolume[0];
                myShotList[i].ts = future.m_iAccVolume;
                myShotList[i].tt = future.m_iAccTurover;
                myShotList[i].OPNPRC = future.m_nOpen / 10000.0;
                myShotList[i].PRECLOSE = future.m_nPreClose / 10000.0;
                myShotList[i].Settle = future.m_nSettle / 10000.0;
                myShotList[i].PrevSettle = future.m_nPreSettle / 10000.0;
                myShotList[i].CurrDelta = future.m_nCurDelta;
                myShotList[i].hp = future.m_nHigh / 10000.0;
                myShotList[i].lp = future.m_nLow / 10000.0;
                myShotList[i].OpenInterest = future.m_nPosition;
                myShotList[i].PreOpenInterest = future.m_nPrePosition;
                if (time<160000000 && time>60000000)//该数据根据日盘夜盘的分割来调整
                {
                    myShotList[i].TradeStatus = "001";
                }
                else
                {
                    myShotList[i].TradeStatus = "002";
                }
            }
            return myShotList;
        }

        /// <summary>
        /// 获取已存储信息的函数。
        /// </summary>
        /// <param name="tableName">数据表</param>
        /// <param name="connectString">连接字符串</param>
        /// <param name="date">日期</param>
        /// <returns>记录数目</returns>
        public int CountRecordNumber(string tableName, string connectString,int date)
        {
            int num = 0;
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select COUNT(*) from [" + tableName + "] where [tdate]="+date.ToString();
                try
                {

                    int number = (int)cmd.ExecuteScalar();
                    if (number > 0)
                    {
                        num=number;
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception myerror)
                {
                    System.Console.WriteLine(myerror.Message);
                }
            }
            return num;
        }

        /// <summary>
        /// 获取已存储信息的函数。
        /// </summary>
        /// <param name="tableName">数据表</param>
        /// <param name="connectString">连接字符串</param>
        /// <returns>记录数目</returns>
        public int MaxRecordDate(string tableName, string connectString)
        {
            int num = 0;
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "select max(tdate) from [" + tableName + "]";
                try
                {

                    int number = Convert.ToInt32(cmd.ExecuteScalar());
                    if (number > 0)
                    {
                        num = number;
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception myerror)
                {
                    System.Console.WriteLine(myerror.Message);
                }
            }
            return num;
        }

        /// <summary>
        /// 新建数据表。
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="connectString">连接字符串</param>
        public void CreateTable(string tableName,string connectString)
        {
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE [dbo].["+tableName+ "]([marketdataid][int] IDENTITY(1, 1) NOT NULL,[id] [int] NOT NULL,[stkcd] [char](11) NOT NULL,[tdate] [char](8) NOT NULL,[ndate] [char](8) NOT NULL,[ttime] [char](9) NOT NULL,[cp] [decimal](12,3) NULL,[S1] [decimal](12,3) NULL,[S2] [decimal](12,3) NULL,[S3] [decimal](12,3) NULL,[S4] [decimal](12,3) NULL,[S5] [decimal](12,3) NULL,[S6] [decimal](12,3) NULL,[S7] [decimal](12,3) NULL,[S8] [decimal](12,3) NULL,	[S9] [decimal](12,3) NULL,[S10] [decimal](12,3) NULL,[B1] [decimal](12,3) NULL,[B2] [decimal](12,3) NULL,	[B3] [decimal](12,3) NULL,[B4] [decimal](12,3) NULL,[B5] [decimal](12,3) NULL,[B6] [decimal](12,3) NULL,	[B7] [decimal](12,3) NULL,[B8] [decimal](12,3) NULL,[B9] [decimal](12,3) NULL,[B10] [decimal](12,3) NULL,	[SV1] [decimal](10, 0) NULL,[SV2] [decimal](10, 0) NULL,[SV3] [decimal](10, 0) NULL,[SV4] [decimal](10, 0) NULL,[SV5] [decimal](10, 0) NULL,[SV6] [decimal](10, 0) NULL,[SV7] [decimal](10, 0) NULL,[SV8] [decimal](10, 0) NULL,[SV9] [decimal](10, 0) NULL,[SV10] [decimal](10, 0) NULL,[BV1] [decimal](10, 0) NULL,[BV2] [decimal](10, 0) NULL,[BV3] [decimal](10, 0) NULL,[BV4] [decimal](10, 0) NULL,[BV5] [decimal](10, 0) NULL,[BV6] [decimal](10, 0) NULL,[BV7] [decimal](10, 0) NULL,[BV8] [decimal](10, 0) NULL,[BV9] [decimal](10, 0) NULL,[BV10] [decimal](10, 0) NULL,[hp] [decimal](12,3) NULL,[lp] [decimal](12,3) NULL,[HighLimit] [decimal](12,3) NULL,[LowLimit]   [decimal](12,3) NULL,[ts] [decimal](20, 0) NULL,[tt] [decimal](20, 3) NULL,[OPNPRC] [decimal](12,3) NULL,	[PRECLOSE] [decimal](12,3) NULL,[Settle] [decimal](12,3) NULL,[PrevSettle] [decimal](12,3) NULL,[CurrDelta]    [int] NULL,[PreDelta] [int] NULL,[OpenInterest] [int] NULL,[PreOpenInterest] [int] NULL,[LocalRecTime]        [char](9) NULL,[TradeStatus] [char](3) NULL,CONSTRAINT[PK_" + tableName + "] PRIMARY KEY NONCLUSTERED([marketdataid] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]) ON [PRIMARY] CREATE CLUSTERED INDEX[IX_" + tableName + "_TDATE] ON[dbo].[" + tableName + "]([tdate] ASC,[id] ASC)WITH(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON[PRIMARY]";
                try
                {
                    cmd.ExecuteReader();
                }
                catch (Exception myerror)
                {
                    System.Console.WriteLine(myerror.Message);
                }
            }
        }

        /// <summary>
        /// 新建数据库函数。
        /// </summary>
        /// <param name="dataBaseName">需新建的数据库名称</param>
        /// <param name="connectString">连接字符串</param>
        public void CreateDataBase(string dataBaseName,string connectString)
        {
            using (SqlConnection conn = new SqlConnection(connectString))
            {
                conn.Open();//打开数据库  
                SqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE DATABASE "+dataBaseName+" ON PRIMARY (NAME = '"+dataBaseName+"', FILENAME = 'G:\\"+dataBaseName+ ".dbf',SIZE = 1024MB,MaxSize = 512000MB,FileGrowth = 1024MB) LOG ON (NAME = '" + dataBaseName + "Log',FileName = 'G:\\" + dataBaseName + ".ldf',Size = 20MB,MaxSize = 1024MB,FileGrowth = 10MB)";
                try
                {
                    cmd.ExecuteReader();
                }
                catch (Exception myerror)
                {
                    System.Console.WriteLine(myerror.Message);
                }
            }
        }

        /// <summary>
        /// 获取商品期货列表的信息。
        /// </summary>
        /// <param name="market">市场</param>
        /// <returns>商品期货信息列表</returns>
        public List<commodityFormat> GetCommodityList(string market)
        {
            List<commodityFormat> myList = new List<commodityFormat>();
            TDBCode[] codeArr;
            tdbSource.GetCodeTable(market, out codeArr);
            foreach (var item in codeArr)
            {
                commodityFormat myCommodity = new commodityFormat();
                string code = item.m_strCode.ToUpper();
                string date="";
                myCommodity.contractName = code;
                if (market=="CZC")
                {
                    date = MyApplication.RemoveNotNumber(code);
                    date = "1" + date;
                }
                else
                {
                    date = MyApplication.RemoveNotNumber(code);
                }
                if (date.Length!=4)
                {
                    continue;
                }
                if (date.Length==4)
                {
                    int num = Convert.ToInt32(date);
                    if (num>=1304)
                    {
                        myCommodity.contractName = code+"."+market;
                        myCommodity.code = code;
                        myCommodity.startDate = (num - 100+200000) * 100 + 01;
                        myCommodity.endDate = (num+200000) * 100 + 31;
                        myCommodity.market = market;
                        myList.Add(myCommodity);
                    }
                }
            }
            return myList;
        }

        /// <summary>
        /// 判断TDB数据库是否连接成功。
        /// </summary>
        /// <returns>返回是否连接成功。</returns>
        public bool CheckConnection()
        {
            TDBLoginResult loginRes;
            TDBErrNo nErr = tdbSource.Connect(out loginRes);
            //输出登陆结果
            if (nErr == TDBErrNo.TDB_OPEN_FAILED)
            {
                Console.WriteLine("open failed, reason:{0}", loginRes.m_strInfo);
                Console.WriteLine();
                return false;
            }
            return true;
        }
    }
}
