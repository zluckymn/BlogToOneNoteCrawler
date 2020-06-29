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

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 网易云课堂
    /// https://study.163.com/j/web/fetchPersonalData.json?categoryId=480000003121024&t=1576574944340
    ///
    /// </summary>
    public class OnlineStudyListCrawler_163 : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“OnlineStudyListCrawler_163.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“OnlineStudyListCrawler_163.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 10;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public OnlineStudyListCrawler_163(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "OnlineStudyRoom";//房间
            DataTableCategoryName = "Category";
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "productId";
        }

        public void initialUrl(string catid)
        {
            var url = $"https://study.163.com/j/web/fetchPersonalData.json?categoryId={catid}&t={GetTimeStamp()}";
            if (!filter.Contains(url))
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = catid });
                filter.Add(url);// 防止执行2次
            }
              
           

        }

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
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 2;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "application/json";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("edu-script-token", "cb8372e2a11b4a3e8b017a77cb9389b5");
            Settings.HeadSetDic.Add("X-Client-Type", "wxa");
            
            Settings.SimulateCookies = "ntes_nnid=07af05e3896c765a57f0661bd5d82541,1574343946255; _ntes_nuid=07af05e3896c765a57f0661bd5d82541; NTESSTUDYSI=cb8372e2a11b4a3e8b017a77cb9389b5; EDUWEBDEVICE=db78416fd81247e3b595ddd4e9e0ab57; eds_utm=eyJjIjoiIiwiY3QiOiIiLCJpIjoiIiwibSI6IiIsInMiOiIiLCJ0IjoiIn0=|aHR0cHM6Ly93d3cuYmFpZHUuY29tL2xpbms/dXJsPUFYdWRHaWNkVTB2NlZxYWhXRE5vZXR1YUZ6ZG9OaXNSYXpPWjBvM2gtN0cmd2Q9JmVxaWQ9YjhmMzIxOWMwMDBiZDQzMzAwMDAwMDA2NWRmMzZlMWI=; hb_MA-BFF5-63705950A31C_source=www.baidu.com; __utmc=129633230; EDU-YKT-MODULE_GLOBAL_PRIVACY_DIALOG=true; __utmz=129633230.1576492724.2.2.utmcsr=baidu|utmccn=(organic)|utmcmd=organic; NNSSPID=697a1f72082647aa8639a3d6f865fbba; UM_distinctid=16f116c9505ac-0e2930ad3c765c-7a1b34-13c680-16f116c950629c; _antanalysis_s_id=1576545327630; ne_analysis_trace_id=1576562473736; vinfo_n_f_l_n3=554562770faa4c02.1.0.1576545322211.0.1576562483504; __utma=129633230.916564851.1576234529.1576568423.1576568423.5; utm=eyJjIjoiIiwiY3QiOiIiLCJpIjoiIiwibSI6IiIsInMiOiIiLCJ0IjoiIn0=|aHR0cHM6Ly9zdHVkeS4xNjMuY29tL2NhdGVnb3J5LzQ4MDAwMDAwMzEyMTAyNA==; STUDY_UUID=29f247ad-c513-47e7-918d-de075c575674; __utmb=129633230.22.8.1576574870428";
            
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            var cateList = FindDataForUpdate(dataTableName: DataTableCategoryName);
            foreach (var catObj in cateList)
            {
                initialUrl(catObj.Text("guid"));
            }
               
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“OnlineStudyListCrawler_163.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“OnlineStudyListCrawler_163.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["result"];
           
            var catId = args.urlInfo.UniqueKey;
            if (result != null)
            {
                
                foreach (var item in result)
                {
                    var contentModuleVo = item["contentModuleVo"];
                    var module= GetBsonDocument(item["module"]);
                    foreach (var room in contentModuleVo)
                    {
                        var bsonDoc = GetBsonDocument(room);
                        bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                        bsonDoc.Set("catId", catId);
                        bsonDoc.Set("moduleName", module.Text("moduleName"));
                        bsonDoc.Set("moduleType", module.Text("moduleType"));
                        PushData(bsonDoc,arrayFieldName: "catId");
                    }
                    
                }
     
            }
            
            ShowStatus();
        }

        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        override
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            try
            {
                JObject jsonObj = GetJsonObject(args.Html);
                var result = jsonObj["message"];

                if (result.ToString().ToLower() == "ok")//需要编写被限定IP的处理
                {
                    return false;
                }
                else
                {
                    Console.WriteLine(args.Url);
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
