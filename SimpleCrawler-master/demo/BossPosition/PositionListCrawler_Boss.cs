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
    public class PositionListCrawler_Boss : SimpleCrawlerBase
    {


#pragma warning disable CS0414 // 字段“PositionListCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_Boss.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 8;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public PositionListCrawler_Boss(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "Position_Boss";//房间
            DataTableCategoryName = "SearchCondition_Boss";//condition
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "encryptJobId";
        }

        public void initialUrl()
        {

            var conditionList = FindDataForUpdate(dataTableName: DataTableCategoryName, fields: new string[] { "code","name", "searchType" });
            var cityList = conditionList.Where(c => c.Int("searchType") == 0 && c.Int("code") != 0).ToList();
            var degreeList = conditionList.Where(c => c.Int("searchType") == 1 && c.Int("code") != 0).ToList();
            var experienceList = conditionList.Where(c => c.Int("searchType") == 2 && c.Int("code") != 0).ToList();
            var industryList = conditionList.Where(c => c.Int("searchType") == 3 && c.Int("code") != 0).ToList();
            var salaryList = conditionList.Where(c => c.Int("searchType") == 4 && c.Int("code") != 0).ToList();
            var scaleList = conditionList.Where(c => c.Int("searchType") == 5 && c.Int("code") != 0).ToList();
            var stageList = conditionList.Where(c => c.Int("searchType") == 6 && c.Int("code") != 0).ToList();

            var postName = "pie工程师,自动化工程师,策划专员,控制器硬件工程师,Java工程师,室内设计师总监,结构工程师,工程经理,物业运营总监,售后服务经理,二手车高级销售顾问,车辆性能实验员,总装工艺工程师,财务经理,外联经理,行政专员,外贸专员,PE工程师,设备管理员,销售技术支持,设备维修人员,酒店总经理,临床医师,英语培训老师,学校学科老师,销售经理,验光师,专职司机,热成型模具设计经理,软件开发工程师,大数据产品经理,网约车司机,小学英语老师,大客户经理,地产总经理,整车开发项目主管,外贸经理,国际贸易市场经理,拖拉机工程师,数控机床总经理,应用工程师,电商运营,食品生产负责人,产品经理,测试工程师,市场部经理,汽车销售,项目品牌经理,高级前端讲师,运营总监,渠道部经理,总经理助理,生产领班,区域销售经理,注塑工程师,教务主管,课程顾问,旅游编辑,视觉设计,建筑设计师,人事助理,PHP讲师,前台文员,行政人事专员,生熟手车工,网店推广运营总监,行政专员,接待,行政前台,渠道销售主管,房地产销售主管,平面设计,珠宝店运营经理,重工吊车操作员,3D设计,地产代理公司销售,片区网络经理,建筑劳务公司货车司机,银行客户经理,活动策划,快递员,外科医生,三甲医院护士,投资公司总监,行政主管,招商主管,广告主管,房地产开发/策划主管,销售经理,内衣厂机修,业务跟单经理,行政人事助理,研发工程师,程序员,PACS软件开发工程师（互联网医疗事业部业）,电仪工程师,塑料色母类研发工程师,PHP后台开发工程师,PLC开发工程师,成控主管,PVC环保稳定剂工程师,废水处理工程师,园林景观设计专业人士,证券事务代表,UI设计师,应用技术工程师";
            var postArray = postName.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var city in cityList.Where(c=>c.Text("name")=="重庆"))
            {
                var url = "https://www.zhipin.com/wapi/zpgeek/miniapp/search/joblist.json?query=&city=&stage=&scale=&industry=&degree=&salary=&experience=&position=&page=1&appId=10002";
                url = ReplaceUrlParam(url, "city", city.Text("code"));
                foreach(var post in postArray.Reverse()) {
                    url = ReplaceUrlParam(url, "query", HttpUtility.UrlEncode(post),"?");
                    // foreach (var degree in degreeList)
                    {
                        
                        //  url = ReplaceUrlParam(url, "degree", degree.Text("code"));
                        //  foreach (var experience in experienceList)
                        {
                            //   url = ReplaceUrlParam(url, "experience", experience.Text("code"));
                            //  foreach (var industry in industryList)
                            {
                                //    url = ReplaceUrlParam(url, "industry", industry.Text("code"));
                                //    foreach (var salary in salaryList)
                                {

                                    //   url = ReplaceUrlParam(url, "salary", salary.Text("code"));
                                    // foreach (var scale in scaleList)
                                    {

                                        //url = ReplaceUrlParam(url, "scale", scale.Text("code"));
                                        // foreach (var stage in stageList)
                                        {

                                            //url = ReplaceUrlParam(url, "stage", stage.Text("code"));
                                            if (!filter.Contains(url))
                                            {
                                                UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = "1", });
                                                filter.Add(url);// 防止执行2次
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                }
                            }
                        }
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
            Settings.ThreadCount = 10;
            Settings.MaxReTryTimes = 2;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
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
            Settings.Referer = "https://servicewechat.com/wxa8da525af05281f3/97/page-frame.html";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            initialUrl();
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionListCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“PositionListCrawler_Boss.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var result = jsonObj["zpData"];
            var hasMore = GetJsonValueString(result, "hasMore").ToLower() == "true";
            var list = result["list"];
            var cityCode = GetUrlParam(args.Url, "city");
            var degreeCode = GetUrlParam(args.Url, "degree");
            var experienceCode = GetUrlParam(args.Url, "experience");
            var industryCode = GetUrlParam(args.Url, "industry");
            var salaryCode = GetUrlParam(args.Url, "salary");
            var scaleCode = GetUrlParam(args.Url, "scale");
            var stageCode = GetUrlParam(args.Url, "stage");

            if (result != null)
            {
                foreach (var item in list)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    //bsonDoc.Set("cityCode", cityCode);
                    //bsonDoc.Set("degreeCode", degreeCode);
                    //bsonDoc.Set("experienceCode", experienceCode);
                    //bsonDoc.Set("industryCode", industryCode);
                    //bsonDoc.Set("salaryCode", salaryCode);
                    //bsonDoc.Set("scaleCode", scaleCode);
                    //bsonDoc.Set("stageCode", stageCode);
                    if (item["jobLabels"] != null)
                    {
                        var jobLabels = bsonDoc["jobLabels"] as BsonArray;
                        if (jobLabels.Count() > 0)
                        {
                            var areaText = jobLabels[0].ToString();
                            var areaArray = areaText.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (areaArray.Length >= 2)
                            {
                                var cityName = areaArray[0];
                                var areaName = areaArray[1];
                                bsonDoc.Set("areaName", areaName);
                            }
                            if (areaArray.Length >= 3)
                            {
                                var regionName = areaArray[2];
                                bsonDoc.Set("regionName", regionName);
                            }
                            if (bsonDoc.Text("areaName") == "" || bsonDoc.Text("regionName") == "")
                            {
                                Console.WriteLine("标签解析出错"+areaText);
                            }
                        }

                    }

                    PushData(bsonDoc);
                }

                if (hasMore)
                {
                    var nextUrl = args.Url;
                    var page = GetUrlParam(nextUrl, "page");
                    if (int.TryParse(page, out int pageIndex))
                    {
                        pageIndex += 1;
                        nextUrl = nextUrl.Replace($"&page={page}", $"&page={pageIndex}");
                        if (!filter.Contains(nextUrl))
                        {
                            UrlQueue.Instance.EnQueue(new UrlInfo(nextUrl) { UniqueKey = pageIndex.ToString(), });
                            filter.Add(nextUrl);// 防止执行2次
                        }
                    }
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
                var result = jsonObj["message"];

                if (result.ToString().ToLower() == "success")//需要编写被限定IP的处理
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
