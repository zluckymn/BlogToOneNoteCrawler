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
using MongoDB.Driver;
using Helper;
using Helpers;
namespace SimpleCrawler.Demo
{

    /// <summary>
    ///  
    ///  中国建材城数据爬取
    ///  
    /// </summary>
    public class JianCai_DetailCrawler : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“MapBarPoiDetailCrawler.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“MapBarPoiDetailCrawler.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 10000;
        public string DataTableName_City = "";
        public List<BsonDocument> allCityList;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public JianCai_DetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Market_JianCai";//房间
            DataTableName_City = "Market_JianCai";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            allCityList = dataop.FindAll(DataTableName_City).ToList();
        }
        List<BsonDocument> allHitObjList;
        public void initialUrl()
        {
           // var catQuery = Query.Or(Query.EQ("catCode", "G20"), Query.EQ("catCode", "3D0"));
            // var catQuery = Query.Or(Query.EQ("catCode", "G20"), Query.EQ("catCode", "3D0"));
            //获取最新一条
            //  var latestId = dataop.FindAllByQuery(DataTableName, Query.Exists("lon", false)).SetSortOrder(new SortByDocument() { { "_id", -1 } }).FirstOrDefault().Text("_id");

            var query = Query.And(Query.Exists("name", false));
            var allCount = dataop.FindCount(DataTableName, query);
         
            var random = new Random();
            var skipCount = random.Next(0, takeCount);
            if (allCount > 10 * takeCount)
            {
                skipCount = random.Next(0, takeCount);
            }
            else
            {
                if (allCount <= takeCount)
                {
                    skipCount = 0;
                }
                else
                {
                    skipCount = random.Next(0, allCount);
                }
            }

            allHitObjList = dataop.FindFieldsByQuery(DataTableName, query, new string[] { "guid"}).Skip(skipCount).Take(takeCount).ToList();
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                var guid = hitObj.Text("guid");
                var url = $"http://www.yunhesaitu.com/wapscinfo.asp?id={guid}";
                if (!filter.Contains(url))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = guid });
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
            Settings.ThreadCount = 5;
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
#pragma warning disable CS0414 // 字段“MapBarPoiDetailCrawler.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“MapBarPoiDetailCrawler.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            if (CanLoadNewData())
            {
                initialUrl();
            }
            var guid = args.urlInfo.UniqueKey;
            var html = args.Html;
            var htmlDoc = html.HtmlLoad().DocumentNode;

            var updateDoc = new BsonDocument();
            updateDoc.Set("guid", guid);
            var tableDiv = html.HtmlLoad().GetElementbyId("table95");
            if (tableDiv != null)
            {
                var tableStr = tableDiv.InnerText.Replace("&nbsp;","");
                var name = tableStr.ToolsSubStr("市场名称", "联 系 人").Replace("：","").Trim();
                var person = tableStr.ToolsSubStr("联 系 人", "联系电话").Replace("：", "").Trim();
                var telphone = tableStr.ToolsSubStr("联系电话", "营业时间").Replace("：", "").Trim();
                var workTime = tableStr.ToolsSubStr("营业时间", "地址").Replace("：", "").Trim();
                var address = tableStr.ToolsSubStr("地址", "导购路线").Replace("：", "").Trim();
                var shoppingGuide = tableStr.ToolsSubStr("导购路线", "所属区域").Replace("：", "").Trim();
                var area = tableStr.ToolsSubStr("所属区域", "关注程度").Replace("：", "").Trim();
                var interesingLevel = tableStr.ToolsSubStr("关注程度", "市场类型").Replace("：", "").Trim();
                var type = tableStr.ToolsSubStr("市场类型", "综合评分").Replace("：", "").Trim();
                updateDoc.Set("name", name);
                updateDoc.Set("person", person);
                updateDoc.Set("telphone", telphone);
                updateDoc.Set("workTime", workTime);
                updateDoc.Set("address", address);
                updateDoc.Set("shoppingGuide", shoppingGuide);
                updateDoc.Set("area", area);
                updateDoc.Set("interesingLevel", interesingLevel);
                updateDoc.Set("type", type);
            }
            var tableDetailDiv = html.HtmlLoad().GetElementbyId("table62");
            if (tableDetailDiv != null)
            {
                updateDoc.Set("intro", tableDetailDiv.InnerText.Trim());
                updateDoc.Set("intro_html", tableDetailDiv.InnerHtml);
            }
            var firImg = html.HtmlLoad().DocumentNode.SelectNodes("//img").Where(c=>c.GetAttributeValue("alt","默认图片")== updateDoc.Text("name")).FirstOrDefault();
            if (firImg != null)
            {
                updateDoc.Set("imgUrl",$"http://www.yunhesaitu.com/{firImg.GetAttributeValue("src", "")}");
            }
            UpdateData(updateDoc, dataTable: DataTableName);
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

                if (args.Html.Contains("市场名称"))//需要编写被限定IP的处理
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
