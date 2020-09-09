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
using Helper.ConsistentHash;
using Helper;

namespace SimpleCrawler.Demo
{

    /// <summary>
    /// 门派url
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class EnterpriseDetailCrawler_XiaoLanBen : SimpleCrawlerBase
    {

        MongoOperation dataOp_New = MongoOpCollection.GetNew121MongoOp_MT("EnterpriseDetailInfo_XiaoLanBen");
#pragma warning disable CS0414 // 字段“EnterpriseDetailCrawler_XiaoLanBen.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“EnterpriseDetailCrawler_XiaoLanBen.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public EnterpriseDetailCrawler_XiaoLanBen(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_ThirdIndustry";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
        }
       
        public void initialUrl()
        {
            var mongoDb = MongoOpCollection.Get121MongoOp("SimpleCrawler");
            Console.WriteLine("开始载入数据");
            var query =  Query.And(Query.Exists("isDetailUpdate", false), Query.Exists("eid", true));
            allCount = dataop.FindCount(DataTableName, query);
            var skipCount = 0;
            var limitCount = 100;
            if (allCount >= 1000)
            {
                skipCount = new Random().Next(0, 100);
            }
            else {
                skipCount =0;
            }
            //var allHitObjList = dataop.FindLimitFieldsByQuery(DataTableName, query, new MongoDB.Driver.SortByDocument() { { "_id",1} } ,skipCount, limitCount, new string[] { "guid", "credit_no", "name", "eid" });
            var allHitObjList = mongoDb.FindAll(DataTableName, query).SetSkip(skipCount).SetLimit(limitCount).SetFields(new string[] { "guid", "credit_no", "name", "eid" });
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                var eid = hitObj.Text("eid");
                var guid = hitObj.Text("guid");
               
                var keyWord = hitObj.Text("credit_no");
                if (string.IsNullOrEmpty(keyWord))
                {
                    keyWord = hitObj.Text("name");
                }
                var curUrl = $"https://www.baidu.com/?guid={guid}&keyWord={keyWord}&eid={eid}";
                InitialForUpdateUrl(curUrl, guid);
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
            Settings.ThreadCount = 4;
            Settings.MaxReTryTimes = 5;
            Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
             Settings.AutoSpeedLimit = true;
            Settings.AutoSpeedLimitMinMSecond = 100;
            Settings.AutoSpeedLimitMinMSecond = 2000;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“EnterpriseDetailCrawler_XiaoLanBen.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“EnterpriseDetailCrawler_XiaoLanBen.noCountTimes”已被赋值，但从未使用过它的值
        int allCount = 0;
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
            var guid = GetUrlParam(args.Url,"guid");
            var keyWord = GetUrlParam(args.Url, "keyWord");
            var eid= GetUrlParam(args.Url, "eid");
           // var curXiaolanBenAppHelper = new XiaoLanBenAppHelper();
            //curXiaolanBenAppHelper.webProxy = Settings.CurWebProxy;
           // var enterpriseDetail = curXiaolanBenAppHelper.SearchEnterpriseDetailByEid(eid, sleepTime: 0);
           // if (enterpriseDetail == null)
           // {
           //     return ;
           // }
           // var updateBsonDoc = new BsonDocument();
           // if (!enterpriseDetail.ContainsColumn("basicInfo"))
           // {
           //     updateBsonDoc.Set("isDetailUpdate", 2);
           //     UpdateData(updateBsonDoc, DataTableName, Query.EQ("guid", guid));
           //     Console.WriteLine("无获取到匹配数据");
           //     return;
           // }
           
           // var baseInfo = enterpriseDetail.GetBsonDocument("basicInfo");
           // updateBsonDoc.Set("isDetailUpdate", 1);

           // updateBsonDoc.Set("paid_in_capi", baseInfo.Double("acConam"));
           // updateBsonDoc.Set("insuredPersonsNum", baseInfo.Double("ssNum"));
           // updateBsonDoc.Set("checkDate", baseInfo.Text("updateTime"));

           //// DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBsonDoc, Name = DataTableName, Query = Query.EQ("guid", guid), Type = StorageType.Update });
           // UpdateData(updateBsonDoc, DataTableName, Query.EQ("guid", guid));
           // //添加到新数据库
           // enterpriseDetail.Set("eid", baseInfo.Text("eid"));
           // enterpriseDetail.Set("guid", guid);
           // var node = QuickConsistentHashHelper.Instance_EnterpriseGuid().GetHashItem(guid);
           // var tableName_Detail = node.Type;
           // dataOp_New.UpdateOrInsert(tableName_Detail, Query.EQ("guid", guid), enterpriseDetail);
            addCount++;
            ShowStatus();
            Console.WriteLine($"总个数{allCount}");
            if (UrlQueue.Instance.Count <= 0)
            {
                initialUrl();
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
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return true;
            }
        }


    }

}
