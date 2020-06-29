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
    /// 门派url
    /// https://menpai.member.fun/api/Activity/GetActivityList
    ///
    /// </summary>
    public class QCCEnterpriseWebPositionDetailInfoCrawler : SimpleCrawlerBase
    {


        int maxCount_ChangeProxy = 20;//
        const int takeCount = 8;
        int curThreadId =0;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QCCEnterpriseWebPositionDetailInfoCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_ThirdIndustry";//第三产业
            //DataTableName = "QCCEnterpriseKey_YangQiDetial";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
        }
       
        public void initialUrl()
        {
            var mongoDb = MongoOpCollection.Get121MongoOp("SimpleCrawler");
            Console.WriteLine("开始载入数据");
             var query = Query.And(Query.EQ("cityName", "合肥"), Query.Exists("isPositionDetailUpdate_web", false));

            // query = Query.And(query, Query.NE("status", "吊销"), Query.NE("status", "注销"));
            var fields = new string[] { "guid" };
           
            allCount = (int)mongoDb.FindCount(DataTableName, query);
            var skipCount = 0;
            var limitCount = 1000;
            if (allCount >= 10000)
            {
                skipCount = new Random().Next(0, 1000);
            }
            else
            {
                skipCount = 0;
            }
           // var allHitObjList = dataop.FindLimitFieldsByQuery(DataTableName, query, new MongoDB.Driver.SortByDocument() { { "_id", 1 } }, skipCount, limitCount, fields);
            var allHitObjList = mongoDb.FindAll(DataTableName, query).SetSkip(skipCount).SetLimit(limitCount).SetFields(fields);

            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList.Take(1))
            {

                var guid = hitObj.Text("guid");
                guid = "f078cc6f887c6d464e9b6a732878f61c";
                var curUrl = $"https://www.qcc.com/firm_{guid}";
                
                if (!filter.Contains(curUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = guid });
                    filter.Add(curUrl);// 防止执行2次
                }
                
            }
            GetProxy();
        }
        public WebProxy GetProxy()
        {
           var webProxy= QuickProxyPoolHelper.Instance().Get(20);
            if (webProxy == null)
            {
                var result = QuickDicMethod.Instance("GetProxy").Put("webProxy", 1);
                 
            }
            else {
                QuickDicMethod.Instance("GetProxy").Put("webProxy", -1);
            }
            return webProxy;
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
            Settings.ThreadCount = 2;
            Settings.MaxReTryTimes = 5;
           // Settings.ContentType = "application/json";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.SimulateCookies = "Cookie: zg_did=%7B%22did%22%3A%20%22170d2764f4b9d4-0ff3938bb7a54c-7a1b34-13c680-170d2764f4c798%22%7D; UM_distinctid=170d27652ca729-093c0dae88028b-7a1b34-13c680-170d27652cb9c0; _uab_collina=158449496197502160055555; acw_tc=751801b015866086896724907e813cd7185bf1b7b6fea063f2d7e93a95; Hm_lvt_78f134d5a9ac3f92524914d0247e70cb=1586762953; CNZZDATA1254842228=1406114251-1584076325-https%253A%252F%252Fwww.qichacha.com%252F%7C1586927237; hasShow=1; QCCSESSID=kh4n3gjsv9lkm9c4lld0666e47; zg_de1d1a35bfa24ce29bbf2c7eb17e6c4f=%7B%22sid%22%3A%201586929502376%2C%22updated%22%3A%201586931211611%2C%22info%22%3A%201586929502380%2C%22superProperty%22%3A%20%22%7B%7D%22%2C%22platform%22%3A%20%22%7B%7D%22%2C%22utm%22%3A%20%22%7B%7D%22%2C%22referrerDomain%22%3A%20%22%22%2C%22cuid%22%3A%20%22208262b6077bf5bef9c2460844ef155f%22%2C%22zs%22%3A%200%2C%22sc%22%3A%200%7D; Hm_lpvt_78f134d5a9ac3f92524914d0247e70cb=1586931213";
            Settings.HeadSetDic.Add("Header-Key", "header-vaule");
            //Settings.CurWebProxy= GetProxy();
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();
 
        }

        private string GetHtmlNodeValue(HtmlNode baseInfNode, string beginTdName, string endTdName)
        {
            var infoNode = baseInfNode.SelectNodes("//td[@class='tb']").Where(c => c.InnerText.Contains(beginTdName)).FirstOrDefault();
            if (infoNode != null)
            {
                var valueNode = infoNode.ParentNode;
                var text = valueNode.InnerText.ToolsSubStr(beginTdName, endTdName).Trim();
                return text;
            }
            return string.Empty;
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
            var baseInfNode = htmlObj.GetElementbyId("Cominfo");
            var updateBson = new BsonDocument();
            if (baseInfNode != null)
            {
                var regCapi = GetHtmlNodeValue(baseInfNode, "实缴资本", "核准日期");
                var checkDate = GetHtmlNodeValue(baseInfNode, "核准日期", "");
                var ssNum = GetHtmlNodeValue(baseInfNode, "参保人数", "所属地区");
                var scale = GetHtmlNodeValue(baseInfNode, "人员规模", "参保人数");
                updateBson.Set("paid_in_capi", regCapi.ToMoney());
                updateBson.Set("insuredPersonsNum", ssNum);
                updateBson.Set("checkDate", checkDate);
                updateBson.Set("scaleNum", scale);
                updateBson.Set("isPositionDetailUpdate_web", 1);
                updateBson.Set("guid", guid);
                PushData(updateBson);
                Console.WriteLine($"{allCount}获取到数据{regCapi}_{checkDate}_{ssNum}_{scale}");
            }
            else
            {
                //if (args.Url.Contains("firm_")) { 
                //    var curl = args.Url.Replace("firm_", "cbase_");
                //    AddUrl(curl, guid);
                //    Console.WriteLine($"替换url{curl}");
                //    return;
                //}
                // var webProxy = GetWebProxy(result.Value.Text("address"));
                //异常访问 更换代理
                Console.WriteLine("异常数据");
                var invalidUpdate = new BsonDocument();
                invalidUpdate.Set("isPositionDetailUpdate_web", -1);
                invalidUpdate.Set("guid", guid);
                PushData(invalidUpdate);

            }
            ShowStatus();
            Settings.CurWebProxy = GetProxy();
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
                var htmlObj = html.HtmlLoad();
                var baseInfNode = htmlObj.GetElementbyId("Cominfo");
                var updateBson = new BsonDocument();
                if (baseInfNode != null)
                {
                    return false;
                }
                else
                {
                    if (args.Html.Contains("基本信息"))
                    {
                        return false;
                    }
                    var needChangeProxy = false;
                    if (args.Html.Contains("从传输流") || args.Html.Contains("请求被中止") || args.Html.Contains("操作超时") || args.Html.Contains("基础连接已经关闭"))
                    {
                        if ((args.Html.Contains("超时")||args.Html.Contains("基础连接已经关闭")) && Settings.CurWebProxy != null)
                        {
                            var proxyKey = Settings.CurWebProxy.Address.ToString();
                            var errorCount = QuickDicMethod.Instance("WebProxySet").Put(proxyKey, 1);
                            if (errorCount <= Settings.MaxReTryTimes)
                            {
                                needChangeProxy = false;

                            }
                            else
                            {
                                needChangeProxy = true;
                            }
                        }

                    }
                    else
                    {
                        if (args.Html.Contains("https://www.qcc.com/index_verify")||args.Html.Contains("'026DAA711301A26E6F784CA653FE9739B0EAAED8"))
                        {
                            needChangeProxy = true;
                        }
                        
                    }
                    if (Settings.CurWebProxy != null&& needChangeProxy)
                    {
                        Console.WriteLine(Settings.CurWebProxy.Address + "无效");
                        QuickProxyPoolHelper.Instance().Delete(Settings.CurWebProxy, updateCount);
                        Thread.Sleep(1000);
                        Settings.CurWebProxy = GetProxy();
                      
                    }
                    Thread.Sleep(5000);
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
