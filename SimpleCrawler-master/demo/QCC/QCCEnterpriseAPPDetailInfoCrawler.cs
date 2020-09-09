using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Yinhe.ProcessingCenter;

using Yinhe.ProcessingCenter.DataRule;
using System.Collections;
using Newtonsoft.Json.Linq;
using LibCurlNet;
using Helper.ConsistentHash;
using Helper;
using System.Threading;
using System.Threading.Tasks;
using MZ.RabbitMQ;
using MZ.QuickHelper;

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 门派url
    /// https://xcx.qichacha.com/wxa/v1/base/
    ///
    /// </summary>
    public class QCCEnterpriseAPPDetailInfoCrawler : SimpleCrawlerBase
    {

        QuickQCCEnterpriseHelper qccHelper = new QuickQCCEnterpriseHelper();

        const int takeCount = 8;
        int curThreadId = 0;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QCCEnterpriseAPPDetailInfoCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_House_Land_Relation";//房间
            //DataTableName = "QCCEnterpriseKey_YangQiDetial";//房间
            updatedValue = "1";//是否更新字段
           // uniqueKeyField = "eGuid";
            //  InitialMQ();
        }
        public static object lockObj = new object();
        public static int lockSeed =0;

        public void ChangeAccount()
        {
            Thread.Sleep(2000);
            if (QuickTaskHelper.Instance().ContinueMethodByBusyGear("ChangeAccount", 20))
            {
                QuickProxyPoolHelper.Instance().ExecChangeIp();
                var deviceAccount = qccHelper.AutoChangeDeviceAccount();
                Settings.AppId = deviceAccount.appId;
                Settings.timestamp = deviceAccount.timestamp;
                Settings.sign = deviceAccount.sign;
                Settings.AccessToken = deviceAccount.accessToken;
                Settings.RefleshToken = deviceAccount.refreshToken;
            }    
            Thread.Sleep(2000);
        }

        public void initialUrl()
        {
            var mongoDb = MongoOpCollection.GetNew121MongoOp_MT("LandFang");
            Console.WriteLine("开始载入数据");
            var query = Query.And( Query.EQ("isDetailUpdate", -2));
            // query = Query.And(query, Query.NE("status", "吊销"), Query.NE("status", "注销"));
            var fields = new string[] { "eGuid" };

            globalTotalCount = (int)mongoDb.FindCount(DataTableName, query);
            var skipCount = 0;
            var limitCount = 1000;
            if (globalTotalCount >= 10000)
            {
                skipCount = new Random().Next(0, 1000);
            }
            else
            {
                skipCount = 0;
            }
            // var allHitObjList = dataop.FindLimitFieldsByQuery(DataTableName, query, new MongoDB.Driver.SortByDocument() { { "_id", 1 } }, skipCount, limitCount, fields);
            var allHitObjList = mongoDb.FindAll(DataTableName, query).SetSkip(skipCount).SetLimit(limitCount).SetFields(fields);
            ChangeAccount();

            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                //0DeviceId:OvxoWqbJUHBtKQrcIlrzqG8F,timestamp:1587580669293,sign:ddcccfa309a1567b9cac673a8644514bfc248713,RefleshToken:aa04aef1beef7d4dac2496a9b07ba82c,AccessToken:NGRlOTAxNjAtOGMwNS00NzQ0LTk4NzEtYzZlYzhkYTAxZGY4
                var guid = hitObj.Text("eGuid");
                var curUrl = $"https://appv2.qichacha.net/app/v1/base/getMoreEntInfo?unique={guid}&sign=ddcccfa309a1567b9cac673a8644514bfc248713&token=NGRlOTAxNjAtOGMwNS00NzQ0LTk4NzEtYzZlYzhkYTAxZGY4&timestamp=1587580669293&from=h5 ";

                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = guid });
                    filter.Add(curUrl);// 防止执行2次
                }

            }
        }
        /// <summary>
        /// 获取新的代理IP
        /// </summary>
        /// <returns></returns>
        public WebProxy GetProxy()
        {
            return QuickProxyPoolHelper.Instance().Default();
            var webProxy = QuickProxyPoolHelper.Instance().Get(20);
            if (webProxy == null)
            {
                var result = QuickDicMethod.Instance("GetProxy").Put("webProxy", 1);
                if (result >= Settings.MaxReTryTimes)
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                QuickDicMethod.Instance("GetProxy").Put("webProxy", -1);
            }
            return webProxy;
        }
        /// <summary>
        /// 代办需要增加 自定义userAgent，自定Referer请求头
        /// </summary>
        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 5;
            Settings.MaxReTryTimes = 5;
            // Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (Linux; Android 5.1.1; MI 6 Build/NMF26X; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/74.0.3729.136 Mobile Safari/537.36";
            Settings.Accept = "application/json, text/javascript, */*; q=0.01";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.ContentType = "application/json";
            Settings.Referer = "https://share.qichacha.com/pro/app_9.2.8/features/more-info.html?deviceType=android";
            Settings.AutoSpeedLimit = true;
            Settings.CrawlerClassName = "QCCEnterpriseAPPDetailInfoCrawler";
            //Settings.CurWebProxy= GetProxy();
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();
            InitialMQ();
        }

      


        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            if (curThreadId == 0)
            {
                curThreadId = Thread.CurrentThread.ManagedThreadId;
            }
            if (UrlQueue.Instance.Count <= 100 && Thread.CurrentThread.ManagedThreadId == curThreadId)
            {
                Console.WriteLine("开始加载新数据");
                initialUrl();
            }
            var guid = args.urlInfo.UniqueKey;


            string keyName = "guid";
            string keyName_bak = "eGuid";

            if (string.IsNullOrEmpty(guid))
            {
                this.ShowMessageInfo("传入Key为空", false);
            }
            if (args.Html.Contains("该企业主体信息获取失败"))
            {
                this.ShowMessageInfo("该企业主体信息获取失败" + UrlQueue.Instance.Count.ToString(), false);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Set("isDetailUpdate", -1), Name = DataTableName, Query = Query.EQ(keyName, guid), Type = StorageType.Update });
                return;
            }
            JObject jsonObj = JObject.Parse(args.Html);
            if (jsonObj != null)
            {
                try
                { 
                    var result = jsonObj["result"];
                    if (result != null)
                    {
                        var companyDoc = result.ToString().GetBsonDocFromJson();
                        var updateBsonDoc = new BsonDocument();
                        var paid_in_capi = result["RecCap"] != null ? result["RecCap"].ToString() : string.Empty;
                        var termStart = result["TermStart"] != null ? result["TermStart"].ToString() : string.Empty;

                        var checkDate = result["CheckDate"] != null ? result["CheckDate"].ToString() : string.Empty;

                        updateBsonDoc.Set("paid_in_capi", paid_in_capi);

                        companyDoc.Set("paid_in_capi", paid_in_capi);
                        if (checkDate.Length >= 10 && !checkDate.Contains("-"))
                        {
                            checkDate = checkDate.ConvertTimeStampDate_Unix().ToString("yyyy-MM-dd");
                        }
                        if (termStart.Length >= 10 & !termStart.Contains("-"))
                        {
                            termStart = termStart.ConvertTimeStampDate_Unix().ToString("yyyy-MM-dd");
                            companyDoc.Set("termBuildDate", termStart);
                        }
                        updateBsonDoc.Set("checkDate", checkDate);
                        companyDoc.Set("checkDate", checkDate);

                        var commonList = result["CommonList"];
                        if (commonList != null)
                        {
                            var jsonStr = commonList.ToString();
                            var commonDocList = jsonStr.ToBsonDocumentList();
                            var hitCommonAttr = commonDocList.Where(c => c.Text("Key") == "3").FirstOrDefault();
                            if (hitCommonAttr != null)
                            {
                                var insuredPersonsNum = hitCommonAttr.Int("Value");
                                updateBsonDoc.Set("insuredPersonsNum", insuredPersonsNum);
                                companyDoc.Set("insuredPersonsNum", insuredPersonsNum);
                            }
                        }
                        
                        updateBsonDoc.Set("isDetailUpdate", 1);
                        updateBsonDoc.CopyFieldFromEnterpriseDetailInfo(companyDoc);
                        updateBsonDoc.Set("eGuid", guid);
                        var detailRemark = updateBsonDoc.ToJson();
                        PushData(updateBsonDoc,keyFiled:"eGuid");
                        ///推送消息队列
                        companyDoc.Set("guid", guid);
                       PushMessageAsync(companyDoc);
                    
                    }
                }

                catch (Exception ex)

                {

                }
            }
            ShowStatus();
            //Settings.CurWebProxy = GetProxy();
            if (updateCount %50 == 0)
            {
                ChangeAccount();
                QuickProxyPoolHelper.Instance().ExecChangeIp();
            }
        }

        int limitTimes = 0;
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        override
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            try
            {
                var html = args.Html;
                var htmlObj = html.HtmlLoad();
                var baseInfNode = htmlObj.GetElementbyId("Cominfo");
                var updateBson = new BsonDocument();
                if (args.Html.Contains("Employees")|| args.Html.Contains("成功") )
                {
                    return false;
                }
                else
                {
                    Interlocked.Increment(ref limitTimes);
                    if (limitTimes>0&&limitTimes % 50 == 0)
                    {
                        //超过限制个数
                        ChangeAccount();
                    }
                    if (args.Html.Contains("异常"))
                    {
                        Console.WriteLine(args.Html);
                        ChangeAccount();
                    }
                    if (args.Html.Contains("nvar arg1"))
                    {
                        Thread.Sleep(5000);
                        QuickProxyPoolHelper.Instance().ExecChangeIp();
                        Thread.Sleep(5000);
                    }
                    var needChangeProxy = false;
                    if (args.Html.Contains("从传输流") || args.Html.Contains("请求被中止") || args.Html.Contains("操作超时") || args.Html.Contains("基础连接已经关闭"))
                    {
                        if ((args.Html.Contains("超时") || args.Html.Contains("基础连接已经关闭")) && Settings.CurWebProxy != null)
                        {
                            var proxyKey = Settings.CurWebProxy.Address.ToString();
                            var errorCount = QuickDicMethod.Instance("WebProxySet").Put(proxyKey, 1);
                            if (errorCount <= Settings.MaxReTryTimes)
                            {
                                needChangeProxy = false;

                            }
                            else
                            {
                                needChangeProxy = true;
                            }
                        }

                    }
                    else
                    {
                        if (args.Html.Contains("异常"))
                        {
                            needChangeProxy = true;
                        }
                    }
                    if (Settings.CurWebProxy != null && needChangeProxy)
                    {
                        Console.WriteLine(Settings.CurWebProxy.Address + "无效");
                       // QuickProxyPoolHelper.Instance().Delete(Settings.CurWebProxy, updateCount);
                        Thread.Sleep(1000);
                        QuickProxyPoolHelper.Instance().ExecChangeIp();
                       // Settings.CurWebProxy = GetProxy();

                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }

        }


    }

}
