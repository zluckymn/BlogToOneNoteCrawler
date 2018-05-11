// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler.Demo
{
    using Microsoft.Office.Interop.OneNote;
    using System;
    using System.Xml.Linq;
    using System.Linq;
    using System.Xml;
    using System.IO;
    using System.Text;
    using HtmlAgilityPack;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Net;
    using System.IO.Compression;
    using System.Web;
    using Yinhe.ProcessingCenter;
    using MongoDB.Bson;
    using Yinhe.ProcessingCenter.DataRule;
    using MongoDB.Driver.Builders;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// The program.
    /// </summary>
    internal partial class Program
    {
        #region Static Fields

        /// <summary>
        /// The settings.
        /// </summary>

         private static string connStr = "mongodb://MZsa:MZdba@192.168.1.124:37088/SimpleCrawler";
      // private static string connStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/SimpleCrawler";
        //private static string connStr = "mongodb://MZsa:MZdba@59.61.72.38:37088/SimpleCrawler";
       // private static string crawlerClassName = "HuiCongMaterialDetailAPPCrawler";
        private static string crawlerClassName = "SiMuListCrawler";//MHDetailCrawler

        private static MongoOperation _mongoDBOp = new MongoOperation(connStr);
        // private static string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/Shared";
        static DataOperation dataop = new DataOperation(new MongoOperation(connStr));
        private static readonly CrawlSettings Settings = new CrawlSettings();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private static BloomFilter<string> filter;
      
        private static List<string> urlFilterKeyWord = new List<string>();
        private static ISimpleCrawler simpleCrawler = null;
        static SecurityQueue<StorageData> DBUpdateQueue  ;
        #endregion
        #region 控制台关闭代理
        public delegate bool ControlCtrlDelegate(int CtrlType);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);

        static ControlCtrlDelegate newDelegate = new ControlCtrlDelegate(HandlerRoutine);

        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        static WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://http-pro.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        static string GetWebProxyString()
        {
            return string.Format("{0}:{1}@{2}:{3}", "H1538UM3D6R2133P", "511AF06ABED1E7AE", "http-pro.abuyun.com", "9010");
        }

        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy(string ip, string port)
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", ip, port));
            return proxy;

        }
        public static bool HandlerRoutine(int CtrlType)
        {
             
            switch (CtrlType)
            {
                case 0:
                    Console.WriteLine("0工具被强制关闭"); //Ctrl+C关闭
                    SaveUrlQueue();
                    break;
                case 2:
                    Console.WriteLine("2工具被强制关闭");//按控制台关闭按钮关闭
                    SaveUrlQueue();
                    break;
            }
            return false;
        }
        #endregion

        #region Methods

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
           
            if (args.Count() > 0)
            {

                crawlerClassName = args[0];
            }
            if (string.IsNullOrEmpty(crawlerClassName))
            {
                Console.WriteLine("请-classname 设置对应的爬取类");
                Console.Read();
                return;
            }
            var factoryClassName = string.Format("SimpleCrawler.Demo.{0}", crawlerClassName);
            filter = new BloomFilter<string>(5000000);
            //LandFangUserUpdateCrawler,LandFangCrawler  
            //SimpleCrawler.Demo.LandFangUserUpdateCrawler 通过模拟登陆更新*号数据
            //LandFangCityRegionCrawler 获取城市区县市的guidCode对应
            //LandFangCityRegionUpdateCrawler 更新交易状态与区县
            //QiXinEnterpriseCrawler  启信爬取对应 企业与guid
            Console.WriteLine(connStr);
            Console.WriteLine(crawlerClassName);
            Console.WriteLine("确认数据库连接后继续进行");
            simpleCrawler = SimpleCrawlerFactory.Instance.Create(factoryClassName, Settings, filter, dataop);
            //Console.ReadLine();
            //const string CityName = "beijing";
            // 设置种子地址 需要添加布隆过滤种子地址，防止重新2次读取种子地址
            //Settings.SeedsAddress.Add(string.Format("http://jobs.zhaopin.com/{0}", CityName));
            // Settings.SeedsAddress.Add(string.Format("http://www.fzhouse.com.cn:7002/result_new.asp"));
            // 设置 URL 关键字
            //Settings.HrefKeywords.Add(string.Format("/{0}/bj", CityName));
            //Settings.HrefKeywords.Add(string.Format("/{0}/sj", CityName));
            //Settings.HrefKeywords.Add(string.Format("building.asp?ProjectID="));
            //Settings.HrefKeywords.Add(string.Format("result_new"));
            // 设置爬取线程个数
            //Settings.ThreadCount = 5;
            //Settings.ThreadCount =1;
            // 设置爬取深度
            Settings.Depth = 27;

            // 设置爬取时忽略的 Link，通过后缀名的方式，可以添加多个
            Settings.EscapeLinks.Add(".jpg");

            // 设置自动限速，1~5 秒随机间隔的自动限速
            Settings.AutoSpeedLimit = false;

            // 设置都是锁定域名,去除二级域名后，判断域名是否相等，相等则认为是同一个站点
            // 例如：mail.pzcast.com 和 www.pzcast.com
            Settings.LockHost = false;
            //是否启用代理
             //Settings.CurWebProxy = GetWebProxy();
             //Settings.CurWebProxyString = GetWebProxyString();


            // 设置请求的 User-Agent HTTP 标头的值
            // settings.UserAgent 已提供默认值，如有特殊需求则自行设置

            // 设置请求页面的超时时间，默认值 15000 毫秒
            // settings.Timeout 按照自己的要求确定超时时间

            // 设置用于过滤的正则表达式
            // settings.RegularFilterExpressions.Add("http://land.fang.com/market/a0a95a6f-43d4-4b59-a948-d48f21a4e468.html");
            //代理ip模式
            //Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            //Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))));
            // Settings.IPProxyList.Add(new IPProxy("31.168.236.236:8080")); 
            //云风Bloginit初始化
            // fang99Init();
            // JGZFBlogInit();
            simpleCrawler.SettingInit();
            var master = new CrawlMaster(Settings);
            master.AddUrlEvent += MasterAddUrlEvent;
            master.DataReceivedEvent += MasterDataReceivedEvent;
            master.CrawlErrorEvent += CrawlErrorEvent;
            master.Crawl();
            // Console.WriteLine("遍历结束");
            Console.ReadKey();
        }

        
        /// <summary>
        /// The master add url event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            //符合条件的url
            //if (urlFilterKeyWord.Any(c => args.Url.Contains(c))) return false;//url过滤
            if (!simpleCrawler.CanAddUrl(args)) return false;
            if (!filter.Contains(args.Url))
            {
                filter.Add(args.Url);
                Console.WriteLine(args.Url);
                return true;
            }

            return false; // 返回 false 代表：不添加到队列中
        }

        /// <summary>
        /// The master data received event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void MasterDataReceivedEvent(DataReceivedEventArgs args)
        {
 
            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析
          try
            {
                Console.WriteLine(string.Format("当前处理：{0} ip:{1}", UrlQueue.Instance.Count,Settings.curIPProxy!=null? Settings.curIPProxy.IP:"localhost"));
                //进行ip限定处理,返回IP是否被限制了
                if (simpleCrawler.IPLimitProcess(args))
                {
                    IPInvalidProcess(args.IpProx);
                    if (Settings.IgnoreFailUrl) { 
                      UrlQueue.Instance.EnQueue(args.urlInfo);
                    }
                    Console.WriteLine(string.Format("当前：{0}被IPLimitProcess判定为IP失效页面", UrlQueue.Instance.Count));
                }
                else
                {
                   
                    simpleCrawler.DataReceive(args);
                    if (!Settings.IgnoreSucceedUrlToDB) { 
                    // Console.WriteLine("{0}处理结束", args.Url);
                    //成功处理后添加当前url到保存队列
                    var curIp = args.IpProx != null ? args.IpProx.IP : string.Empty;
                    AddSucceedUrl(args.Url, curIp, simpleCrawler.DataTableNameURL);
                        // //开始保存数据库进程，后续考虑执行一段时间后进行更新，或者超过一定数量进行更新
                    }
                    StartDBChangeProcess();
                }

                if (UrlQueue.Instance.Count <= 0) {

                    while (DBChangeQueue.Instance.Count > 0)
                    {
                        Console.WriteLine("正在等待保存数据库");
                        Thread.Sleep(1000);
                    }
                    if (DBChangeQueue.Instance.Count <= 0)
                    {
                        Console.WriteLine("处理完毕,5秒后退出");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                       
                }
                //YunFengBlogReceive(args);
                // fang99DataReceive(args);
                
            
            }
            catch (NullReferenceException ex)//未将对象引用到对象实例，将当前连接所使用的Ip进行设置为无效,IP被禁用
            {
                 IPInvalidProcess(args.IpProx);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0}出错{1}", args.Url, ex.Message));
            }
           

        }

        /// <summary>
        /// ip无效处理
        /// </summary>
        private static void IPInvalidProcess(IPProxy ipproxy)
        {
            Settings.SetUnviableIP(ipproxy);//设置为无效代理
            
            simpleCrawler.SimulateLogin();//模拟登陆
            if (ipproxy != null) {
             
                DBChangeQueue.Instance.EnQueue(new StorageData()
                {
                    Name = "IPProxy",
                    Document = new BsonDocument().Add(string.Format("{0}_status", simpleCrawler.DataTableName), "1"),
                    Query = Query.EQ("ip", ipproxy.IP)
                });
                StartDBChangeProcess();
            }

        }

        /// <summary>
        /// 对需要更新的队列数据更新操作进行批量处理,可考虑异步执行
        /// </summary>
        private static void StartDBChangeProcess()
        {
            StartDBChangeProcessQuick();
                return;
             var limitCount = Settings.DBSaveCountLimit;
            if (limitCount <= 0)
            {
                limitCount = 20;
            }
             if (UrlQueue.Instance.Count >= 10&&DBChangeQueue.Instance.Count<= limitCount) return;//待处理rul队列大于10并且更新队列小与10进行处理，批量更新
            //    var task = new Task(() =>
            //{
                    List<StorageData> updateList = new List<StorageData>();
                    while (DBChangeQueue.Instance.Count > 0&& updateList.Count()<= limitCount)
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
                            Console.WriteLine(string.Format("数据插入失败当前{0}个!", updateList.Count()));
                            foreach (var storageData in updateList)
                            {
                                DBChangeQueue.Instance.EnQueue(storageData);
                            }
                        }
                    }
                    ///队列中还有
                    if (DBChangeQueue.Instance.Count > 0)
                    {
                        StartDBChangeProcess();
                    }
           // }
            //);
            //task.Start();
       }



        private static void StartDBChangeProcessQuick()
        {
            var result = new InvokeResult();
            List<StorageData> updateList = new List<StorageData>();
            var limitCount = Settings.DBSaveCountLimit;
            //if (limitCount <= 0)
            //{
            //    limitCount = 5;
            //}
            // if (UrlQueue.Instance.Count >= 10 && DBChangeQueue.Instance.Count < limitCount) return;
            var curDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            while (DBChangeQueue.Instance.Count > 0)
            {

                var temp = DBChangeQueue.Instance.DeQueue();
                if (temp != null)
                {
                    var insertDoc = temp.Document;

                    switch (temp.Type)
                    {
                        case StorageType.Insert:
                            if (insertDoc.Contains("createDate") == false) insertDoc.Add("createDate", curDate);      //添加时,默认增加创建时间
                            if (insertDoc.Contains("createUserId") == false) insertDoc.Add("createUserId", "1");
                            //更新用户
                            result = _mongoDBOp.Save(temp.Name, insertDoc);
                            break;
                        case StorageType.Update:
                            // insertDoc.Set("updateDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //更新时间
                            // insertDoc.Set("updateUserId", "1");
                            result = _mongoDBOp.Save(temp.Name, temp.Query, insertDoc);
                            break;
                        case StorageType.Delete:
                            result = _mongoDBOp.Delete(temp.Name, temp.Query);
                            break;
                    }
                    //logInfo1.Info("");
                    if (result.Status == Status.Failed) throw new Exception(result.Message);

                }

            }

            if (DBChangeQueue.Instance.Count > 0)
            {
                StartDBChangeProcessQuick();
            }
            
        }
        /// <summary>
        /// 对成功处理后的url进行保存,以小写保证防止大小写
        /// </summary>
        private static void AddSucceedUrl(string url,string ip,string DataTableName)//保存
        {
            var urlBson = new BsonDocument().Add("url", url.ToLower()).Add("ip", ip);
            DBChangeQueue.Instance.EnQueue(new StorageData() {  Document=urlBson, Name=DataTableName,Type=StorageType.Insert});
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
         
        /// <summary>
        /// 发送至我的oneNote
        /// </summary>
        /// <param name="html"></param>
        private static void SendOneNote(string html, string url, string pageName)
        {

            var ls = new Application();
            string ls_return = "";
            var pageId = string.Empty;

            string notebookXml;
            ls.GetHierarchy(null, HierarchyScope.hsPages, out notebookXml);
            string existingPageId = string.Empty;
            var doc = XDocument.Parse(notebookXml);
            var ns = doc.Root.Name.Namespace;
            var session = doc.Descendants(ns + "Section").Where(n => n.Attribute("name").Value == pageName).FirstOrDefault();
            if (session != null)
            {
                existingPageId = session.Attribute("ID").Value;
                ls.CreateNewPage(existingPageId, out pageId, NewPageStyle.npsDefault);
                var page = new XDocument(new XElement(ns + "Page",
                                          new XElement(ns + "Outline",
                                            new XElement(ns + "OEChildren",
                                              new XElement(ns + "OE",
                                                new XElement(ns + "T",
                                                  new XCData(html + url)))))));
                page.Root.SetAttributeValue("ID", pageId);
                try
                {
                    ls.UpdatePageContent(page.ToString(), DateTime.MinValue);


                }
                catch (Exception ex)
                {
                    ls.DeleteHierarchy(pageId, DateTime.MinValue);
                    Console.WriteLine(url + ex.Message);
                }
            }


        }
        #endregion


        /// <summary>
        /// 异常捕获
        /// </summary>
        /// <param name="args"></param>
        private static void CrawlErrorEvent(CrawlErrorEventArgs args)
        {
          

            simpleCrawler.ErrorReceive(args);

            if (args.needChangeIp)//限制无法访问的IP
            {
                IPInvalidProcess(args.IpProx);
            }

            var nextDepth = args.Depth + Settings.Depth / 10;
            //超时考虑重新添加,防止无限循环
            if (args.needTryAgain&&Settings.IgnoreFailUrl==false)
            {
                if (args.Depth <= Settings.Depth)
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = nextDepth });
                }
                Console.WriteLine(string.Format("{0}重试深度{1}{2}", args.Exception.Message, nextDepth, args.IpProx!=null? args.IpProx.IP:string.Empty));
            }
           

            Console.WriteLine(args.Exception.Message);
        }



        /// <summary>
        /// 序列化当前队列
        /// </summary>
        public static void SaveUrlQueue()
        {
            List<string> QueueList = new List<string>();
            if (UrlQueue.Instance.Count > 0)
            {
                while (UrlQueue.Instance.Count > 0)
                {
                    QueueList.Add(UrlQueue.Instance.DeQueue().UrlString);
                }
                SerializerXml<List<string>> serial = new SerializerXml<List<string>>(QueueList);
                serial.BuildXml(string.Format("UrlQueue_{0}.xml", simpleCrawler.DataTableName));
            }
        }
        /// <summary>
        /// 反序列化当前队列
        /// </summary>
        /// <returns></returns>
        public static List<string> LoadUrlQueue()
        {
            var fileName = string.Format("UrlQueue_{0}.xml", simpleCrawler.DataTableName);
            List<string> QueueList = new List<string>();
            if (File.Exists(fileName)) { 
                SerializerXml<List<string>> serial = new SerializerXml<List<string>>(QueueList);
                QueueList = serial.BuildObject(fileName);
                if (QueueList.Count() > 0)
                {
                    QueueList.ForEach(c => { UrlQueue.Instance.EnQueue(new UrlInfo(c)); });

                }
            }
            return QueueList;
        }
    }
}