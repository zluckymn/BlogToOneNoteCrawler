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
    /// 门派url
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class PositionDetailCrawler_Boss : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionDetailCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionDetailCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public PositionDetailCrawler_Boss(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Note_XiaoHongShu";//房间
            DataTableCategoryName = "Goods_XiaoHongShu";//商品
            updatedValue = "3";//是否更新字段
            uniqueKeyField = "id";
        }
        List<BsonDocument> allHitObjList;
        public void initialUrl()
        {
            allHitObjList = FindDataForUpdate(dataTableName: DataTableCategoryName,fields:new string[] { "guid","baseCatId"});
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                var curUrl = "https://www.xiaohongshu.com/api/store/jpd/notes/{0}/notes_detail?page=1&per_page=10";
                InitialForUpdateUrl(curUrl, hitObj.Text("guid"));
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
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionDetailCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“PositionDetailCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["data"];
            var goodsId = args.urlInfo.UniqueKey;
            if (result != null)
            {
                var hitGoods = allHitObjList.Where(c => c.Text("guid") == goodsId).FirstOrDefault();
                foreach (var item in result)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    bsonDoc.Set("goodsId", goodsId);
                    if (hitGoods != null)
                    {
                        bsonDoc.Set("baseCatId", hitGoods.Text("baseCatId"));
                        bsonDoc.Set("baseCatIdArray", hitGoods.Text("baseCatId"));
                    }
                    PushData(bsonDoc, arrayFieldName: "baseCatIdArray");
                }
                  
                 var updateDoc = new BsonDocument();
                 updateDoc.Set("guid", args.urlInfo.UniqueKey);
                 updateDoc.Set(updatedField, updatedValue);
                 UpdateData(updateDoc,dataTable: DataTableCategoryName);
                
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
                var success = GetJsonValueString(jsonObj, "success");
                if (success.ToString().ToLower() == "true")//需要编写被限定IP的处理
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
