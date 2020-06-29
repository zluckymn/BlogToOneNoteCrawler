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
    /// 斗鱼房间爬取，太快爬取可能出错 需要
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class GameRoomListCrawler_DouYu : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“GameRoomListCrawler_DouYu.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“GameRoomListCrawler_DouYu.isUpdate”已被赋值，但从未使用过它的值
        bool canNextPage = false;//是否可以跳转下一页
        const int takeCount = 20;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public GameRoomListCrawler_DouYu(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "GameRoom_DouYu";//房间
            DataTableCategoryName = "Game_DouYu";
            uniqueKeyField = "room_id";
        }
        public void initialUrl(string catId, int nextIndex, int pageSize = 1)
        {

            //初始化布隆过滤器
            for (var index = nextIndex; index < nextIndex + pageSize; index++)
            {
                var skipCount = index * takeCount;
                var curUrl = $"https://apiv2.douyucdn.cn/gv2api/rkc/roomlistV1/2_{catId}/{skipCount}/{takeCount}/android?client_sys=android";
                //  var postData = $"Title=&ActivityType=1&SkipCount={skipCount}&MaxResultCount={takeCount}";
                //var postData = "{\"Title\"=&ActivityType=1&SkipCount={skipCount}&MaxResultCount={takeCount}";

                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = nextIndex.ToString(), Authorization = catId });
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
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/json";
            Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 5000;
            //Settings.AutoSpeedLimitMaxMSecond = 10000;
            Settings.UserAgent = "android/6.0.2 (android 5.1.1; ; SM-G955F)";
            Settings.HeadSetDic = new Dictionary<string, string>();
           // Settings.HeadSetDic.Add("User-Device", "NTE3MjVhMDk4MjUyNDM5ZGZjN2QxYTg1MzA2MDUxMTF8djYuMC4y");
            Settings.HeadSetDic.Add("aid", "android1");
            Settings.HeadSetDic.Add("time", GetTimeStamp());
           // Settings.HeadSetDic.Add("auth", "a1ca3d173b24ebe94d53ed69309c8e06");
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip");
           // Settings.HeadSetDic.Add("Cookie", "acf_did=51725a098252439dfc7d1a8530605111");

            //Settings.HeadSetDic.Add("auth", "f8531224e9d8b4708d7f0689cb9d3fb7");
            //Settings.HeadSetDic.Add("dy-app-aname", "%E6%96%97%E9%B1%BC%E7%9B%B4%E6%92%AD");
            //Settings.HeadSetDic.Add("dy-app-pname", "air.tv.douyu.android");
            //Settings.HeadSetDic.Add("phone_system", "5.1.1");
            //Settings.HeadSetDic.Add("timestamp", "1574323236");
            //Settings.HeadSetDic.Add("dy-device-imei", "355757010001598");
            //Settings.HeadSetDic.Add("dy-device-op", "0");
            //Settings.HeadSetDic.Add("dy-device-id", "51725a098252439dfc7d1a8530605111");
            //Settings.HeadSetDic.Add("phone_model", "SM-G955F");
            //Settings.HeadSetDic.Add("client", "android");
            //Settings.HeadSetDic.Add("version", "602");
            //Settings.HeadSetDic.Add("dy-device-devtype", "0");
            //Settings.HeadSetDic.Add("x-dy-traceid", "030badcbbd98debd:030badcbbd98debd:0:023479");
            //Settings.UserAgent = "okhttp/3.12.3";
            //Settings.Accept = "application/vnd.mapi-yuba.douyu.com.4.0+json";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            var allRoomCat = FindDataForUpdate(dataTableName: DataTableCategoryName, fields:new string[] { "cate2_id", "cate2_name" });//获取待更新的目录列表
            //var hasCatIds = dataop.FindFieldsByQuery(DataTableName, null, new string[] { "cate_id" }).Select(c=>c.Text("cate_id")).Distinct().ToList();
            //foreach (var cat in allRoomCat.Where(c=>!hasCatIds.Contains(c.Text("cate2_id"))))
            //{
            //    initialUrl(cat.Text("cate2_id"), 0);
            //}
            DataQueueInit(allRoomCat);
            //qu'y
            DataDeQueue((cat) => { initialUrl(cat.Text("cate2_id"), 0); });
            //foreach (var cat in allRoomCat.Take(2))
            //{
            //    initialUrl(cat.Text("cate2_id"), 0);
            //}
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“GameRoomListCrawler_DouYu.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“GameRoomListCrawler_DouYu.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 需要一直循环直到
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["data"];

            var items = result["list"];
            if (items != null)
            {

                foreach (var item in items)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    PushData(bsonDoc);
                }
            }
            
            if (canNextPage==false||items.Count() <20)
            {
                ShowMessage("已到最后一行");
                //取出第二个分类，并更新
                DataDeQueue((cat) => { initialUrl(cat.Text("cate2_id"), 0); });
                return;
            }
            var index = args.urlInfo.UniqueKey;
            if (index != "")
            {
                var catId = args.urlInfo.Authorization;//当前房间目录，游戏类型
                if (int.TryParse(index, out int pageIndex))
                {
                    initialUrl(args.urlInfo.Authorization, pageIndex+1);
                    ShowMessage($"获取{index}页数据并初始化成功");
                }
                else
                {
                    ShowMessage($"转换{index}出错1");
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
                var result = jsonObj["data"];
                var success = jsonObj["msg"];
                var error = jsonObj["error"];
                if (error.ToString() == "0")//需要编写被限定IP的处理
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
