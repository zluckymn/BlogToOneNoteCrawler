using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    //请注意
    public class QCCEnterpriseDetailInfoGuidAdd : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/WorkPlanManage";

        DataOperation dataop = null;
        private static MongoOperation _mongoDBOp=null;
        private CrawlSettings Settings = null;
        Queue<BsonDocument> AccountQueue=new  Queue<BsonDocument>();
        HttpInput hi = new HttpInput();
      
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private const string _DataTableName = "QCCEnterpriseDetailInfo";//存储的数据库表明
        Dictionary<string, string> EnterpriseInfoMapDic = new Dictionary<string, string>();
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
            get { return _DataTableName+"URL"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameList
        {
            get { return "QCCEnterprise"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableAccount
        {
            get { return "QCCAccount"; }

        }
        
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QCCEnterpriseDetailInfoGuidAdd(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
          

        }

        /// <summary>
        /// 读取初始化队列
        /// </summary>
        public void InitialUrlQueue()
        {
#pragma warning disable CS0219 // 变量“skipCount”已被赋值，但从未使用过它的值
            ///请注意改detailInfo表guid 为eGuid
            var skipCount = 0;
#pragma warning restore CS0219 // 变量“skipCount”已被赋值，但从未使用过它的值
            var takeCount = 1000;
            var allCount = dataop.FindCount(DataTableName, Query.And(Query.Exists("detailInfo", false)));
            Console.WriteLine("待处理个数:{0}", allCount);
            //注意 后续需要对为空的在轮询一次因为之前可能有几个有值但被设置为空
            var allEnterpriseList = dataop.FindLimitByQuery(DataTableName, Query.And(Query.Exists("detailInfo", false)),new MongoDB.Driver.SortByDocument(),0,takeCount).SetFields("eGuid").ToList();
            if (allEnterpriseList.Count() > 0)
            {
                foreach (var enterprise in allEnterpriseList)
                {

                    var backDetailInfoUrl = string.Format("http://www.qichacha.com/more_findmuhou?keyNo={0}", enterprise.Text("eGuid"));
                    ////var detailInfoUrl = string.Format("http://www.qichacha.com/cms_map?keyNo={0}&upstreamCount=1&downstreamCount=1", enterprise.Text("guid"));
                    ////UrlQueue.Instance.EnQueue(new UrlInfo(detailInfoUrl) { Depth = 1 });
                    UrlQueue.Instance.EnQueue(new UrlInfo(backDetailInfoUrl) { Depth = 1 });
                }

            }
        }

        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", ConstParam.proxyHost, ConstParam.proxyPort));
            proxy.Credentials = new NetworkCredential("HJ6B0D59N2DB125P", "D9566E8C93091049");
            return proxy;
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            string connStr = "mongodb://sa:dba@192.168.1.134/SimpleCrawler";
            MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder();
            builder.Server = new MongoServerAddress("192.168.1.134", 27017);
            builder.DatabaseName = "SimpleCrawler";
            builder.Username = "sa";
            builder.Password = "dba";
            builder.SocketTimeout = new TimeSpan(00, 01, 59);
            dataop = new DataOperation(new MongoOperation(builder));
            _mongoDBOp = new MongoOperation(connStr);
                Console.WriteLine("ip模拟登陆失败");
            

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
        HttpHelper http = new HttpHelper();

        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetGuidFromUrl(string url)
        {
            var beginStrIndex = url.LastIndexOf("_");
            var endStrIndex = url.IndexOf(".");
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                var queryStr = url.Substring(beginStrIndex + 1, endStrIndex - beginStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
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
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            return;

        }
        public void ShowMessageInfo(string info)
        {
            Console.WriteLine(info);
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

       // {"status":0,"data":{"gt":"9d80817516218c6af63ab41963087b69","challenge":"099dcc97f64393fa531a96926b08ed57","success":true}}
       public class GtregisterCls
       {
            public int status;

            public DataCls data;


        }
        public class DataCls
        {
            public string gt { get; set; }
            public string challenge { get; set; }
            public bool success { get; set; }
        }

        public long GetTimeLikeJS()

        {

            long lLeft = 621355968000000000;

            DateTime dt = DateTime.Now;

            long Sticks = (dt.Ticks - lLeft) / 10000;

            return Sticks;

        }
        public static bool canSimulateLoginEx=true;
        PassGeetestHelper geetestHelper = new PassGeetestHelper();
        /// <summary>
        /// 模拟登陆，ip代理可能需要用到
        /// </summary>
        /// <returns></returns>
        public bool SimulateLogin()
        {
           
            return false;
        }

        private bool SimulateLoginEx()
        {
          
            
                 return false;
         
        }

        public bool UrlContentLimit(string html)
        {
          
            if (html.Length <= 10 || html.Contains("您使用验证码过于频繁") || html.Contains("请求的网址（URL）无法获取") || html.Contains("上限"))
            {
                return true;
            }
            else
            {
                return false;
            }
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
                  //  Document = new BsonDocument().Add("status", "1"),
                    Document = new BsonDocument().Add(string.Format("{0}_status", DataTableName), "1"),
                    Query = Query.EQ("ip", ipproxy.IP),
                    Type = StorageType.Update
                });
                StartDBChangeProcess();
            }
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
              
                    DBChangeQueue.Instance.EnQueue(new StorageData()
                    {
                        Name = "QCCAccount",
                        //  Document = new BsonDocument().Add("status", "1"),
                        Document = new BsonDocument().Add(string.Format("status", DataTableName), "1"),
                        Query = Query.EQ("name", Settings.LoginAccount),
                        Type = StorageType.Update
                    });
                    StartDBChangeProcess();
                

                //验证验证码重新进行设定
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

        private   void StartDBChangeProcessQuick()
        {
            var result = new InvokeResult();
            List<StorageData> updateList = new List<StorageData>();
            var limitCount = Settings.DBSaveCountLimit;
            if (limitCount <= 0)
            {
                limitCount = 20;
            }
            if (UrlQueue.Instance.Count >= 10 && DBChangeQueue.Instance.Count <= limitCount) return;

            {

                var temp = DBChangeQueue.Instance.DeQueue();
                if (temp != null)
                {
                    var insertDoc = temp.Document;

                    switch (temp.Type)
                    {
                        case StorageType.Insert:
                            if (insertDoc.Contains("createDate") == false) insertDoc.Add("createDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));      //添加时,默认增加创建时间
                            if (insertDoc.Contains("createUserId") == false) insertDoc.Add("createUserId", "1");
                            //更新用户
                            result = _mongoDBOp.Save(temp.Name, insertDoc); ;
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

    }

}
