using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
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
    public class FangListCrawler_JiangYin : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/WorkPlanManage";
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
        private const string _DataTableName = "FangCrawler.JiangYin";//存储的数据库表名

        /// <summary>
        /// 项目名
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName+ "_Project"; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameHouse
        {
            get { return _DataTableName + "_House"; }

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
        public FangListCrawler_JiangYin(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }


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
            Settings.ThreadCount =10;
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            //Settings.UseSuperWebClient = true;
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");

            Settings.SimulateCookies = "UM_distinctid=163c909dac3a0b-0a58055a844938-737356c-13c680-163c909dac46fa; JSESSIONID=0000uekkAKP_Q0hcDP33LrLYCY5:-1; CNZZDATA4237675=cnzz_eid%3D1374545162-1528085523-null%26ntime%3D1528091007";
            var regionList = dataop.FindAll(DataTableNameRegion).ToList();
            var typeList = dataop.FindAll(DataTableNameType).ToList();
             var region = new BsonDocument();
             var type = new BsonDocument();
            //foreach (var region in regionList)
            {
             //   foreach (var type in typeList)
                {
                    var url = string.Format("http://www.jyfcc.com.cn/PreSellCert_List.do?region={0}&type={1}", region.Text("name"), type.Text("name"));
                    var postData = string.Format("region={0}&hsusage={1}&project=&developer=&button=%B2%E9%D1%AF",region.Text("id"),type.Text("id"));
                    if (!filter.Contains(url))//详情添加
                    {
                        filter.Add(url);
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1, PostData= postData });
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var catName = string.Empty;
            if (urlDic.ContainsValue(args.Url)) {
               
                var catObj = urlDic.Where(c => c.Value == args.Url).FirstOrDefault();
                 catName = catObj.Key;
            }
            var region = GetUrlParam(args.Url, "region");
            var type= GetUrlParam(args.Url, "type");
            var page = GetUrlParam(args.Url, "page");
            var ibodyDiv = htmlDoc.GetElementbyId("columntable_blue");
            if (ibodyDiv != null)
            {
                var add = 0;
                var update = 0;
                var aNodes = ibodyDiv.SelectNodes("//a");
                foreach (HtmlNode aNode in aNodes)
                {
                    if (aNode.Attributes["href"].Value == null || !aNode.Attributes["href"].Value.ToString().Contains("PreSellCert_Detail.do?pscid="))
                    {
                        continue;
                    }
                    if (aNode != null && aNode.Attributes["href"] != null)
                    {
                        var title = aNode.InnerText;
                        if (title.Contains("\n"))
                        {
                            var splitArray = title.SplitParam(new string[] { "\n", "\r" });
                            if (splitArray.Length >= 1)
                            {
                                title = splitArray[0].ToString();
                            }
                        }
                        var curDoc = new BsonDocument();
                        var projId = GetGuidFromUrl(aNode.Attributes["href"].Value);
                        if (string.IsNullOrEmpty(projId))
                        {
                            Console.WriteLine("projId不存在");
                            continue;
                        }
                        curDoc.Add("projId", projId);
                        curDoc.Add("region", region);
                        curDoc.Add("type", type);
                        curDoc.Add("name", title.Trim());
                        curDoc.Add("url", aNode.Attributes["href"].Value.ToString());

                        var trNode = aNode.ParentNode.ParentNode.ParentNode.ParentNode.ChildNodes.Where(c => c.InnerText.Contains("日期")).FirstOrDefault();
                        if (trNode != null)
                        {
                            var tdNodes = trNode.ChildNodes.Where(c => c.Name.ToLower() == "td").ToList();
                            if (tdNodes.Count == 4)
                            {
                                var address = tdNodes[0].InnerText.Trim().Replace("项目地址：", "").Trim();
                                var houseCount = tdNodes[2].InnerText.Trim().Replace("套", "").Trim();
                                var saleDate = tdNodes[3].InnerText.Trim().Replace("日期：", "").Trim();
                                curDoc.Add("saleDate", saleDate);
                                curDoc.Add("houseCount", houseCount);
                                curDoc.Add("address", address);
                             
                            }
                        }
                        ///cool286073/1.html?s=11
                        //提取数字

                        if (!idFilter.Contains(projId) && !hasExistObj(projId))
                        {
                            add++;
                            this.idFilter.Add(projId);
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curDoc, Name = DataTableName, Type = StorageType.Insert });
                        }
                        else
                        {
                            if (curDoc.Contains("saleDate")&&!string.IsNullOrEmpty(curDoc.Text("saleDate")))
                            {
                                var projObj = this.dataop.FindOneByQuery(this.DataTableName, Query.EQ("projId", projId));
                                if (projObj != null)
                                {
                                    var houseUpdate = new BsonDocument();
                                   // var saleDate = curDoc.Date("saleDate");
                                    houseUpdate.Add("region", projObj.Text("region"));
                                  //  houseUpdate.Add("type", type);
                                    //houseUpdate.Add("year", saleDate.Year.ToString());
                                    //houseUpdate.Add("month", saleDate.Month.ToString());
                                    //houseUpdate.Add("day", saleDate.Day.ToString());
                                    //houseUpdate.Add("saleDate", saleDate.ToString("yyyy-MM-dd"));
                                  //  DBChangeQueue.Instance.EnQueue(new StorageData() { Document = houseUpdate, Name = DataTableName, Query = Query.EQ("projId", projId), Type = StorageType.Update });
                                    // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = houseUpdate, Name = DataTableNameHouse, Query = Query.EQ("projId", projId), Type = StorageType.Update });
                                }
                            }
                            update++;
                            //Console.WriteLine("已存在");
                        }
                    }
                }
                Console.WriteLine("增加:{0}更新:{1}", add, update);

                //获取其他分页
                if (string.IsNullOrEmpty(page) || page == "1")
                {
                    var pageSpan= ibodyDiv.SelectSingleNode("//span[@class='pagelist_last']/a");
                    if (pageSpan != null&& pageSpan.Attributes["href"]!=null)
                    {
                        var num = GetUrlParam(pageSpan.Attributes["href"].Value,"page");
                        var maxPageNum = 0;
                        if (int.TryParse(num, out maxPageNum))
                        {
                            Console.WriteLine("获取页数{0}", maxPageNum);
                            for (var i = 2; i <= maxPageNum; i++)
                            {
                                var url = string.Format("{0}&page={1}",args.Url,i);
                                if (!filter.Contains(url))//详情添加
                                {
                                    filter.Add(url);
                                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = args.urlInfo.Depth, PostData = args.urlInfo.PostData });
                                }
                            }
                        }

                    }
                }

            }
            else
            {
                Console.WriteLine("目录不存在");
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
            return (this.dataop.FindCount(this.DataTableName, Query.EQ("projId", guid)) > 0);
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
            if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                return true;
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var ibodyDiv = htmlDoc.GetElementbyId("columntable_blue");
            if (ibodyDiv == null)
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
