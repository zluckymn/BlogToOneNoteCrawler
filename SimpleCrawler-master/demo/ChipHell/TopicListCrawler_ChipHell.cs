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
using Helper;
using Yinhe.ProcessingCenter.DataRule;
using System.Collections;
using Newtonsoft.Json.Linq;
using LibCurlNet;

namespace SimpleCrawler.Demo
{

    /// <summary>
    ///  https://www.shihuo.cn/app_swoole_goods/list?platform=ios&timestamp=1575601200000&v=6.6.5&token=37720600849a6567cc97de90dd531e8f&groups%5B51%5D=234&lspm=a29406422d41f727778529e0099bf9c1&page=1&range=%E5%B8%BD%E8%A1%AB/%E5%8D%AB%E8%A1%A3&show_type=grid&sort=hot
   /// </summary>
    public class TopicListCrawler_ChipHell : SimpleCrawlerBase
    {

        

#pragma warning disable CS0414 // 字段“TopicListCrawler_ChipHell.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“TopicListCrawler_ChipHell.isUpdate”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“TopicListCrawler_ChipHell.canNextPage”已被赋值，但从未使用过它的值
        bool canNextPage = false;//是否可以跳转下一页
#pragma warning restore CS0414 // 字段“TopicListCrawler_ChipHell.canNextPage”已被赋值，但从未使用过它的值
        const int takeCount = 20;
        

        
        ///品牌
        /// </summary>
        public string DataTableBrands
        {
            get { return "Brands_ShiHuo"; }
        }
        List<BsonDocument> allCategoryList = null;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public TopicListCrawler_ChipHell(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Topic_ChipHell";//商品
            DataTableCategoryName = "Category";//分类
            uniqueKeyField = "guid";
        }
        public void initialUrl(BsonDocument catDoc, int nextIndex)
        {
            
            try
            {
                
               
                //初始化布隆过滤器
                for (var index = nextIndex; index <= nextIndex + 1; index++)
                {
                    //类型
                        var curUrl = catDoc.Text("href");
                      
                         if (!filter.Contains(curUrl))
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = catDoc.ToJson() });
                            filter.Add(curUrl);// 防止执行2次
                        }
                  
                }
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

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
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
           // Settings.HeadSetDic.Add("User-Device", "NTE3MjVhMDk4MjUyNDM5ZGZjN2QxYTg1MzA2MDUxMTF8djYuMC4y");
            //Settings.HeadSetDic.Add("aid", "android1");
            //Settings.HeadSetDic.Add("time", GetTimeStamp());
           // Settings.HeadSetDic.Add("auth", "a1ca3d173b24ebe94d53ed69309c8e06");
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
             
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            //潮流服饰品-438352421 篮球-1963444998 球衣 (guid)1154008570
            var allRoomCat = FindDataForUpdate(dataTableName: DataTableCategoryName, query:Query.And(Query.EQ("level",2)), fields:new string[] { "guid","name","href" });//获取待更新的目录列表
            allCategoryList = dataop.FindAll(DataTableCategoryName).ToList();
            // DataQueueInit(allRoomCat);//加到待爬取列表 ，一个一个爬取
            foreach (var cat in allRoomCat)
            {
                initialUrl(cat, 1);
            }
            //while (DynamicQueue<BsonDocument>.Instance.Count > 0) { 
            // DataDeQueue((cat) => { initialUrl(cat, 1); });//从列表中取出
            //}
            base.SettingInit();



        }
        
        /// <summary>
        /// 需要一直循环直到
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            //var hmtl = args.Html.HtmlLoad();
            
            //var result = jsonObj["data"];
            //var filters = result["filters"];
            //var brands= filters["brands"];
            //var curBrandName = string.Empty;
            //if (brands.Count() > 0)
            //{
            //    curBrandName = brands[0]["name"].ToString();
            //}
            //var catInfo = GetBsonDocument(args.urlInfo.UniqueKey);
            //var pageStr = GetUrlParam(args.urlInfo.UrlString, "page");
            //var items = result["lists"];
            //if (items != null)
            //{
            //    if (items.Count() <= 0)
            //    {
            //        ShowMessage($"无数据{args.urlInfo.PostData}");
            //        return;
            //    }
            //    foreach (var item in items)
            //    {
            //        var bsonDoc = GetBsonDocument(item);
            //        bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
            //        bsonDoc.Set("baseCatId", catInfo.Text("baseCatId"));
            //        bsonDoc.Set("secCatId", catInfo.Text("secCatId"));
            //        bsonDoc.Set("curCatName", catInfo.Text("name"));
            //        bsonDoc.Set("curBrandName", curBrandName);
            //        PushData(bsonDoc);
            //    }
            //}

            //if (items.Count() < takeCount || UrlQueue.Instance.Count == 0)
            //{
            //    ShowMessage($"已到最后一行{items.Count()}");
            //    //取出第二个分类，并更新
            //    //initialUrl(catInfo, 1)
            //    return;
            //}

            //if (pageStr != "")
            //{
              
            //    if (int.TryParse(pageStr, out int pageIndex))
            //    {
            //        var curUrl = args.Url;
            //        curUrl = curUrl.Replace($"page={pageStr}&", $"page={pageIndex + 1}&");
            //        var token = TokenGenerateHelper.TokenGenerater_ShiHuo(curUrl);
            //        curUrl = ReplaceUrlParam(curUrl, "token", token);
            //        if (!filter.Contains(token))
            //        {
            //            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = catInfo.ToJson()});
            //            filter.Add(curUrl);// 防止执行2次
            //        }

            //        ShowMessage($"获取{pageIndex}页数据并初始化成功");
            //    }
            //    else
            //    {
            //        ShowMessage($"转换{pageIndex}出错1");
            //    }

            //}
            //ShowStatus();

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
                var result = jsonObj["msg"];
              
                if (result.ToString() == "ok")//需要编写被限定IP的处理
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
