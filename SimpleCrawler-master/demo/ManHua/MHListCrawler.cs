using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
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
    /// 用于城市与区域代码初始化
    /// </summary>
    public class MHListCrawler : ISimpleCrawler
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
        private BloomFilter<string> schoolIdFilter = new BloomFilter<string>(8000000);
        private const string _DataTableName = "MH_Cartoon";//存储的数据库表名

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
        public string DataTableNameCity
        {
            get { return "MH_CartoonCity"; }

        }



        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public MHListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            Settings.ThreadCount =1;
            Settings.ContentType = "text/html; charset=utf-8";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";
            Settings.UseSuperWebClient = true;
            Settings.hi = new HttpInput();
            HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");

            //urlDic.Add("魔物娘的相伴日常","http://www.hhcool.com/manhua/11027.html");
            // urlDic.Add("伪恋", "http://www.hhcool.com/manhua/8399.html");
            //urlDic.Add("在异世界开后宫", "http://www.hhcool.com/manhua/33112.html");
            // urlDic.Add("天空危机", "http://www.hhcool.com/manhua/21523.html");
            //urlDic.Add("闪灵酷企鹅", " http://www.hhcool.com/manhua/1099.html");
            //urlDic.Add("鬼斩", "http://www.hhcool.com/manhua/7315.html");
            //urlDic.Add("极乐天师ms", "http://www.hhcool.com/manhua/5192.html");
            //urlDic.Add("12best", "http://www.hhcool.com/manhua/16407.html");
            //urlDic.Add("柜台西施", "http://www.hhcool.com/manhua/9575.html");
            //urlDic.Add("女仆", "http://www.hhcool.com/manhua/4703.html");
            //urlDic.Add("爱意汤", "http://www.hhcool.com/manhua/14803.html");
            // urlDic.Add("美少女恶魔", "http://www.hhcool.com/manhua/30241.html");
            //urlDic.Add("入间同学入魔了", "http://www.hhcool.com/manhua/31990.html");
            //urlDic.Add("魔女", "http://www.hhcool.com/manhua/28139.html");
            // urlDic.Add("成为魔王", "http://www.hhcool.com/manhua/27520.html");
            //urlDic.Add("神眉", "http://www.hhcool.com/manhua/23826.html");
            // urlDic.Add("魔域英雄传说", "http://www.hhcool.com/manhua/2900.html");
            //urlDic.Add("双面战姬", "http://www.hhcool.com/manhua/5071.html");
            //urlDic.Add("狩灵士", "http://www.hhcool.com/manhua/4767.html");
            //urlDic.Add("精灵狩猎者", "http://www.hhcool.com/manhua/24519.html");
            //urlDic.Add("亲亲小魔女", "http://www.hhcool.com/manhua/1065.html");
            //urlDic.Add("灌篮高手", "http://www.hhcool.com/manhua/3632.html");
            urlDic.Add("灌篮高手全国大赛", "http://www.hhcool.com/manhua/6604.html");
           
            foreach (var dic in urlDic)
            {
                var url = dic.Value; 
                if (!filter.Contains(url))//详情添加
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 ,  Authorization= dic.Key });
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
            var ibodyDiv = htmlDoc.GetElementbyId("list");
            if (ibodyDiv != null)
            {
                
                var aNodes = ibodyDiv.SelectNodes("//ul[@class='cVolUl']/li/a");
                foreach (HtmlNode aNode in aNodes)
                {
                    if (aNode != null && aNode.Attributes["href"] != null)
                    {
                        var title = aNode.InnerText;
                        var curDoc = new BsonDocument();
                        curDoc.Add("catName", catName);
                        curDoc.Add("name", title.Trim());
                        curDoc.Add("url", string.Format("http://www.hhcool.com{0}", aNode.Attributes["href"].Value.ToString()));
                        ///cool286073/1.html?s=11
                        //提取数字

                        var match = Regex.Matches(curDoc.Text("url"), @"\d+");
                        if (match != null && match.Count > 0)
                        {
                            var result = match[0].Value;
                            curDoc.Add("num", int.Parse(result.ToString()));
                        }
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curDoc, Name = DataTableName, Type = StorageType.Insert });
                    }
                }
            }
            else
            {
                Console.WriteLine("目录不存在");
            }

           
        }
       

        #region method

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
            if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                return true;
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var ibodyDiv = htmlDoc.GetElementbyId("list");
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
