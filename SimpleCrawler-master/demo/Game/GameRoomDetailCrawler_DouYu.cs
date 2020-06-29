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
    /// 门派url
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class GameRoomDetailCrawler_DouYu : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“GameRoomDetailCrawler_DouYu.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“GameRoomDetailCrawler_DouYu.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public GameRoomDetailCrawler_DouYu(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "GameRoom_DouYu";//注销企业
            updatedValue = "2";
        }
        public void initialUrl()
        {
            var allHitObjList = FindDataForUpdate();
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList.Take(1))
            {
                var curUrl = "https://www.douyu.com/{0}";
                InitialForUpdateUrl(curUrl, hitObj.Text("guid"));
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
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 2;
           
            Settings.Accept = "application/json, text/plain, */*";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            
            Settings.CrawlMode = EnumCrawlMode.PhantomJsViaSelenium;
            
            Settings.operation = new SeleniumOperation() { Timeout = 15 };
            Console.WriteLine("正在获取已存在的url数据");
    
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“GameRoomDetailCrawler_DouYu.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“GameRoomDetailCrawler_DouYu.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            //JObject jsonObj = GetJsonObject(hmtl);
            //var result = jsonObj["result"];
            //var bsonDoc = GetBsonDocument(result);
            //bsonDoc.Set("guid", bsonDoc.Text("id"));
            //UpdateData(bsonDoc);
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
                var result = jsonObj["result"];
                var success = jsonObj["success"];
                if (success == null)//需要编写被限定IP的处理
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }


    }

}
