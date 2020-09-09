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
using System.Web.UI.WebControls;

namespace SimpleCrawler.Demo
{

    /// <summary>
    ///  
    /// 建材市场网爬取
    ///
    /// </summary>
    public class JianCai_ListCrawler : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 6;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public JianCai_ListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Market_JianCai";//房间
            DataTableCategoryName = "Market_JianCai_City";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";

        }

        List<BsonDocument> allmanHuaList = new List<BsonDocument>();
        string url = "http://m.jinnong.cn/jnwapcompanylistlist_ajax.htm";
        public void initialUrl()
        {
            var provinceList = FindDataForUpdate(dataTableName: DataTableCategoryName, fields:new string[] { "guid","name"});
            foreach (var province in provinceList)
            {
                var guid = province.Text("guid");
                var provinceName = province.Text("name");
                var url = $"http://www.yunhesaitu.com/wapsc.asp?page=1&class1=504&sheng={guid}";
                if (!filter.Contains(url))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = guid,  extraData= provinceName });
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
            Settings.UserAgent = "Mozilla/5.0 (Linux; Android 6.0.1; MuMu Build/V417IR; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/52.0.2743.100 Mobile Safari/537.36";
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //Settings.HeadSetDic.Add(":scheme", "https");
            //Settings.HeadSetDic.Add(":path", "/Argeon_Highmayne");
            //Settings.HeadSetDic.Add(":method", "GET");
            //Settings.HeadSetDic.Add(":authority", "duelyst.gamepedia.com");
            Settings.SimulateCookies = "ASPSESSIONIDQADQQRSQ=PFGDBOACCKOPJIDKPKGDBFPF; __51cke__=; Hm_lvt_7f9d27d20b571e05a234f682001f9d92=1599450696,1599450699; ASPSESSIONIDQCASRQTQ=BNEHOJNCLJHCFFLLHIEEPBHB; __tins__14397883=%7B%22sid%22%3A%201599462426319%2C%20%22vd%22%3A%206%2C%20%22expires%22%3A%201599464898377%7D; __51laig__=71; Hm_lpvt_7f9d27d20b571e05a234f682001f9d92=1599463098";
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
            var guid = args.urlInfo.UniqueKey;
            var province = args.urlInfo.extraData;
            var page = GetUrlParam(args.Url, "page");
            var hmtl = args.Html;
            var root = hmtl.HtmlLoad().DocumentNode;
            var pageCount = 0;
            if (root == null) return;
            var finalPageANode = root.SelectNodes("//a").Where(c => c.InnerText.Contains("尾页")).FirstOrDefault();
            if (finalPageANode != null)
            {
                var url = finalPageANode.GetAttributeValue("href", "").Replace("&amp;","&");
                if (!string.IsNullOrEmpty(url))
                {
                    var pageCountStr = GetUrlParam(url, "page");
                    if (!string.IsNullOrEmpty(pageCountStr))
                    {
                       if(int.TryParse(pageCountStr,out pageCount))
                       { 
                        
                       }
                    }
                    
                }
               
            }
            var dataList = root.SelectNodes("//a").Where(c=>c.GetAttributeValue("href","").Contains("wapscinfo.asp?id=")).ToList();
            if (dataList != null)
            {
              

                foreach (var aNode in dataList)
                {
                    var poiDoc = new BsonDocument();
                    var href = aNode.GetAttributeValue("href", "");
                    var poi_guid = GetUrlParam(href,"id");
                    if (!string.IsNullOrEmpty(poi_guid))
                    {
                        poiDoc.Set("url", href);
                        poiDoc.Set("guid", poi_guid);
                        poiDoc.Set("province", province);
                        PushData(poiDoc);
                    }
               
                }
                
            }
            if (pageCount!=0 && (page==""|| page=="1"))
            {
              
                var url = args.urlInfo.UrlString;
               
                for (var index =2; index <= pageCount; index++)
                {
                    url = ReplaceUrlParam(url, "page", index.ToString(), "?", true);
                    if (!filter.Contains(url))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = args.urlInfo.UniqueKey,extraData= args.urlInfo.extraData });
                        filter.Add(url);// 防止执行2次
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

                if (args.Html.Contains("wapscinfo.asp?id="))//需要编写被限定IP的处理
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
