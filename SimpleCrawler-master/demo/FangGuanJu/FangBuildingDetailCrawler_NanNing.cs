using DotNet.Utilities;
using HtmlAgilityPack;
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
    /// 用于城市与区域代码初始化
    /// </summary>
    public class FangBuildingDetailCrawler_NanNing : ISimpleCrawler
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
        private const string _DataTableName = "FangCrawler.NanNing";//存储的数据库表名

        /// <summary>
        /// 项目名
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName + "_Project"; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameHouse
        {
            get { return _DataTableName + "_House"; }

        }
        /// <summary>
        /// 房屋名
        /// </summary>
        public string DataTableNameBuilding
        {
            get { return _DataTableName + "_Building"; }

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
            get { return _DataTableName + "_Region"; }

        }
        /// <summary>
        /// 五类类型
        /// </summary>
        public string DataTableNameType
        {
            get { return _DataTableName + "_Type"; }

        }



        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public FangBuildingDetailCrawler_NanNing(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }


        List<BsonDocument> buildingList = new List<BsonDocument>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount = 10;
            Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/66.0.3359.181 Safari/537.36";
            Settings.HeadSetDic = new Dictionary<string, string>();
            Settings.HeadSetDic.Add("Accept-Encoding", "gzip, deflate");
            Settings.SimulateCookies = "__51cke__=; __tins__18980026=%7B%22sid%22%3A%201530601694251%2C%20%22vd%22%3A%208%2C%20%22expires%22%3A%201530604031753%7D; __51laig__=8";
            Settings.Referer = "www.nnfcxx.com";
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");
          
            buildingList = dataop.FindAllByQuery(DataTableNameBuilding, Query.NE("isUpdate", 1)).ToList();
            //iCPH
            foreach (var building in buildingList)
            {
                var mhUrl = building.Text("url");
                if (!filter.Contains(mhUrl))//具体页面
                {
                    filter.Add(mhUrl);
                    UrlQueue.Instance.EnQueue(new UrlInfo(mhUrl) { Depth = 1, UniqueKey= building.Text("projId"), Authorization = building.Text("buildNO") });
                }
            }
            // UrlQueue.Instance.EnQueue(new UrlInfo("http://www.hhcool.com/cool286073/1.html?s=11&d=0"+"&checkPageCount=1") { Depth = 1 });
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
        public static List<Task> allTask = new List<Task>();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// cool62061/1.html?s=10&d=0
        /// 
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var root = htmlDoc.DocumentNode;
            var liNodes = root.SelectNodes("//li[contains(@class,'tjCor')]");
            var projId = args.urlInfo.UniqueKey;
            var buildNO= args.urlInfo.Authorization;
            var page = GetUrlParam(args.Url, "page");
            //获取页数
            if (liNodes != null)
            {
                var add=0;
                var update = 0;
             
                if (liNodes != null)
                {
                    foreach (var trNode in liNodes)
                    {
                        var oldTitle = string.Empty;
                        if (trNode.Attributes.Contains("oldtitle"))
                        {
                            oldTitle = trNode.Attributes["oldtitle"].Value.ToString();
                        }
                        if (trNode.Attributes.Contains("title"))
                        {
                            oldTitle = trNode.Attributes["title"].Value.ToString();
                        }
                        
                        if (string.IsNullOrEmpty(oldTitle))
                        {
                            continue;
                        }
                            var roomNo = Toolslib.Str.Sub(oldTitle, "房号：", "结构：").Replace("<br />","").Trim();
                            var structure = Toolslib.Str.Sub(oldTitle, "结构：", "户型：").Replace("<br />", "").Trim();
                            var purpose = Toolslib.Str.Sub(oldTitle, "用途：", "建筑面积：").Replace("<br />", "").Trim();
                            var type = Toolslib.Str.Sub(oldTitle, "户型：", "用途：").Replace("<br />", "").Trim();
                            var saleArea = Toolslib.Str.Sub(oldTitle, "建筑面积：", "拟售单价：").Replace("<br />", "").Replace("平方米", "").Trim();
                            var price = Toolslib.Str.Sub(oldTitle, "拟售单价：", "").Replace("<br />", "").Replace("元/平方米", "").Trim();
                            var houseDoc = new BsonDocument();
                            houseDoc.Add("projId", projId);
                            houseDoc.Add("buildNO", buildNO);
                            houseDoc.Add("roomNo", roomNo);
                            houseDoc.Add("purpose", purpose);
                            houseDoc.Add("saleArea", saleArea);
                            houseDoc.Add("price", price);
                            var style =trNode.Attributes["class"].Value.ToString();
                            if (string.IsNullOrEmpty(style))
                            {
                              Console.WriteLine("当前房字无样式文件");
                              continue;
                            }
                            var saleStatus = string.Empty;
                            switch (style)
                            {
                              case "tjCor1":
                                saleStatus = "已备案";
                                houseDoc.Set("saleStatus", "1");
                                break;
                            case "tjCor2":
                                saleStatus = "已签约";
                                houseDoc.Set("saleStatus", "1");
                                break;
                            case "tjCor3":
                                saleStatus = "签约中";
                                houseDoc.Set("saleStatus", "1");
                                break;
                            case "tjCor4":
                                saleStatus = "未出售";
                                houseDoc.Set("saleStatus", "0");
                                break;
                            case "tjCor5":
                                saleStatus = "在建工程抵押";
                                break;
                            }

                            houseDoc.Add("saleStatusName", saleStatus);
                           
                           
                            var curHouseObj = this.dataop.FindOneByQuery(this.DataTableNameHouse, Query.And(Query.EQ("roomNo", roomNo), Query.EQ("buildNO", buildNO),Query.EQ("projId", projId)));
                            if (!idFilter.Contains(roomNo) && curHouseObj==null)
                            {
                                
                                add++;
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = houseDoc, Name = DataTableNameHouse, Type = StorageType.Insert });
                            }
                            else
                            {
                                
                                update++;
                            }
                     }
                    Console.WriteLine("新增{0}更新:{1}", add,update);
                    //  DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isUpdate",1) , Name = DataTableName,Query=Query.EQ("projId", projId), Type = StorageType.Update });
                }
           }
           

        }


        private bool hasExistObj(string guid)
        {
            return (this.dataop.FindCount(this.DataTableNameHouse, Query.EQ("houseId", guid)) > 0);
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



        #region method
 
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
            var root = htmlDoc.DocumentNode;
            var liNodes = root.SelectNodes("//li[contains(@class,'tjCor')]");
            if (liNodes == null)
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
