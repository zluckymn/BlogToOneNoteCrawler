using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
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
    public class QiXinEnterpriseDetailInfoCrawler : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/WorkPlanManage";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        Queue<BsonDocument> AccountQueue=new  Queue<BsonDocument>();
        HttpInput hi = new HttpInput();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private const string _DataTableName = "QiXinEnterpriseKey";//存储的数据库表明

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
            get { return "QiXinEnterprise"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableAccount
        {
            get { return "QiXinAccount"; }

        }
        
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QiXinEnterpriseDetailInfoCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }

        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式

            //SimulateLogin();
            //return;
            this.Settings.Timeout = 1000;
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 1;
            //Settings.AutoSpeedLimit = true;
            Settings.DBSaveCountLimit = 1;
            Settings.IgnoreFailUrl = true;//失败数据不进行继续爬取，多几次就好，因为有些数据获取的为空与无登陆一样无法判断

            var allEnterpriseList = dataop.FindAllByQuery(DataTableName,Query.And(Query.Exists("detailInfo",false),Query.EQ("cityName","西安"),Query.Or( Query.NE("status", "吊销"), Query.NE("status", "注销")))).SetFields("name", "guid").ToList();
           
            var allAccountList = dataop.FindAllByQuery(DataTableAccount,Query.And(Query.NE("status", "1"),Query.NE("isInvalid", "1"))).ToList();
            var allAccountCount = allAccountList.Count();
            if (allAccountCount <= 0)
            {
                Console.WriteLine("无登陆账号可用");
                Console.ReadKey();
                return;
            }
             Console.WriteLine("初始化数据");
          
            foreach (var enterprise in allEnterpriseList)
            {
               var guidUrl = string.Format("http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId={0}&_={1}", enterprise.Text("guid"), GetTimeLikeJS());
               UrlQueue.Instance.EnQueue(new UrlInfo(guidUrl) { Depth = 1 });
            }

            foreach (var account in allAccountList.OrderBy(c=>new Random().Next(0, allAccountCount)))
            {
                AccountQueue.Enqueue(account);
            }
 
            Settings.RegularFilterExpressions.Add("XXXX");
          
            if (SimulateLogin())
            {
                Console.WriteLine("ip登陆成功");
            }
            else
            {
                Console.WriteLine("ip模拟登陆失败");
            }

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
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
          
            var curUpdateBson = new BsonDocument();
            var guid = string.Empty;
            var queryStr = GetQueryString(args.Url);
            if (!string.IsNullOrEmpty(queryStr))
            {
               var dic = HttpUtility.ParseQueryString(queryStr);
                guid = dic["enterpriseId"] != null ? dic["enterpriseId"].ToString() : string.Empty;
             
            }
           
            //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
            if (!string.IsNullOrEmpty(guid)&& hmtl.Contains("status")) {
                        Console.WriteLine("详细信息获取成功");
                        curUpdateBson.Set("detailInfo", hmtl);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update,Query=Query.EQ("guid",guid) });
            }
            
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
            var avaiableCount = Settings.IPProxyList!=null? Settings.IPProxyList.Where(c => c.Unavaiable == false).Count():0;

            if (!args.Html.Contains("status")||args.Html.Contains("您使用验证码过于频繁")|| args.Html.Contains("请求的网址（URL）无法获取")|| args.Html.Contains("上限"))
            {
                Console.WriteLine("请刷新浏览器");
                //Thread.Sleep(3000);
                
                if (args.Html.Contains("上限"))
                {
                    canSimulateLoginEx = false;
                    Console.WriteLine("访问上限无法继续登陆");
                }
                else
                {
                    canSimulateLoginEx = true;
                }
                IPInvalidProcess(null);
                if (Settings.IPProxyList!=null&&Settings.IPProxyList.Count() > 0 && avaiableCount <= 0)
                {
                    Environment.Exit(0);
                }
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
            //Settings.SimulateCookies = "pgv_pvid=1513639250; aliyungf_tc=AQAAADPRHnGrvwQAIkg9OwOqtkYJbU4N; oldFlag=1; CNZZDATA1259577625=112366950-1466409958-%7C1466415358; hide-index-popup=1; hide-download-panel=1; _alicdn_sec=576ba5f9a986fb4802dacf51bc99b1e76724f58e; connect.sid=s%3AeYWXycPKai63BYTmB9d6h-0IM_R2kp6n.EUgfW0AmJ6GB%2F0TamTi4tT53QK4OR4yQtU1I3Ba8Ryo; userKey=QXBAdmin-Web2.0_N3iUdNobAoys4M395Pk5v%2F6Zxcwjt1tiCqeSf3X3ZnI%3D; userValue=bea26f0d-e414-168a-0fe2-b8eb4278ab07; Hm_lvt_52d64b8d3f6d42a2e416d59635df3f71=1464663982,1464775028,1464776749,1465799273; Hm_lpvt_52d64b8d3f6d42a2e416d59635df3f71=1466672591";//设置cookie值
            //return true;
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
                if (canSimulateLoginEx)
                {
                    if (SimulateLoginEx())
                    {
                        return true;
                    }
                }
                else
                {
                    
                    return false;
                }
            }
            // return SimulateLoginEx();
          
            var userName = string.Empty;
            var passWord = string.Empty;
          
            if (AccountQueue.Count() > 0)
            {
                var _curCookie = AccountQueue.Dequeue();
                if (_curCookie != null)
                {
                    Console.WriteLine("提取账号{0}", _curCookie.Text("name"));
                    userName = _curCookie.Text("name");
                    passWord = _curCookie.Text("password");
                    Settings.LoginAccount = userName;
                }
                else
                {
                    Environment.Exit(0);
                }
               
            }
            else
            {
                Environment.Exit(0);
            }
             
            IPProxy ipProxy = null;
          
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            Random rand = new Random(Environment.TickCount);
           
            //尝试登陆
            while (true)
            {
                try
                {
                    ipProxy = Settings.GetIPProxy();
                    if (ipProxy == null || string.IsNullOrEmpty(ipProxy.IP))
                    {
                        Settings.SimulateCookies = string.Empty;
                        //return true;

                    }
                    var tempCookie = string.Empty;

                    
                    hi.Url = string.Format("http://www.qixin.com/service/gtregister?t={0}", GetTimeLikeJS());
                    var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}|jordan";
                    var validUrl = "http://www.qixin.com/service/gtloginvalidate";
                    var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl);
                    if (passResult.Status==true)
                    {
                        hi.Url = "http://www.qixin.com/service/login";
                        hi.Refer = "http://www.qixin.com/login?returnURL=http%3A%2F%2Fwww.qixin.com%2Fcompany%2Fae71e9ad-81f8-4400-88bf-042dd547c93d";
                        hi.PostData = string.Format("userAcct={0}&userPassword={1}&token={2}%7Cjordan", userName, passWord, passResult.ValidCode);
                        var ho = HttpManager.Instance.ProcessRequest(hi);
                        if (ho.IsOK)
                        {
                            if (ho.TxtData.Contains("成功"))
                            {
                                canSimulateLoginEx = true;
                                Settings.SimulateCookies = ho.Cookies;
                                Console.WriteLine("过验证码模拟登陆成功");
                                return true;
                            }
                        }

                    }
                    else
                   {
                        continue;
                    }

                  
                    return false;
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (WebException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }

            }
        }

        private bool SimulateLoginEx()
        {
            // var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&requestType=search_enterprise";
            var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan";
            var validUrl = "http://www.qixin.com/service/gt-validate-for-chart";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl);
            //在查看一遍防止无线过点
            var item = new HttpItem()
            {
                URL = "http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=a4001398-6739-4941-a031-5f3cbea5459f&_=1470156562958",//URL     必需项    
                Method = "get",//URL     可选项 默认为Get   
                ContentType = "text/html",//返回类型    可选项有默认值 
                Timeout = Settings.Timeout,
                Cookie = Settings.SimulateCookies
            };
            HttpResult curResult = http.GetHtml(item);
            if (UrlContentLimit(curResult.Html))
            {
                 return false;
            }
            return passResult.Status;

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
                        Name = "QiXinAccount",
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
    }

}
