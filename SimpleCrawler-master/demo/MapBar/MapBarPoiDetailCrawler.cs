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
    ///  优先爬取机场G20 与3D0 经济连锁型酒店
    ///
    /// </summary>
    public class MapBarPoiDetailCrawler : SimpleCrawlerBase
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
        public MapBarPoiDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "MapBar_Poi";//房间
            DataTableName_City = "MapBar_City";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "id";
            allCityList = dataop.FindAll(DataTableName_City).ToList();
        }
        List<BsonDocument> allHitObjList;
        public void initialUrl()
        {
            var catQuery = Query.Or(Query.EQ("catCode", "G20"), Query.EQ("catCode", "3D0"));
            //获取最新一条
          //  var latestId = dataop.FindAllByQuery(DataTableName, Query.Exists("lon", false)).SetSortOrder(new SortByDocument() { { "_id", -1 } }).FirstOrDefault().Text("_id");

            foreach (var city in allCityList)
            {
                var cityCode = city.Text("cityCode");
                var query = Query.And(Query.EQ("cityCode", cityCode), Query.Exists("lon", false), catQuery);
                var allCount = dataop.FindCount(DataTableName, query);
                if (allCount <= 0) continue;
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

                allHitObjList = dataop.FindFieldsByQuery(DataTableName, query, new string[] { "guid", "url" }).Skip(skipCount).Take(takeCount).ToList();
                //初始化布隆过滤器
                foreach (var hitObj in allHitObjList)
                {
                    var curUrl = hitObj.Text("url");
                    if (!filter.Contains(curUrl))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = hitObj.Text("guid") });
                        filter.Add(curUrl);// 防止执行2次
                    }

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
            Settings.MaxReTryTimes = 5;

            Settings.ContentType = "text/html; charset=utf-8";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
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
            var html = args.Html;
            var htmlDoc = html.HtmlLoad();
            var telNode = htmlDoc.DocumentNode.SelectSingleNode("//li[@class='telCls']");
            var addressNode = htmlDoc.DocumentNode.SelectNodes("//ul[@class='POI_ulA']/li").Where(c=>c.InnerText.Contains("地址")).FirstOrDefault();
            var guid = args.urlInfo.UniqueKey;
            if (telNode != null&& addressNode!=null)
            {
                var updateDoc = new BsonDocument();
                var tel = telNode.InnerText.Replace("电话：", "").Replace("我来添加", "").Trim();
                var addressArray = addressNode.InnerText.Replace("地址：", "").Replace("\n", "").Replace("\t", "").Replace(" ", "").SplitParam(StringSplitOptions.RemoveEmptyEntries, new string[] { "\r" });
                if (addressArray.Length >= 3)
                {
                    updateDoc.Set("cityName", addressArray[0].Trim());
                    updateDoc.Set("regionName", addressArray[1].Trim());
                    updateDoc.Set("address", addressArray[2].Trim());
                }
                else
                {
                    Console.WriteLine("地址格式不正确");
                 }
                var coordStr = html.ToolsSubStr("coord=", "\">");
                var coordArray = coordStr.SplitParam(new string[] { "," });
                if (coordArray.Count()== 2)
                {
                    updateDoc.Set("lat", coordArray[1]);
                    updateDoc.Set("lon", coordArray[0]);
                }
               
                updateDoc.Set("tel", tel);
                updateDoc.Set("guid", args.urlInfo.UniqueKey);
                
                UpdateData(updateDoc, dataTable: DataTableName);
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
                
                if (args.Html.Contains("coord="))//需要编写被限定IP的处理
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
