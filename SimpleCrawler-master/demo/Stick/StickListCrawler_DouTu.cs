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
    public class StickListCrawler_DouTu : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“StickListCrawler_DouTu.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“StickListCrawler_DouTu.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public StickListCrawler_DouTu(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Stick_DouTu";//房间
            DataTableCategoryName = "Stick_Tags_DouTu";//condition
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
        }

        public void initialUrl()
        {

            var endIndex = 3368;
            //endIndex = 1;
            for (var index=1;index<= endIndex; index++)
            {
               var curUrl = $"https://www.doutula.com/photo/list/?page={index}";
                //var curUrl = $"https://www.baidu.com";
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) {  });
                filter.Add(curUrl);// 防止执行2次
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
            //Settings.KeepCookie = true;
            //Settings.UseSuperWebClient = true;
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Settings.AutoSpeedLimit = true;
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.8(0x17000820) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Settings.HeadSetDic.Add("platform", "zhipin");
            Settings.HeadSetDic.Add("mpt", "17ab75f3be34ba26d4d6a1fa0f5c6b68");
            Settings.HeadSetDic.Add("zpAppId", "10002");
            Settings.HeadSetDic.Add("v", "2");
            Settings.HeadSetDic.Add("wt", "");
            Settings.Referer = "https://www.doutula.com";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“StickListCrawler_DouTu.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“StickListCrawler_DouTu.noCountTimes”已被赋值，但从未使用过它的值
        int countPerFolder = 100;//每个文件夹1000张
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {

            var hmtl = args.Html;
            HtmlDocument HtmlDoc = args.Html.HtmlLoad();
            var root = HtmlDoc.DocumentNode;
            if (root != null)
            {
                var aNodeList = root.SelectNodes("//a[@class='col-xs-6 col-sm-3']").Where(c=>c.GetAttributeValue("href","").Contains("https://www.doutula.com/photo/"));//城市筛选
                foreach (var aItem in aNodeList)
                {
                    var bsonDoc = new BsonDocument();
                    var href = aItem.GetAttributeValue("href","");
                    var id = GetGuidFromUrl(href,"/","|END|");
                    var imgFolderIndex = Math.Abs(id.GetHashCode()) % countPerFolder;
                    var imgFolderName = $"DouTuImage/{imgFolderIndex}";
                    bsonDoc.Set("guid", id);
                    bsonDoc.Set("imgFolderName", imgFolderName);
                    //获取图片地址与标签
                    var imgNode = aItem.ChildNodes.Where(c => c.Name == "img"&&c.GetAttributeValue("alt","")!="").FirstOrDefault();
                    if (imgNode != null)
                    {
                        var alt = imgNode.GetAttributeValue("alt", "");//标签
                        var src= imgNode.GetAttributeValue("src", "");//图片路径
                        var src_original = imgNode.GetAttributeValue("data-original", "");//标签
                        var src_backup = imgNode.GetAttributeValue("data-backup", "");//标签
                        var imgUrl = src.StartsWith("http") ? src : src_original;
                        
                        bsonDoc.Set("alt", alt);
                        bsonDoc.Set("src", src);
                        bsonDoc.Set("src_original", src_original);
                        bsonDoc.Set("src_backup", src_backup);
                        if (!string.IsNullOrEmpty(src))
                        {
                            var fileName = GetGuidFromUrl(imgUrl, "/", "|END|");
                            var file = new FileInfo(fileName);
                            var ext = file.Extension;
                            var newFileName = $"{id}{ext}";
                            bsonDoc.Set("fileName", newFileName);
                         }
                    }
                    PushData(bsonDoc,addAction:(doc)=> {
                        var src = doc.Text("src");
                        var src_original = doc.Text("src_original");
                        var src_backup = doc.Text("src_backup");
                        var imgUrl = src.StartsWith("http") ? src : src_original;
                        var newFileName= doc.Text("fileName");
                        if (!string.IsNullOrEmpty(imgUrl))
                        {
                            DownLoadFile(imgUrl, newFileName, imgFolderName);
                        }
                    });
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
                
 
                if (args.Html.Contains("random_picture"))//需要编写被限定IP的处理
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
