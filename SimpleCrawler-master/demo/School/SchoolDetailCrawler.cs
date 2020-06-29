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
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 用于城市与区域代码初始化
    /// </summary>
    public class SchoolDetailCrawler : ISimpleCrawler
    {

         
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> schoolIdFilter=new BloomFilter<string>(8000000);
        private const string _DataTableName = "CitySchoolNew_School";//存储的数据库表名

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
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCityRegion
        {
            get { return "CityRegionInfo_School"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_School"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCityArea
        {
            get { return "CityAreaInfo_School"; }

        }
        List<BsonDocument> allSchoolList = new List<BsonDocument>();
       
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public SchoolDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }
        Dictionary<string,string> jiaoyuCategoryDic = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void InitialUrlQueue()
        {
            var skipCount = 1000;
            var takeCount = 1000;
            var query = Query.And(Query.NE("isUpdate", "1"));
            //过滤没有detailInfo的值
            var allCount = dataop.FindCount(DataTableName, query);
            Console.WriteLine("待处理个数:{0}", allCount);
            var random = new Random();

            if (allCount >= 10000)
            {
                skipCount = random.Next(1000, allCount);
            }
            else
            {
                skipCount = 0;
            }
            allSchoolList = dataop.FindLimitFieldsByQuery(DataTableName, Query.NE("isUpdate", "1"),new MongoDB.Driver.SortByDocument() {  }, skipCount, takeCount, new string[] { "href" }).ToList();
            foreach (var shchool in allSchoolList)
            {
                var url = string.Format("http://www.todgo.com/{0}", shchool.Text("href"));
                if(!filter.Contains(url))
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { });
            }
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =5;
            //Settings.CurWebProxy = GetWebProxy();
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            
            Console.WriteLine("正在初始化选择url队列");

            InitialUrlQueue();


            //Settings.RegularFilterExpressions.Add(@".*?market/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}).html");
            //Settings.RegularFilterExpressions.Add(@".*?data/land.*?.html");
            //广州_440105________1_1.html
            //Settings.RegularFilterExpressions.Add(@".*?data/land/.*?_.*?________.*?_1.html");
            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/CitySelect.aspx"));
            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("zluckymn模拟登陆失败");
            }

        }
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", ConstParam.proxyHost, ConstParam.proxyPort));
            proxy.Credentials = new NetworkCredential(ConstParam.proxyUser, ConstParam.proxyPass);
            return proxy;
        }
        public string GetWebProxyString()
        {
            return string.Format("{0}:{1}@{2}:{3}", ConstParam.proxyUser, ConstParam.proxyPass, "proxy.abuyun.com", ConstParam.proxyPort);
        }
        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetUrlParam(string url, string name)
        {
            var queryStr = GetQueryString(url);
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

        private bool hasExistObj(string guid)
        {
            return dataop.FindCount(DataTableName, Query.EQ("schoolId", guid)) > 0;
        }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            
            if (UrlQueue.Instance.Count <= Settings.ThreadCount * 10 + 50)
            {
                
                if ((DateTime.Now - Settings.LastAvaiableTime).TotalSeconds >= 60)
                {
                    Console.WriteLine("url剩余少于40");
                    Settings.LastAvaiableTime = DateTime.Now;
                    Console.WriteLine("开始获取url");
                    InitialUrlQueue();

                }
            }
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var schoolId = GetSchoolKey(args.Url);
            var root = htmlDoc.DocumentNode;
            var doc = new BsonDocument();
            //周边站点
            var siteNode = root.SelectNodes("//div[@class='block']").Where(c => c.InnerText.Contains("周边站点")).FirstOrDefault();
            if (siteNode != null)
            {
                var siteDetailNode = siteNode.SelectNodes("./div[@class='bd']/span");
                if (siteDetailNode != null)
                {
                    var docArr = new BsonArray();
                    foreach (var siteDetail in siteDetailNode)
                    {
                        docArr.Add(siteDetail.InnerText);
                    }
                    doc.Add("distinctDetail", docArr);
                }

            }

            //公交线路
            var busNode = root.SelectNodes("//div[@class='block']").Where(c => c.InnerText.Contains("公交线路")).FirstOrDefault();
            if (busNode != null)
            {
                var siteDetailNode = busNode.SelectNodes("./div[@class='bd']/span");
                if (siteDetailNode != null)
                {
                    var docArr = new BsonArray();
                    foreach (var siteDetail in siteDetailNode)
                    {
                        docArr.Add(siteDetail.InnerText);
                    }
                    doc.Add("busSiteDetail", docArr);
                }

            }
            ///2016.6.20添加更新xy轴
            var xBenStr = "var poi_lat =";
            var yBenStr = "var poi_lng =";
            var xBeginIndex = args.Html.IndexOf(xBenStr);
            var yBeginIndex = args.Html.IndexOf(yBenStr);
            if (xBeginIndex != -1 && yBeginIndex != -1)
            {
                var xValue =Toolslib.Str.Sub(hmtl, xBenStr,";");
                var yValue = Toolslib.Str.Sub(hmtl, yBenStr, ";");
                if (!string.IsNullOrEmpty(xValue) && !string.IsNullOrEmpty(yValue)) {
                    doc.Add("x", xValue.Trim());
                    doc.Add("y", yValue.Trim());
                }
                  
           }
            if (!string.IsNullOrEmpty(schoolId))
            {
                doc.Add("isUpdate", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = doc, Query = Query.EQ("schoolId", schoolId), Type = StorageType.Update });
            }

        }

        public string GetXYValue(int startIndex, int allLength, string html)
        {
            var hitResult = new StringBuilder();
            if (startIndex >= allLength) return string.Empty;
            var curChart = html[++startIndex];
            while (curChart != '"')
            {
                hitResult.AppendFormat(curChart.ToString());
                if (++startIndex < allLength)
                {
                    curChart = html[startIndex];
                }
                else
                {
                    break;
                }
            }
            return hitResult.ToString();
        }

        /// <summary>
        /// /jiaoyu/1607709.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetSchoolKey(string url)
        {
            
            var endIndex = url.LastIndexOf(".");
            var index = url.LastIndexOf("/");
            var cityCode = string.Empty;
            if (index != -1 && endIndex != -1)
            {
                cityCode = url.Substring(index + 1, endIndex - index - 1);

            }
            return cityCode;
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetCityCode(string url)
        {
            url = url.Replace("jiaoyu/", "").Replace("http://www.todgo.com","");
            var endIndex = url.LastIndexOf("/");
            var index = url.IndexOf("/");
            var cityCode = string.Empty;
            if (index!=-1&&endIndex!=-1)
            {
                cityCode = url.Substring(index + 1, endIndex- index-1);

            }
            return cityCode;
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetRegionCode(string url,string cityCode)
        {
            var fixUrl = url.Replace(cityCode+"_", "");
            return GetCityCode(fixUrl);
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
             if (string.IsNullOrEmpty(args.Html)||args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                 return true;
            }
            return false;
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
            Settings.SetUnviableIP(ipproxy);//设置为无效代理
            if (ipproxy != null)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData()
                {
                    Name = "IPProxy",
                    Document = new BsonDocument().Add("status", "1"),
                    Query = Query.EQ("ip", ipproxy.IP)
                });
                StartDBChangeProcess();
            }

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
