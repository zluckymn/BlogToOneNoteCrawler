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
    /// boss直聘
    /// https://www.zhipin.com/wapi/zpgeek/miniapp/search/condition.json?appId=10002 
    ///
    /// </summary>
    public class PositionConditionCrawler_Boss : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionConditionCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionConditionCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“PositionConditionCrawler_Boss.canNextPage”已被赋值，但从未使用过它的值
        bool canNextPage = false;//是否可以跳转下一页
#pragma warning restore CS0414 // 字段“PositionConditionCrawler_Boss.canNextPage”已被赋值，但从未使用过它的值
        const int takeCount = 20;
#pragma warning disable CS0414 // 字段“PositionConditionCrawler_Boss.sid”已被赋值，但从未使用过它的值
        string sid = "session.1574834509370652972201";
#pragma warning restore CS0414 // 字段“PositionConditionCrawler_Boss.sid”已被赋值，但从未使用过它的值
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public PositionConditionCrawler_Boss(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "SearchCondition_Boss";//房间
            uniqueKeyField = "code";
        }
        public void initialUrl()
        {
            var curUrl = $"https://www.zhipin.com/wapi/zpgeek/miniapp/search/condition.json?appId=10002";
            if (!filter.Contains(curUrl))
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl));
                filter.Add(curUrl);// 防止执行2次
            }
        }
        override
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 2;
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Settings.HeadSetDic.Add("platform", "zhipin");
            Settings.HeadSetDic.Add("mpt", "1797b2aeab7b59e46119c18805f8540d");
            Settings.HeadSetDic.Add("zpAppId", "10002");
            Settings.HeadSetDic.Add("v", "2");
            Settings.HeadSetDic.Add("wt", "");
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
           //while (DynamicQueue<BsonDocument>.Instance.Count > 0) { 
           // DataDeQueue((cat) => { initialUrl(cat, 1); });//从列表中取出
           //}
           base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionConditionCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“PositionConditionCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 需要一直循环直到
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["zpData"];
            
            var cityGroupList = result["cityGroupList"];//城市 根据字母分类
            var degreeList = result["degreeList"];//学历要求
            var experienceList = result["experienceList"];//经验要求
            var industryList = result["industryList"];//所属行业
            var salaryList = result["salaryList"];//薪资范围
            var scaleList = result["scaleList"];//公司规模
            var stageList = result["stageList"];//融资阶段


            if (cityGroupList != null)
            {

                foreach (var cityGroup in cityGroupList)
                {
                    var cityList = cityGroup["cityList"];//城市列表
                    var firstChar = GetJsonValueString(cityGroup, "firstChar");//首字母
                    foreach (var city in cityList)
                    {
                       var bsonDoc = GetBsonDocument(city);
                       bsonDoc.Set("guid", (bsonDoc.Text(uniqueKeyField)+ bsonDoc.Text("name")).GetHashCode().ToString());
                       bsonDoc.Set("searchType", 0);//城市
                       bsonDoc.Set("firstChar", firstChar);//首字母
                       PushData(bsonDoc);
                    }
                }
            }
            QuickSaveDataList(degreeList, 1);
            QuickSaveDataList(experienceList, 2);
            QuickSaveDataList(industryList, 3);
            QuickSaveDataList(salaryList, 4);
            QuickSaveDataList(scaleList, 5);
            QuickSaveDataList(stageList, 6);
            ShowStatus();

        }
        private void QuickSaveDataList(JToken list, int searchType)
        {
            if (list != null)
            {
                foreach (var item in list)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", (bsonDoc.Text(uniqueKeyField) + bsonDoc.Text("name")).GetHashCode().ToString());
                    bsonDoc.Set("searchType", searchType);//学历要求
                    PushData(bsonDoc);
                }
            }
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
               
                if (result.ToString().ToLower() == "success")//需要编写被限定IP的处理
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
                Console.WriteLine(ex.Message+ args.Url);
                return true;
            }
        }


    }

}
