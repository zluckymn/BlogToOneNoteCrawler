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
    /// http://so.11467.com/cse/search?s=662286683871513660&ie=utf-8&q=%E7%9F%B3%E5%AE%B6%E5%BA%84%E8%B7%AF%E5%8A%B2%E6%88%BF%E5%9C%B0%E4%BA%A7%E5%BC%80%E5%8F%91%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8 
    /// </summary>
    public class EnterpriserListCrawler_ShunQi : SimpleCrawlerBase
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
        public EnterpriserListCrawler_ShunQi(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Enterprise";//房间
            DataTableCategoryName = "LandFang";
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
            industryList = dataop.FindAllByQuery(DataTableCategoryName,Query.NE("_id",-1)).ToList();
        }

        public void initialUrl()
        {
             var dataOp= MongoOpCollection.GetNew121MongoOp_MT(DBCollection.LandFang);
             var url = $"http://so.11467.com/cse/search?s=662286683871513660&ie=utf-8&q=";
            var hitEnterpriseList = dataOp.FindAll(DTCollection.QCCEnterpriseKey_House_Land_Relation,Query.NE("isOther",1)).SetFields(new string[] { "eGuid","ent_Name"});
            
             foreach (var ent in hitEnterpriseList.SetLimit(1)) {
                var name = ent.Text("ent_Name");
                var guid= ent.Text("eGuid");
                var curUrl = $"{url}{HttpUtility.UrlEncode(name).ToUpper()}";
                if (!filter.ContainsAdd(curUrl)) {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = guid, extraData=name });
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
            Settings.ThreadCount =3;
            //Settings.AutoSpeedLimitMinMSecond = 300;
            //Settings.AutoSpeedLimitMinMSecond = 800;
            //Settings.KeepCookie = true;
            //Settings.UseSuperWebClient = true;
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            //Settings.AutoSpeedLimit = true;
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.SimulateCookies = "Hm_lvt_819e30d55b0d1cf6f2c4563aa3c36208=1591007921,1591007953,1591007997,1591325273; Hm_lpvt_819e30d55b0d1cf6f2c4563aa3c36208=1591325485";
            Settings.Referer = "so.11467.com";
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
                var name = args.urlInfo.extraData;
                var guid = args.urlInfo.UniqueKey;
                var htmlDoc = args.Html.HtmlLoad().DocumentNode;
                var companylist = htmlDoc.SelectSingleNode("/ul[@class='companylist']");
                foreach (var company in companylist.ChildNodes)
                {
                    var bsonDoc = new BsonDocument();
                    var mainProduct = QuickGetHtmlNodeValue(company, "主营产品：", "地址：");
                    var urlNode = company.SelectSingleNode("/h4/a");
                    if (urlNode != null)
                    {
                        var url = GetNodeAttribute(urlNode, "href");
                        var ent_name = GetNodeAttribute(urlNode, "title");

                        if (name != ent_name)
                        {
                            continue;
                        }
                        bsonDoc.Set("url_qishun", url);
                        var guid_qishun = GetGuidFromUrl(url, "co/");
                        bsonDoc.Set("guid_qishun", guid_qishun);
                    }
                    bsonDoc.Set("guid", guid);
                    //AddData(bsonDoc);
                    PushData(bsonDoc, "guid");
                }
                ShowStatus();

                Console.WriteLine(args.Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DataReceive" + ex.Message);
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
                
 
                if (args.Html.Contains("companylist"))//需要编写被限定IP的处理
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
