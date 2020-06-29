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
    public class PositionListCrawler_LiePin : SimpleCrawlerBase
    {

        MongoOperation oldOp = MongoOpCollection.Get121MongoOp("SimpleCrawler");
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        bool isUpdate = true;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.isUpdate”已被赋值，但从未使用过它的值
        const int takeCount = 10;
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public PositionListCrawler_LiePin(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop) : base(_Settings, _filter, _dataop)
        {
            DataTableName = "QCCEnterpriseKey_Position_LiePin";//房间
            DataTableCategoryName = "QCCEnterpriseKey_ThirdIndustry";//房间
            updatedValue = "1";//是否更新字段
            uniqueKeyField = "jobId";
        }

        public void initialUrl(string area,int pageindex=0,string keyword="")
        {
            var query = Query.And(Query.EQ("cityName", "合肥"));
            var fields = new string[] { "name", "reg_capi_desc", "status", "guid" };
            ///获取等待爬取的公司
            var hitEntperpriseList = dataop.FindFieldsByQuery(DataTableCategoryName, query,  fields);
            // var postName = "pie工程师,自动化工程师,策划专员,控制器硬件工程师,Java工程师,室内设计师总监,结构工程师,工程经理,物业运营总监,售后服务经理,二手车高级销售顾问,车辆性能实验员,总装工艺工程师,财务经理,外联经理,行政专员,外贸专员,PE工程师,设备管理员,销售技术支持,设备维修人员,酒店总经理,临床医师,英语培训老师,学校学科老师,销售经理,验光师,专职司机,热成型模具设计经理,软件开发工程师,大数据产品经理,网约车司机,小学英语老师,大客户经理,地产总经理,整车开发项目主管,外贸经理,国际贸易市场经理,拖拉机工程师,数控机床总经理,应用工程师,电商运营,食品生产负责人,产品经理,测试工程师,市场部经理,汽车销售,项目品牌经理,高级前端讲师,运营总监,渠道部经理,总经理助理,生产领班,区域销售经理,注塑工程师,教务主管,课程顾问,旅游编辑,视觉设计,建筑设计师,人事助理,PHP讲师,前台文员,行政人事专员,生熟手车工,网店推广运营总监,行政专员,接待,行政前台,渠道销售主管,房地产销售主管,平面设计,珠宝店运营经理,重工吊车操作员,3D设计,地产代理公司销售,片区网络经理,建筑劳务公司货车司机,银行客户经理,活动策划,快递员,外科医生,三甲医院护士,投资公司总监,行政主管,招商主管,广告主管,房地产开发/策划主管,销售经理,内衣厂机修,业务跟单经理,行政人事助理,研发工程师,程序员,PACS软件开发工程师（互联网医疗事业部业）,电仪工程师,塑料色母类研发工程师,PHP后台开发工程师,PLC开发工程师,成控主管,PVC环保稳定剂工程师,废水处理工程师,园林景观设计专业人士,证券事务代表,UI设计师,应用技术工程师";
            //if (string.IsNullOrEmpty(keyword)) {
            //    keyword = "工业,嵌入式,工业设计";//制造
            //}
          //  var postArray = keyword.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
           // postArray.Add("制造");
           // var url = "https://app-tongdao.liepin.com/a/t/job/v2/search-cbh.json";
           
            foreach (var ent in hitEntperpriseList)
            {
                if (ent.Text("status").Contains("注销")) continue;
                var reg_capi = ent.Text("reg_capi_desc").ToMoney();
                if (reg_capi < 1000)
                {
                    continue;
                }
                var name = ent.Text("name").Replace("股份有限公司", "").Replace("有限公司", "");
                // name = "厦门蒙友互联软件有限公司";
                var guid = ent.Text("guid");
                initialKeyWordUrl("", 0, name, guid);


           }

        }
        public void initialKeyWordUrl(string area, int pageindex = 0, string keyword = "",string guid="")
        {
            var url = "https://app-tongdao.liepin.com/a/n/job/search.json";
            var postData = new BsonDocument();
            postData.Set("client_id", 80001);
            postData.Set("version", "3.0.2");
            postData.Set("version_code", 30002);
            postData.Set("dev_type", 1);
            var otherData = new BsonDocument();
            otherData.Add("keyword", keyword);
            // otherData.Add("dq", area);//铜梁040010220 万州 040010180 ，永川040010100，江津040010130
            otherData.Add("industry", "000");//010 000默认
            otherData.Add("salaryLow", 0);
            otherData.Add("salaryHigh", 999);
            otherData.Add("refreshTime", "000");
            otherData.Add("jobKind", null);
            otherData.Add("sortType", 0);
            otherData.Add("compKind", new BsonArray() { "000" });
            otherData.Add("compScale", "000");
            otherData.Add("currentPage", pageindex);
            otherData.Add("pageSize", takeCount);
            otherData.Add("isCampus", false);
            postData.Set("data", otherData);
            UrlQueue.Instance.EnQueue(new UrlInfo(url) { UniqueKey = guid, PostData = postData.ToJson() });
            filter.Add(url);// 防止执行2次
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
            Settings.MaxReTryTimes = 2;
            // Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMinMSecond = 3000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            Settings.ContentType = "application/json; charset=UTF-8";
            Settings.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 12_3_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 MicroMessenger/7.0.5(0x17000523) NetType/WIFI Language/zh_CN";
            Settings.Accept = "*/*";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "br, gzip, deflate");
            Settings.HeadSetDic.Add("X-Client-Type", "wxa");
            Settings.PostEncoding = Encoding.UTF8;
            Settings.SimulateCookies = "_mscid=cx_wx_01";
            Settings.Referer = "https://servicewechat.com/wx4d70579bfaefd959/105/page-frame.html";
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("初始化url");
            //var areas = new string[] { "040010180"};//, "040010180", "040010100", "040010130" "040010220"
            //foreach (var area in areas)
            //{
            //    initialUrl(area,0 );
            //}
            initialUrl("", 0);
            base.SettingInit();



        }
#pragma warning disable CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        int noCountTimes = 3;
#pragma warning restore CS0414 // 字段“PositionListCrawler_LiePin.noCountTimes”已被赋值，但从未使用过它的值
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        override
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            JObject jsonObj = GetJsonObject(hmtl);
            var entGuid = args.urlInfo.UniqueKey;
            var result = jsonObj["data"];
            var totalcnt = result["totalcnt"];
         
            if (result != null)
            {
                var items = result["soJobForms"];
                foreach (var item in items)
                {
                    var bsonDoc = GetBsonDocument(item);
                    bsonDoc.Set("guid", bsonDoc.Text(uniqueKeyField));
                    bsonDoc.Set("eGuid", entGuid);
                    PushData(bsonDoc);
                }
                var catUpdateDoc = new BsonDocument().Set("isJobUpdate", 1);
                UpdateData(catUpdateDoc, DataTableCategoryName, Query.EQ("guid", entGuid) );



                var postObj = GetBsonDocument(args.urlInfo.PostData);
                var pageObj = postObj["data"] as BsonDocument;
                var pageStr = pageObj.Text("currentPage");
                if (items.Count() < takeCount )
                {
                    ShowMessage($"已到最后一行{items.Count()}{pageObj.Text("keyword")}");
                    //取出第二个分类，并更新
                    //initialUrl(catInfo, 1)
                    return;
                }
               
                if (pageStr != "")
                {

                    if (int.TryParse(pageStr, out int pageIndex))
                    {
                        var curUrl = args.Url;
                        var dq = pageObj.Text("dq");
                        initialKeyWordUrl(dq, pageIndex + 1, pageObj.Text("keyword"), entGuid);
                        ShowMessage($"获取数据并初始化成功{pageObj.Text("keyword")}{pageIndex + 1}页");
                    }
                    else
                    {
                        ShowMessage($"转换{pageIndex}出错1");
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

                if (result.ToString().ToLower() == "ok")//需要编写被限定IP的处理
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
