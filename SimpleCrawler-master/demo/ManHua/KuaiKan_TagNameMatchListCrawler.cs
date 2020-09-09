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
    /// https://www.kuaikanmanhua.com/v1/search/by_tag?since=0&count=48&f=3&tag=20&sort=1&query_category=%7B%22update_status%22:1%7D 
    ///
    /// </summary>
    public class KuaiKan_TagNameMatchListCrawler : SimpleCrawlerBase
    {

        
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 48;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public KuaiKan_TagNameMatchListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "ManHua_Category_KuaiKan";//房间
            DataTableCategoryName = "ManHua_Category_KuaiKan";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
        }

        public void initialUrl()
        {

            var tags = dataop.FindAll(DataTableCategoryName).SetFields("name", "guid").ToList();
            
            foreach (var hitTag in tags)
            {
                var key = hitTag.Text("guid");
                var type = hitTag.Text("name");
                var url = $"https://search.kkmh.com/search/complex?q={HttpUtility.UrlEncode(type)}&uuid=d01276be-346c-4440-851e-ba3316f94933&entrance=1";
                if (!filter.Contains(url)) {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = key, extraData= type });
                    filter.Add(url);// 防止执行2次
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
            Settings.ThreadCount = 1;
            Settings.MaxReTryTimes = 10;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            //Settings.ContentType = "application/json; charset=UTF-8";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "application/json, text/plain, */*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate, br");
            //Settings.HeadSetDic.Add(":scheme", "https");
            //Settings.HeadSetDic.Add(":path", "/Argeon_Highmayne");
            //Settings.HeadSetDic.Add(":method", "GET");
            //Settings.HeadSetDic.Add(":authority", "duelyst.gamepedia.com");
           //Settings.SimulateCookies = "Geo={%22region%22:%22FJ%22%2C%22country%22:%22CN%22%2C%22continent%22:%22AS%22}; _ga=GA1.2.987882347.1594083335; __qca=P0-1227115721-1594083385201; __gads=ID=b05f8069fced27fa:T=1594083398:S=ALNI_MaHAGtfVP3fAbyrBDkvfMj4w7Oz-w; vector-nav-p-Factions=true; crfgL0cSt0r=true; _gid=GA1.2.1608004051.1594206130; wikia_beacon_id=3Dmw2iT6gy; tracking_session_id=nwxlyfoG9c; ___rl__test__cookies=1594206451799; OUTFOX_SEARCH_USER_ID_NCOO=76744037.13684113; _gat_tracker0=1; _gat_tracker1=1; mnet_session_depth=1%7C1594207140419; pv_number=9; pv_number_global=9; _sg_b_p=%2FLyonar_Kingdoms%2C%2FArgeon_Highmayne%2C%2FSonghai_Empire%2C%2FVetruvian_Imperium%2C%2FAbyssian_Host%2C%2FMagmar_Aspects%2C%2FVanar_Kindred%2C%2FNeutral%2C%2FSonghai_Empire%2C%2FArgeon_Highmayne; _sg_b_v=2%3B1542%3B1594206133";
            Settings.Referer = "duelyst.gamepedia.com";
            Settings.SimulateCookies = "nickname=%257F; sajssdk_2015_cross_new_user=1; sensorsdata2015jssdkcross=%7B%22distinct_id%22%3A%221737b3298d64fb-095586bbdcff62-7a1b34-1296000-1737b3298d72fa%22%2C%22first_id%22%3A%22%22%2C%22props%22%3A%7B%22%24latest_traffic_source_type%22%3A%22%E8%87%AA%E7%84%B6%E6%90%9C%E7%B4%A2%E6%B5%81%E9%87%8F%22%2C%22%24latest_search_keyword%22%3A%22%E6%9C%AA%E5%8F%96%E5%88%B0%E5%80%BC%22%2C%22%24latest_referrer%22%3A%22https%3A%2F%2Fwww.baidu.com%2Flink%22%7D%2C%22%24device_id%22%3A%221737b3298d64fb-095586bbdcff62-7a1b34-1296000-1737b3298d72fa%22%7D; Hm_lvt_c826b0776d05b85d834c5936296dc1d5=1595499781,1595501273,1595502765; kk_s_t=1595502819311; Hm_lpvt_c826b0776d05b85d834c5936296dc1d5=1595503186";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            //var areas = new string[] { "040010180"};//, "040010180", "040010100", "040010130" "040010220"
            //foreach (var area in areas)
            //{
            //    initialUrl(area,0 );
            //}
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
         
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var tagValue = args.urlInfo.UniqueKey;
            var tagName = args.urlInfo.extraData;
            var hmtl = args.Html;
            var root = hmtl.GetBsonDocFromJson();
          
            if (root == null) return;
           
            var data = root.GetBsonDocument("data");
            if (data != null)
            {
                var catDoc= data.GetBsonDocument("category");
                if (catDoc != null && catDoc.ElementCount > 0) {
                    var catUpdateDoc = new BsonDocument();
                    catUpdateDoc.Set("guid", tagValue);
                    catUpdateDoc.Set("title", catDoc.Text("title"));
                    catUpdateDoc.Set("id", catDoc.Text("id"));
                    PushData(catUpdateDoc);
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

                if (args.Html.Contains("data"))//需要编写被限定IP的处理
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
