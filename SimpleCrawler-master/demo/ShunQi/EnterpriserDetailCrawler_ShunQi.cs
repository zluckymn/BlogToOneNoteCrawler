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
using Helper;

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 门派url
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class EnterpriserDetailCrawler_ShunQi : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        List<BsonDocument> industryList = new List<BsonDocument>();
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public EnterpriserDetailCrawler_ShunQi(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "User_ZhengHeDao";//房间
            DataTableCategoryName = "User_ZhengHeDao";
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            industryList = dataop.FindAllByQuery(DataTableCategoryName, Query.NE("_id", -1)).ToList();
        }

        public void initialUrl()
        {
             
            //var curUrl = $"https://www.baidu.com";
            var userlist=FindDataForUpdate(dataTableName: DataTableCategoryName,query:Query.Exists("bizcardDetailVo",false));
            foreach (var user in userlist)
            {
                var guid = user.Text("guid");
                var curUrl = $"https://www.zhisland.com/bms-api-app/user/{guid}/index";
                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = guid });
                    filter.Add(curUrl);// 防止执行2次
                }
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
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount = 1;
            Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 300;
            //Settings.AutoSpeedLimitMinMSecond = 800;
            //Settings.KeepCookie = true;
            //Settings.UseSuperWebClient = true;
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Settings.AutoSpeedLimit = true;
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "okhttp/2.5.0";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip");
            Settings.HeadSetDic.Add("apiVersion", "1.1");
            Settings.HeadSetDic.Add("device_id", "a02efbfd4bcd4a50af658fe41c4a53e2");
            Settings.HeadSetDic.Add("deviceModel", "R8207");
            Settings.HeadSetDic.Add("brand", "OPPO");
            Settings.HeadSetDic.Add("os", "android");
            Settings.HeadSetDic.Add("version", "5.0.7");
            Settings.HeadSetDic.Add("uid", "6670625429163868165");
            Settings.HeadSetDic.Add("atk", "c15f39512ca14d75890a093b4f341c92");
            Settings.HeadSetDic.Add("pageId", "FeedRecommendList");

            Settings.Referer = " www.zhisland.com";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.noCountTimes”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.countPerFolder”已被赋值，但从未使用过它的值
        int countPerFolder = 100;//每个文件夹1000张
#pragma warning restore CS0414 // 字段“EnterpriserListCrawler_ZhengHeDao.countPerFolder”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            try
            {
                var guid = args.urlInfo.UniqueKey;
                var bsonDoc = args.Html.GetBsonDocFromJson();
                bsonDoc.Set("guid", guid);
                    //AddData(bsonDoc);
                PushData(bsonDoc);
                ShowStatus();
                //新增分页?area=110000&industry=ind_01&financing=financing_stage_01&scale=enterprise_employee_num_5&count=20
                
                Console.WriteLine(args.Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DataReceive" + ex.Message);
            }
        }
        private void GetMoreUrl(JToken filterObj, string type, DataReceivedEventArgs args)
        {
            var industryCode = args.urlInfo.UniqueKey;
            var url = args.Url;
            if (filterObj[type] == null) return;
            var filter_areas = filterObj[type];
            foreach (var area in filter_areas)
            {
                var enabled = GetJsonValueString(area, "enabled");
                if (enabled != "1") continue;
                var value = GetJsonValueString(area, "code");
                var curUrl = ReplaceUrlParam(url, type, value);

                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = industryCode });
                    filter.Add(curUrl);// 防止执行2次
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


                if (args.Html.Contains("user"))//需要编写被限定IP的处理
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
