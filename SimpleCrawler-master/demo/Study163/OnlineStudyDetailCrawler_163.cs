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
    /// 门派url
    /// https://study.163.com/course/introduction/1003743015.htm
    ///
    /// </summary>
    public class OnlineStudyDetailCrawler_163 : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“OnlineStudyDetailCrawler_163.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“OnlineStudyDetailCrawler_163.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public OnlineStudyDetailCrawler_163(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "OnlineStudyRoom";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "guid";
        }
        List<BsonDocument> allHitObjList;
        public void initialUrl()
        {
            allHitObjList = FindDataForUpdate(dataTableName: DataTableName, fields:new string[] { "guid", "targetUrl" });
            //初始化布隆过滤器
            foreach (var hitObj in allHitObjList)
            {
                var targetUrl = "https:" + hitObj.Text("targetUrl");
                // var curUrl = "https://study.163.com/course/introduction/{0}.htm";
                if (!filter.Contains(targetUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(targetUrl) { UniqueKey = hitObj.Text("guid") });
                    filter.Add(targetUrl);// 防止执行2次
                }
               // InitialForUpdateUrl(curUrl, hitObj.Text("guid"));
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
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/73.0.3683.75 Safari/537.36";
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.SimulateCookies = "OPENID=; _ntes_nnid=07af05e3896c765a57f0661bd5d82541,1574343946255; _ntes_nuid=07af05e3896c765a57f0661bd5d82541; NTESSTUDYSI=cb8372e2a11b4a3e8b017a77cb9389b5; EDUWEBDEVICE=db78416fd81247e3b595ddd4e9e0ab57; eds_utm=eyJjIjoiIiwiY3QiOiIiLCJpIjoiIiwibSI6IiIsInMiOiIiLCJ0IjoiIn0=|aHR0cHM6Ly93d3cuYmFpZHUuY29tL2xpbms/dXJsPUFYdWRHaWNkVTB2NlZxYWhXRE5vZXR1YUZ6ZG9OaXNSYXpPWjBvM2gtN0cmd2Q9JmVxaWQ9YjhmMzIxOWMwMDBiZDQzMzAwMDAwMDA2NWRmMzZlMWI=; __utmc=129633230; EDU-YKT-MODULE_GLOBAL_PRIVACY_DIALOG=true; __utmz=129633230.1576492724.2.2.utmcsr=baidu|utmccn=(organic)|utmcmd=organic; NNSSPID=697a1f72082647aa8639a3d6f865fbba; UM_distinctid=16f116c9505ac-0e2930ad3c765c-7a1b34-13c680-16f116c950629c; _antanalysis_s_id=1576545327630; ne_analysis_trace_id=1576562473736; vinfo_n_f_l_n3=554562770faa4c02.1.0.1576545322211.0.1576562483504; hb_MA-BFF5-63705950A31C_source=mooc.study.163.com; STUDY_UUID=fc8f6c4a-6bca-49eb-898e-2f38f45ed673; utm=eyJjIjoiIiwiY3QiOiIiLCJpIjoiIiwibSI6IiIsInMiOiIiLCJ0IjoiIn0=|aHR0cHM6Ly9zdHVkeS4xNjMuY29tL2NvdXJzZS9pbnRyb2R1Y3Rpb24vMTAwNjUwNjAxNy5odG0=; __utma=129633230.916564851.1576234529.1576568423.1576584283.6; __utmb=129633230.3.8.1576584284184";
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate, br");
            Settings.HeadSetDic.Add("Accept-Language", "en,zh-CN;q=0.9,zh;q=0.8,en-AU;q=0.7");
            Settings.HeadSetDic.Add("Upgrade-Insecure-Requests", "1");
            Settings.Referer = "study.163.com";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“OnlineStudyDetailCrawler_163.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“OnlineStudyDetailCrawler_163.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {

            var htmlObj = args.Html.HtmlLoad();
            var root = htmlObj.DocumentNode;
            var teacherInfo = GetTeacherDesc(args.Html);
            var goodsId = args.urlInfo.UniqueKey;
            var bsonDoc = new BsonDocument();
            if (teacherInfo != null)
            {
               bsonDoc.Set("teacherInfo", teacherInfo);
            }
            var detailInfo = GetDetailInfo(args.Html); ;
            if (detailInfo == null)
            {
                bsonDoc.Set("detailInfo", detailInfo);
            }
            bsonDoc.Set(updatedField, updatedValue);
            bsonDoc.Set("guid", args.urlInfo.UniqueKey);
            UpdateData(bsonDoc);
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
                 
              
                if (GetTeacherDesc(args.Html).Text("teacherInfo") != "")//需要编写被限定IP的处理
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

        /// <summary>
        /// 获取介绍信息
        /// </summary>
        /// <param name="htmlObj"></param>
        /// <returns></returns>
        public BsonDocument GetDetailInfo(string html)
        {
            var htmlObj = html.HtmlLoad();
            var updateDoc = new BsonDocument();
            var root = htmlObj.DocumentNode;
            var introNode = htmlObj.GetElementbyId("j-course-briefintro");
            if (introNode == null)
            {
                var courseInfoJson = html.ToolsSubStr("window.displaySettingResult = ", "};").Trim();
                if (string.IsNullOrEmpty(courseInfoJson))
                {
                    var descNode = htmlObj.DocumentNode.SelectSingleNode("//meta[@name='description']");
                    if (descNode != null)
                    {
                        var desc = GetNodeAttribute(descNode, "content");
                        var descInfo = new BsonDocument().Add("description", desc);
                        updateDoc.Set("detailInfo", descInfo);
                    }

                    return updateDoc;
                }
                var courseInfo = (courseInfoJson + "}").GetBsonDocFromJson();
                updateDoc.Set("detailInfo", courseInfo);
            }
            else
            {
                var courseInfo = new BsonDocument().Add("description", introNode.InnerText);
                updateDoc.Set("detailInfo", courseInfo);
            }
            return updateDoc;
        }

        /// <summary>
        /// 获取讲师
        /// </summary>
        /// <param name="htmlObj"></param>
        /// <returns></returns>
        public  BsonDocument GetTeacherDesc(string html)
        {
            var htmlObj = html.HtmlLoad();
            var root = htmlObj.DocumentNode;
            
            var updateDoc = new BsonDocument();
            var userNode = htmlObj.DocumentNode.SelectSingleNode("a[@class='j-userNode']");
            if (userNode == null)
            {
                var teacherListInfoJson = html.ToolsSubStr("window.detailInfoResult =", "};").Trim();
               
                if (string.IsNullOrEmpty(teacherListInfoJson))
                {
                    var teacher= html.ToolsSubStr("teacher: \"", "\",").Trim();
                    if (string.IsNullOrEmpty(teacher))
                    {
                        var keywordNode= htmlObj.DocumentNode.SelectSingleNode("//meta[@name='keywords']");
                        if (keywordNode != null)
                        {
                            teacher = GetNodeAttribute(keywordNode, "content");
                        }
                         
                    }
                    var teacherInfo = new BsonDocument();
                    teacherInfo.Set("name", teacher);
                    updateDoc.Set("teacherInfo", teacherInfo);
                    return updateDoc;
                }
                  var teacherList = (teacherListInfoJson+"}").GetBsonDocFromJson();
                  updateDoc.Set("teacherInfo", teacherList);
            }
            else
            {
                var teacherInfo = new BsonDocument();
                teacherInfo.Set("name", userNode.InnerText);
                var href = GetNodeAttribute(userNode, "href");
                teacherInfo.Set("href", href);
                if (!string.IsNullOrEmpty(href))
                {
                    var guid = GetGuidFromUrl(href, "/");
                    teacherInfo.Set("guid", guid);
                }
                updateDoc.Set("teacherInfo", teacherInfo);
            }
            return updateDoc;
        }
            

    }

}
