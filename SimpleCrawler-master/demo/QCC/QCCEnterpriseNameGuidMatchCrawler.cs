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
using System.Threading;

namespace SimpleCrawler.Demo
{

    /// <summary>
    ///通过名称进行关键字匹配
    ///
    /// </summary>
    public class QCCEnterpriseNameGuidMatchCrawler : SimpleCrawlerBase
    {


        int maxCount_ChangeProxy = 20;//
        const int takeCount = 8;
        int curThreadId =0;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QCCEnterpriseNameGuidMatchCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_OtherEnterprise_Land_Relation";//房间
            //DataTableName = "QCCEnterpriseKey_YangQiDetial";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "name";
        }
       
        public void initialUrl()
        {
           
            Console.WriteLine("开始载入数据");
             var query = Query.And(Query.EQ("isHouse",1),Query.Exists("eGuid", false));

            // query = Query.And(query, Query.NE("status", "吊销"), Query.NE("status", "注销"));
            var fields = new string[] { "name" };
           
            allCount = (int)dataop.FindCount(DataTableName, query);
            var skipCount = 0;
            var limitCount = 1000000;
            if (allCount >= 10000)
            {
                skipCount = new Random().Next(0, 1000);
            }
            else
            {
                skipCount = 0;
            }
           // var allHitObjList = dataop.FindLimitFieldsByQuery(DataTableName, query, new MongoDB.Driver.SortByDocument() { { "_id", 1 } }, skipCount, limitCount, fields);
            var allHitObjList = dataop.FindAllByQuery(DataTableName, query).SetSkip(skipCount).SetLimit(limitCount).SetFields(fields);

            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {

                var guid = hitObj.Text("name");
                var curUrl = $"https://www.qcc.com/gongsi_mindlist?type=mind&searchKey={HttpUtility.UrlEncode(guid)}&searchType=0";
                
                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = guid });
                    filter.Add(curUrl);// 防止执行2次
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
            Settings.MaxReTryTimes = 5;
           // Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "*/*";
            Settings.AutoSpeedLimit = true;
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.SimulateCookies = "_uab_collina=158632359219467072927264; CNZZDATA1254842228=475275439-1586322401-%7C1588934343; hasShow=1; acw_tc=73dc082015863235914835304e1018a1f71387f176d124f8a29d52262c; Hm_lpvt_78f134d5a9ac3f92524914d0247e70cb=1588934625; QCCSESSID=r2q2r5lp95bbbvcke7ib6gt0r4; UM_distinctid=1715840f75e2-02fb8b34093d84-71415a3b-13c680-1715840f75f559; Hm_lvt_78f134d5a9ac3f92524914d0247e70cb=1588226381,1588229806,1588728770,1588923646; zg_did=%7B%22did%22%3A%20%221715840f68023e-001573e2671408-71415a3b-13c680-1715840f682171%22%7D; zg_de1d1a35bfa24ce29bbf2c7eb17e6c4f=%7B%22sid%22%3A%201588933307608%2C%22updated%22%3A%201588934624898%2C%22info%22%3A%201588831405927%2C%22superProperty%22%3A%20%22%7B%7D%22%2C%22platform%22%3A%20%22%7B%7D%22%2C%22utm%22%3A%20%22%7B%7D%22%2C%22referrerDomain%22%3A%20%22www.qcc.com%22%2C%22zs%22%3A%200%2C%22sc%22%3A%200%2C%22cuid%22%3A%20%22b2e67a32a6abca173d32cf6c794e20f8%22%7D";
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate, br");
            Settings.HeadSetDic.Add("X-Requested-With", "XMLHttpRequest");
            Settings.Referer = "https://www.qcc.com/";
            //Settings.CurWebProxy= GetProxy();
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();
 
        }
 
#pragma warning disable CS0414 // 字段“EnterpriserDetailCrawler_ZhengHeDao.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“EnterpriserDetailCrawler_ZhengHeDao.noCountTimes”已被赋值，但从未使用过它的值
        int allCount = 0;
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            if (curThreadId==0)
            {
                curThreadId = Thread.CurrentThread.ManagedThreadId;
            }
            if (allCount>100&&UrlQueue.Instance.Count <= 100&&Thread.CurrentThread.ManagedThreadId== curThreadId)
            {
                Console.WriteLine("开始加载新数据");
                initialUrl();
            }
            var guid = args.urlInfo.UniqueKey;
            
            var html = args.Html;
            var htmlObj = html.HtmlLoad();
            var baseInfNode = htmlObj.DocumentNode.SelectNodes("./div[@class='list-group nsearch-list']").FirstOrDefault();
            var updateBson = new BsonDocument();
            if (baseInfNode != null)
            {
                var aNode = baseInfNode.ChildNodes.Where(c => c.Name == "a").FirstOrDefault();
                if (aNode == null) return;
                var href = GetNodeAttribute(aNode, "href");
                var name = Toolslib.Str.Sub(aNode.OuterHtml, "addSearchIndex('", "',");
                updateBson.Set("href", href);
                var eGuid= Toolslib.Str.Sub(href, "_", ".html");
                updateBson.Set("matchName", name);
                updateBson.Set("name", guid);
                updateBson.Set("eGuid", eGuid);
                PushData(updateBson,keyFiled:"name");
                Console.WriteLine($"{allCount}获取到数据{name}_{eGuid}_{href}");
            }
            else
            {
                
                ////异常访问 更换代理
                //Console.WriteLine("异常数据");
                //var invalidUpdate = new BsonDocument();
                //invalidUpdate.Set("isDetailUpdate_web", -1);
                //invalidUpdate.Set("guid", guid);
                //PushData(invalidUpdate);

            }
            ShowStatus();
           
            if (updateCount > 0&& updateCount % maxCount_ChangeProxy == 0)
            {
                //手动更新ip
                QuickProxyPoolHelper.Instance().ExecChangeIp();
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
                var html = args.Html;
                
                if (html.Contains("list-group nsearch-list"))
                {
                    return false;
                }
                else
                {
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
