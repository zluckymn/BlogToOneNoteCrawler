using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 江阴房管局列表爬取
    /// </summary>
    public class MiListCrawler : ISimpleCrawler
    {

        
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：
        ///  www.hhcool.com/cool62061/1.html?s=10&d=0
        /// 结束http://www.hhcool.com/cool62061/253.html?s=10&d=0
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> idFilter = new BloomFilter<string>(8000000);
        private const string _DataTableName = "MiTuProfile";//存储的数据库表名

        /// <summary>
        /// 项目名
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameDetail
        {
            get { return _DataTableName + "Detail"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }

        /// <summary>
        /// 区域
        /// </summary>
        public string DataTableNameRegion
        {
            get { return _DataTableName+"_Region"; }

        }
        /// <summary>
        /// 五类类型
        /// </summary>
        public string DataTableNameType
        {
            get { return _DataTableName+"_Type"; }

        }


        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public MiListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }

        const int takeCount = 10;
        private Dictionary<string, string> urlDic = new Dictionary<string, string>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =1;
            Settings.Accept = "application/json, text/plain, */*";
           // Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate, br");
            //Settings.UseSuperWebClient = true;
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");

            //Settings.SimulateCookies = "serviceToken=rcu+GfCMDhx4ZCDJLkqDU3/2m8d3M3zMS0UfygZTLjHh0Pc1ch+8xHq9RcoydhhNhFpUIzLU+dE/QTFqlBNxUMxmE1Zm6Le0D5+Ued9T9M/4tRwfTIaqhcthlNd4mbjUOKcQmLv1Sl/mBIk7nYgGwC4wjcKOWoqhyScI3v/P63KN6/tHny5ukDe8nu4VfkLYty8g1R/J1xTzpeUe8Eua9pqnp8RfJxaijBkkXDc5CLCZieq2/Jdw7E1pbUUIMyaLLkGPX2qIr1PWV7k8hVi8Pg==; userId=86746990; jiamitu_slh=m+6hSHbUeRXZg+u7iCPkZycZ+Bs=; jiamitu_ph=iYZ5flgCd0IjNWfZk3N+Xw==; Hm_lvt_08ad038db088bb26c69084e80bc70125=1529372392,1529372396; Hm_lpvt_08ad038db088bb26c69084e80bc70125=1531119965";


            var allCount = 697591;
            var allPage = (allCount/ takeCount)+1;
            //foreach (var region in regionList)
            {
               for(var pageIndex=1; pageIndex <= 1; pageIndex++)
                {
                    //https://jiamitu.mi.com/pet/data/list?page=1&limit=10&order=rare_degree&orderBy=desc&followUp=https:%2F%2Fjiamitu.mi.com%2Fhome 
                    var url = string.Format("https://jiamitu.mi.com/pet/data/list?page={0}&limit=10&order=generated_id&orderBy=desc&followUp=https:%2F%2Fjiamitu.mi.com%2Fhome%23", pageIndex,takeCount);
                   
                    if (!filter.Contains(url))//详情添加
                    {
                        filter.Add(url);
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                    }
                }
            }

            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/CitySelect.aspx"));
            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("初始化失败");
            }

        }

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功");
        }

        public static object lockThis = new object();

        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// cool62061/1.html?s=10&d=0
        /// 
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            //获取图片地址
            JObject jsonObj = JObject.Parse(args.Html);
            var result = jsonObj["result"];
            if (result == null) return;
            var total = int.Parse(result["total"].ToString());
            var data= result["data"];
            var page = GetUrlParam(args.Url, "page");
            var insert = 0;
            var update = 0;
            foreach (var entInfo in data.ToList())
            {

                BsonDocument document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(entInfo.ToString());

                var guid = document.Text("id");
                document.Set("id", guid);
                if (!idFilter.Contains(guid) && !hasExistObj(guid))
                {
                    insert++;
                    idFilter.Add(guid);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = document, Name = DataTableName, Type = StorageType.Insert });
                   
                }
                else
                {
               
                    update++;
                }
            }
            Console.WriteLine("获得数据当前添加：{0} 更新{1}剩余url:{2}", insert, update, UrlQueue.Instance.Count);
            var pageCount = ((total) / takeCount) + 1;
            if (!string.IsNullOrEmpty(page)&& page=="1")
            {
                for (var pageIndex = 2; pageIndex <= pageCount; pageIndex++)
                {
                    var url = string.Format("https://jiamitu.mi.com/pet/data/list?page={0}&limit={1}&order=generated_id&orderBy=desc&followUp=https:%2F%2Fjiamitu.mi.com%2Fhome%23", pageIndex, takeCount);

                    if (!filter.Contains(url))//详情添加
                    {
                        filter.Add(url);
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                    }
                }
            }
        }


        #region method
        private string GetNum(string url)
        {
            var match = Regex.Matches(url, @"\d+");
            if (match != null && match.Count > 0)
            {
                var result = match[0].Value;
                return result;
            }
            return string.Empty;
        }

        private bool hasExistObj(string guid)
        {
            return (this.dataop.FindCount(this.DataTableName, Query.EQ("id", guid)) > 0);
        }

        private static string GetGuidFromUrl(string url)
        {
            int num = url.LastIndexOf("=");
            int num2 = url.Length;
            if ((num != -1) && (num2 != -1))
            {
                if (num > num2)
                {
                    int num3 = num;
                    num = num2;
                    num2 = num3;
                }
                return url.Substring(num + 1, (num2 - num) - 1);
            }
            return string.Empty;
        }

        public string GetInnerText(HtmlNode node)
        {
            if ((node == null) || string.IsNullOrEmpty(node.InnerText))
            {
                throw new NullReferenceException();
            }
            return node.InnerText;
        }

        private static string GetQueryString(string url)
        {
            int index = url.IndexOf("?");
            if (index != -1)
            {
                return url.Substring(index + 1, (url.Length - index) - 1);
            }
            return string.Empty;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetUrlParam(string url, string name)
        {
            NameValueCollection values = HttpUtility.ParseQueryString(GetQueryString(url));
            return ((values[name] != null) ? values[name].ToString() : string.Empty);
        }
 
 
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        public bool IPLimitProcess(DataReceivedEventArgs args)
        {

            if (string.IsNullOrEmpty(args.Html.Trim()) || args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                return true;
            }
            try
            {
                JObject jsonObj = JObject.Parse(args.Html);
                var result = jsonObj["result"];
                if (result == null)
                {

                    return true;
                }
            }
            catch (Exception ex)
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
        #endregion
    }

}
