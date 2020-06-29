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
    public class GoodsListCrawler_XHS : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“GoodsListCrawler_XHS.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“GoodsListCrawler_XHS.isUpdate”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“GoodsListCrawler_XHS.canNextPage”已被赋值，但从未使用过它的值
        bool canNextPage = false;//是否可以跳转下一页
#pragma warning restore CS0414 // 字段“GoodsListCrawler_XHS.canNextPage”已被赋值，但从未使用过它的值
        const int takeCount = 20;
        string sid = "session.1574834509370652972201";
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public GoodsListCrawler_XHS(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Goods_XiaoHongShu";//房间
            DataTableCategoryName = "Category_XiaoHongShu";
            uniqueKeyField = "id";
        }
        public void initialUrl(BsonDocument catDoc, int nextIndex, int pageSize = 1)
        {
            try
            {
                var catId = catDoc.Text("id");
                var catName = HttpUtility.UrlEncode(catDoc.Text("name"));
                //初始化布隆过滤器
                for (var index = nextIndex; index <= pageSize; index++)
                {
                    var curUrl = $"https://www.xiaohongshu.com/api/store/ps/products/v1?sid={sid}&keyword={catName}&source=classifications.{catId}&mode=classification&open_page=yes&page={index}&per_page={takeCount}";
                    if (!filter.Contains(curUrl))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = catDoc.ToJson(),  });
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
            Settings.ThreadCount = 2;
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
           // Settings.HeadSetDic.Add("User-Device", "NTE3MjVhMDk4MjUyNDM5ZGZjN2QxYTg1MzA2MDUxMTF8djYuMC4y");
            //Settings.HeadSetDic.Add("aid", "android1");
            //Settings.HeadSetDic.Add("time", GetTimeStamp());
           // Settings.HeadSetDic.Add("auth", "a1ca3d173b24ebe94d53ed69309c8e06");
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
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
            var allRoomCat = FindDataForUpdate(dataTableName: DataTableCategoryName,query:Query.EQ("level",3), fields:new string[] { "id", "name","baseCatId" });//获取待更新的目录列表

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
#pragma warning disable CS0414 // 字段“GoodsListCrawler_XHS.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“GoodsListCrawler_XHS.noCountTimes”已被赋值，但从未使用过它的值
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
            var total_count = int.Parse(jsonObj["data"]["total_count"].ToString());
            var catInfo = GetBsonDocument(args.urlInfo.UniqueKey);
            var pageStr = GetUrlParam(args.Url, "page");
            var items = result["items"];
            if (items != null)
            {

                foreach (var item in items)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    bsonDoc.Set("baseCatId", catInfo.Text("baseCatId"));
                    bsonDoc.Set("curCatId", catInfo.Text("id"));
                    bsonDoc.Set("curCatIdArray", catInfo.Text("id"));
                    PushData(bsonDoc, arrayFieldName: "curCatIdArray");
                }
            }

            if (items.Count() < takeCount || UrlQueue.Instance.Count == 0)
            {
                ShowMessage("已到最后一行");
                //取出第二个分类，并更新
                // DataDeQueue((cat) => { initialUrl(catInfo, 1); });
                //return;
            }

            if (pageStr != "")
            {
                var pageSize = ((total_count-1) / takeCount)+1;
                if (pageSize <= 0)
                {
                    pageSize = 1;
                }
                if (pageSize >= 50)
                {
                    pageSize = 50;
                }
                if (int.TryParse(pageStr, out int pageIndex))
                {
                    initialUrl(catInfo, pageIndex+1, pageSize);
                    ShowMessage($"获取{pageIndex}页数据并初始化成功");
                }
                else
                {
                    ShowMessage($"转换{pageIndex}出错1");
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
                var success =GetJsonValueString(jsonObj, "success") ;
                if (success.ToString() == "True")//需要编写被限定IP的处理
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
