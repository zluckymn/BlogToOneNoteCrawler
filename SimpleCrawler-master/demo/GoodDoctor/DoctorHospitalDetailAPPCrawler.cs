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
using System.Security.Cryptography;
using System.Globalization;

namespace SimpleCrawler.Demo
{
    ///
    /// 
    ///  </summary>
    public class DoctorHospitalDetailAPPCrawler : ISimpleCrawler
    {

       // private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/SimpleCrawler";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> guidFilter;
        private   string _DataTableName = "GoodDoctorHospital";//存储的数据库表明
       

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }
        public string DataTableNameSpecialURL
        {
            get { return _DataTableName + "SpecialURL"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_MT"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCityCategory
        {
            get { return "CityCategoryInfo_MT"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameDoctor
        {
            get { return _DataTableName + "Doctor"; }

        }


        ///// <summary>
        /////  分类信息
        ///// </summary>
        //public string DataTableNameCategory
        //{
        //    get { return "CategoryInfo_MT"; }

        //}

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public DoctorHospitalDetailAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        public string GetWebProxyString()
        {
           return string.Format("{0}:{1}@{2}:{3}", "H1538UM3D6R2133P", "511AF06ABED1E7AE", "proxy.abuyun.com", "9010");
        }
        
        int pageSize = 20;//24
        int pageSkipNum = 0;
         
        string materialUrl = "http://mobile-api.haodf.com/patientapi/hospital_getHospitalBaseInfoNew";
        string curUrl = string.Empty;
        HuiCongAppHelper appHelper = new HuiCongAppHelper();
        List<BsonDocument> reasonList = new List<BsonDocument>();
        string reqtoken = "864324321401123";
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

            //种子地址需要加布隆过滤
            
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
            Settings.MaxReTryTimes = 10;
            Settings.IgnoreFailUrl = true;
            //Settings.AutoSpeedLimit = true;
            //Settings.AutoSpeedLimitMaxMSecond = 1000;
            Settings.CurWebProxy = GetWebProxy();
            Settings.AccessToken = reqtoken;
          
            Settings.ContentType = "application/x-www-form-urlencoded";
            this.Settings.UserAgent = "haodf_app/1.0";
            Settings.PostEncoding = Encoding.UTF8;
           
            var allHospitalList = dataop.FindAll(DataTableName).Select(c=>c.Text("guid")).ToList();
            
            foreach (var guid in allHospitalList)//法庭
            {
                     
                    var postData = string.Format("app=p&os=android&n=2&hospitalId={0}&m=GT-I9300&v=5.2.5&di={1}&s=hd&deviceToken={1}4&p=1&userId=0&currentUserId=0&sv=4.4.2&api=1.2", guid, reqtoken);
                    UrlQueue.Instance.EnQueue(new UrlInfo(materialUrl) { Depth = 1, PostData = postData });
            }


            //var testUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=外墙面砖&v=59&e=100&c=供应信息&n=3101&m=2&H=1&bt=0";
            //var testAuthorization = appHelper.GetHuiCongAuthorizationCode(testUrl);
            //UrlQueue.Instance.EnQueue(new UrlInfo(testUrl) { Depth = 1, Authorization = testAuthorization });
            Console.WriteLine("正在加载账号数据");
            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的
            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            //是否guid
            //不进行地址爬取
            Settings.RegularFilterExpressions.Add(@"luckymnXXXXXXXXXXXXXXXXXX");

            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("模拟登陆失败");
            }

        }

        /// <summary>
        /// unicode转中文
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string UnicodeFix(string s)
        {

            //string s = "\\u91cd\\u5e86\\u5730\\u4ea7\\uff0c";
            string r = Regex.Replace(s, @"\\u([a-f0-9]{4})", m => ((char)ushort.Parse(m.Groups[1].Value, NumberStyles.HexNumber)).ToString());
            return r;
        }

        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
      
            var html = UnicodeFix(args.Html);
            //修正为Jobject可用的对象
            html = "{\"data\":" + html + "}";
            JObject jsonObj = JObject.Parse(html);
            //获取查询条件
            var data = jsonObj["data"]["content"];

            if (data != null)
            {
                var insert = 0;
                var update = 0;
                Console.WriteLine("获得数据:{0}", data.ToList().Count);
                BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(data.ToString());
               
               
                var docList = data["wxServiceStart"];
                foreach (var doctor in docList)
                {
                    BsonDocument doctorDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(doctor.ToString());
                    var doctorId = doctorDoc.Text("doctorId");
                    var guid = doctorId;
                    if (!guidFilter.Contains(guid) && !hasExistObj(guid))
                    {
                        doctorDoc.Add("hospitalId", document.Text("hospitalId"));
                        doctorDoc.Add("guid", guid);
                        guidFilter.Add(guid);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = doctorDoc, Name = DataTableNameDoctor, Type = StorageType.Insert });
                        insert++;
                    }
                }
                document.Set("wxServiceStart", "");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = document, Name = DataTableName, Query = Query.EQ("guid", document.Text("hospitalId")), Type = StorageType.Update });
                update++;
                Console.WriteLine("新增专家：{0}", insert);
            }
            
              
           
        }
        

        private BsonDocument GetObj(string guid)
        {
            return dataop.FindOneByQuery(DataTableName, Query.EQ("guid", guid));
        }
        private bool hasExistObj(string guid)
        {
            return dataop.FindCount(DataTableNameDoctor, Query.EQ("guid", guid)) > 0;
        }
        private string TrimStr(string str)
        {
            return str.Replace(" ", "").Replace("\"", "").Trim();
        }
       
        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetUrlParam(string url, string name)
        {
            var queryStr = GetQueryString(url);
            if (string.IsNullOrEmpty(queryStr))
            {
                queryStr = url;
            }
            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }


        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetQueryString(string url)
        {
            var queryStrIndex = url.IndexOf("?");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }


       
        public string ValeFix(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Trim();
        }

        public string GetInnerText(HtmlNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.InnerText)) { throw new NullReferenceException(); }
            return node.InnerText;
        }

        public string[] GetStrSplited(string str)
        {
            var strArr = str.Split(new string[] { ":", "：" }, StringSplitOptions.RemoveEmptyEntries);
            return strArr;
        }
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            var html = args.Html;
            if (string.IsNullOrEmpty(html))
            {
                return true;
            }
            if (html.Contains("Object moved")|| html.Contains("Service Unavailable") )//需要编写被限定IP的处理
            {
                return true;
            }

            if (!html.Contains("It is not legal"))
            {
                 return false;
              
            }
            
            return true;
        }
        /// <summary>
        /// url处理,是否可添加
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CanAddUrl(AddUrlEventArgs args)
        {

            return true;
        }

        /// <summary>
        /// void错误处理
        /// </summary>
        /// <param name="args"></param>
        public void ErrorReceive(CrawlErrorEventArgs args)
        {


        }

        /// <summary>
        /// 模拟登陆，ip代理可能需要用到
        /// </summary>
        /// <returns></returns>
        public bool SimulateLogin()
        {
            return true;
          
             
        }

        /// <summary>
        /// ip无效处理
        /// </summary>
        private void IPInvalidProcess(IPProxy ipproxy)
        {
           
        }

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private void StartDBChangeProcess()
        {

            List<StorageData> updateList = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0)
            {
                var curStorage = DBChangeQueue.Instance.DeQueue();
                if (curStorage != null)
                {
                    updateList.Add(curStorage);
                }
            }
            if (updateList.Count() > 0)
            {
                var result = dataop.BatchSaveStorageData(updateList);
                if (result.Status != Status.Successful)//出错进行重新添加处理
                {
                    foreach (var storageData in updateList)
                    {
                        DBChangeQueue.Instance.EnQueue(storageData);
                    }
                }
            }



        }
    }

}
