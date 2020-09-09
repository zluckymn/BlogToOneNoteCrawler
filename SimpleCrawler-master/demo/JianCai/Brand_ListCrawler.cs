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
    public class Brand_ListCrawler : SimpleCrawlerBase
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
        public Brand_ListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Brand_JianCai";//房间
            DataTableCategoryName = "Brand_JianCai_Category";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";

        }
        public string DataTableNameCompany { 
            get { return DataTableName + "_Company"; }
        }

        List<BsonDocument> allmanHuaList = new List<BsonDocument>();
        string url = "http://m.jinnong.cn/jnwapcompanylistlist_ajax.htm";
        public void initialUrl()
        {
            var provinceList = FindDataForUpdate(dataTableName: DataTableCategoryName, fields:new string[] { "guid","name","href"});
            foreach (var province in provinceList)
            {
                var url = province.Text("href");
                var catName = province.Text("name");
                
                if (!filter.Contains(url))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = catName,extraData="1"});
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
            Settings.ThreadCount = 10;
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
            Settings.SimulateCookies = "__cfduid=ddf85f86fe1bd24643ffcb3b84935ac7d1599457212; ASP.NET_SessionId=5n0lfe0wot5iztfaoo2xxvqy; UM_distinctid=17467143e423c0-0ed574ea951837-7a1b34-13c680-17467143e4313c; Hm_lvt_dfc52f0a9da3ba51632f28e81ae59735=1599457214; Hm_lvt_532294200876036f33e1cfea0ef2d9ab=1599457244; CNZZDATA2671330=cnzz_eid%3D1162254094-1599453102-%26ntime%3D1599479146; Hm_lpvt_dfc52f0a9da3ba51632f28e81ae59735=1599480658; Hm_lpvt_532294200876036f33e1cfea0ef2d9ab=1599480658";
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
            var catName = args.urlInfo.UniqueKey;
             
            var page = args.urlInfo.extraData;
            var hmtl = args.Html;
            var root = hmtl.HtmlLoad();
            var pageCount = 0;
        
            if (root == null) return;
            var itemNode = root.GetElementbyId("item");
            if (itemNode != null)
            {
                var brandNodeList = itemNode.SelectNodes("//a").Where(c => c.GetAttributeValue("href", "").Contains("brand-")).ToList();
                foreach (var brandNode in brandNodeList)
                {
                    var brandName = brandNode.InnerText.Trim();
                    if (brandName.Contains("了解详情")) continue;
                    var href = brandNode.GetAttributeValue("href","");
                    var guid = href.ToolsSubStr("brand-", "/");
                    var updateDoc = new BsonDocument();
                    updateDoc.Set("guid", guid);
                    updateDoc.Set("name", brandName);
                    updateDoc.Set("catName", catName);
                    PushData(updateDoc);
                }
                var companyNodeList= itemNode.SelectNodes("//a").Where(c => c.GetAttributeValue("href", "").Contains("c-")).ToList();
               
                foreach (var companyNode in companyNodeList)
                {
                    var companyName = companyNode.InnerText.Trim();
                    if (companyName.Contains("了解详情")|| companyName.Contains("主页")) continue;
                    var href = companyNode.GetAttributeValue("href", "");
                    var guid = href.ToolsSubStr("c-", "/");
                    var updateDoc = new BsonDocument();
                    if (!string.IsNullOrEmpty(guid))
                    {
                        updateDoc.Set("guid", guid);
                        updateDoc.Set("name", companyName);
                        updateDoc.Set("catName", catName);
                        PushData(updateDoc, dataTable: DataTableNameCompany);
                    }
                   
                }
            }
            var pageCountNode = root.DocumentNode.SelectNodes("//a").Where(c => c.InnerText.Contains("尾页")).FirstOrDefault();
            if (pageCountNode != null)
            {
                var  pageCountUrl= pageCountNode.GetAttributeValue("href", "");
                var  pageCountStr= pageCountUrl.Replace(args.Url,"").Replace("/","");
                int.TryParse(pageCountStr, out pageCount);
            }
            if (pageCount!=0 && (page==""|| page=="1"))
            {
              
                var url = args.urlInfo.UrlString;
               
                for (var index =2; index <= pageCount; index++)
                {
                    url = $"{args.urlInfo.UrlString}{index}/";
                    if (!filter.Contains(url))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = args.urlInfo.UniqueKey, extraData = index .ToString()});
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

                if (args.Html.Contains("brand-"))//需要编写被限定IP的处理
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
