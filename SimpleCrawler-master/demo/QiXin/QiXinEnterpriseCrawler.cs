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
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    public class QiXinEnterpriseCrawler : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/WorkPlanManage";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        Queue<BsonDocument> AccountQueue=new  Queue<BsonDocument>();
        HttpInput hi = new HttpInput();
        List<string> existGuidList = new List<string>();
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
        /// 返回
        /// </summary>
        public string DataTableNameFileName
        {
            get { return "txt/竞得方.txt"; }

        }
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QiXinEnterpriseCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 1;
            //Settings.AutoSpeedLimit = true;
            Settings.DBSaveCountLimit = 1;
            //Settings.AutoSpeedLimitMaxMSecond = 20000;
            //Settings.AutoSpeedLimitMinMSecond = 10000;
            //var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid").ToList();
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("name", "guid").ToList();
            var existNameList = allEnterpriseList.Select(c => (BsonValue)c.Text("name")).ToList();
            existGuidList = allEnterpriseList.Select(c => c.Text("guid")).ToList();
            var allAccountList = dataop.FindAllByQuery(DataTableAccount,Query.And(Query.NE("status", "1"), Query.NE("isInvalid", "1"))).ToList();
            if (allAccountList.Count() <= 0)
            {
                Console.WriteLine("无登陆账号可用");
                Console.ReadKey();
                return;
            }
            //var cityNameStr = "上海,北京,成都,福州,广州,杭州,黄山,济南,龙岩,南昌,南京,宁波,泉州,深圳,苏州,武汉,西安,厦门,大连,长沙,合肥,镇江,宁波,中山,郑州,昆明,江苏,重庆";
            // var cityNameStr = "上海,北京,成都,广州,泉州,深圳,厦门,重庆";
            var cityNameStr = "上海,北京,广州,深圳";//北京,广州,上海
            var cityNameList = cityNameStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var allNeedEnterpriseList= dataop.FindAllByQuery(DataTableNameList, Query.And(Query.In("城市",cityNameList.Select(c=>(BsonValue)c)),Query.EQ("isFirst", "1"),Query.NE("status","1"), Query.NE("isSearched", "1") )).SetFields("name").Take(1500).Select(c=>c.Text("name")).ToList(); 
            Console.WriteLine("初始化数据");
            //StreamReader reader = null;
            var updateSB = new StringBuilder();
              foreach (string enterpriseName in allNeedEnterpriseList.Where(c=>c.Length>3))
                    {
                        //if (allEnterpriseList.Where(c => c.Text("name") == enterpriseName.Trim()).Count() > 0) continue;
                        //var enterPriseNameArray = enterpriseName.Split(new string[] { ",","、","，","和"},StringSplitOptions.RemoveEmptyEntries);
                        //foreach(var name in enterPriseNameArray){
                        enterpriseName.Replace("（）", "");
                        var url = string.Format("http://www.qixin.com/search?key={0}&type=enterprise&source=&isGlobal=Y", HttpUtility.UrlEncode(enterpriseName).ToUpper());
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                        //}
                    }

            foreach (var cookie in allAccountList)
            {
                AccountQueue.Enqueue(cookie);
            }

            //Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy",Query.And(Query.NE("status", "1"))).ToList();
            //  Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            //  Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.RegularFilterExpressions.Add("XXXX");
            //Settings.RegularFilterExpressions.Add(@".*?market/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}).html");
            //Settings.RegularFilterExpressions.Add(@".*?data/land.*?.html");
            //广州_440105________1_1.html
            //Settings.RegularFilterExpressions.Add(@".*?data/land/.*?_.*?________.*?_1.html");
            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/440100_________0_1.html"));
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var curUpdateBson = new BsonDocument();
            var oldBsonDocument = new BsonDocument();
            var queryStr = GetQueryString(args.Url);
            var oldName = string.Empty;
            if (!string.IsNullOrEmpty(queryStr))
            {
                var dic = HttpUtility.ParseQueryString(queryStr);
                 var  serchKey = dic["key"] != null ? dic["key"].ToString() : string.Empty;
                 oldName = HttpUtility.UrlDecode(serchKey);
                 curUpdateBson.Add("oldName", oldName);
                 DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isSearched", "1"), Query = Query.EQ("name", oldName), Name = DataTableNameList, Type = StorageType.Update });
            }
          
            var searchResult = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='search-result-company-name']");
            if (searchResult == null) return;
            var enterpriseName = searchResult.InnerText;
            var url = searchResult.Attributes["href"] != null ? searchResult.Attributes["href"].Value : string.Empty;
            if (string.IsNullOrEmpty(url)) return;
            curUpdateBson.Add("name", enterpriseName);
            curUpdateBson.Add("url", string.Format("http://www.qixin.com{0}",url));
            ///company/fc0de68c-acff-4e5e-9444-7ed41761c2f5
            var startIndex = url.LastIndexOf("/");
            if (startIndex == -1) return;
            var guid = url.Substring(startIndex + 1, url.Length - startIndex - 1);
            curUpdateBson.Add("guid", guid);
            //获取企业信息http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
            if (!string.IsNullOrEmpty(guid) && !existGuidList.Contains(guid))
            {
                existGuidList.Add(guid);
                var guidUrl = string.Format("http://www.qixin.com/service/getRootNodeInfoByEnterpriseId?enterpriseId={0}&_={1}", guid, GetTimeLikeJS());
                //获取 信息
                hi.Url = guidUrl;
                hi.Refer = string.Format("http://www.qixin.com/company/network/{0}?name={1}", guid, HttpUtility.UrlEncode(enterpriseName));
                hi.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1653.0 Safari/537.36";
                //hi.Cookies = Settings.SimulateCookies;
                var ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    if (ho.TxtData.Contains("status"))
                    {
                        Console.WriteLine("详细信息获取成功");
                        curUpdateBson.Set("detailInfo", ho.TxtData);
                    }
                    else
                    {
                     
                    }

                }
                hi.Url = string.Format("http://www.qixin.com/service/getEnterpriseClan?eid={0}&_={1}", guid, GetTimeLikeJS());
                hi.Refer = string.Format("http://www.qixin.com/company/network/{0}?name={1}", guid, HttpUtility.UrlEncode(enterpriseName));
                //hi.Cookies = Settings.SimulateCookies;
                 ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    if (ho.TxtData.Contains("status"))
                    {
                        Console.WriteLine("企业链信息获取成功");
                        curUpdateBson.Set("relationInfo", ho.TxtData);
                    }
                   

                }

                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });
              // DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status","1"),Query= Query.EQ("name", enterpriseName), Name = DataTableNameList, Type = StorageType.Update });
            }
            if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(oldName))
            {
                oldBsonDocument.Add("guid", guid);
                oldBsonDocument.Add("searchName", enterpriseName);
                oldBsonDocument.Set("status", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = oldBsonDocument, Query = Query.EQ("name", oldName), Name = DataTableNameList, Type = StorageType.Update });
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
            
            if (args.Html.Contains("您使用验证码过于频繁")|| args.Html.Contains("请求的网址（URL）无法获取")|| args.Html.Contains("上限"))
            {
                if (!string.IsNullOrEmpty(Settings.LoginAccount) &&!args.Html.Contains(Settings.LoginAccount))//是否已经退出登录
                {
                    canSimulateLoginEx = false;
                    Console.WriteLine("登陆信息已丢失");
                }
                    else { 
                    Console.WriteLine("当前ip被验证码检测已过期");
                    if (args.Html.Contains("验证后继续使用"))
                    {
                        canSimulateLoginEx = true;
                    }
                    else {
                        canSimulateLoginEx = false;
                        if (args.Html.Contains("上限") || args.Html.Contains("使用验证码过于频繁"))
                        {

                            Console.WriteLine("访问上限无法继续登陆");
                        }
                        else {
                            Console.WriteLine("未知原因无法访问");
                        }
                    }
                }

                IPInvalidProcess(null);
                if (Settings.IPProxyList!=null&&Settings.IPProxyList.Count() > 0 && avaiableCount <= 0)
                {
                    Environment.Exit(0);
                }
                return true;
            }
         
            Console.WriteLine("当前剩余可用Ip数：{0}",avaiableCount);
           
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var searchResult = htmlDoc.DocumentNode.SelectSingleNode("//a[@class='search-result-company-name']");
            if (searchResult == null)
            {
                Console.WriteLine("页面获取失败");
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
            
            //if (!string.IsNullOrEmpty(Settings.LoginAccount))
            //{
            //    if (canSimulateLoginEx)
            //    {
            //        if (SimulateLoginEx())
            //        {
            //            return true;
            //        }
            //    }
                
            //}
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
            //hi.EnableProxy = true;
            //hi.ProxyIP = "127.0.0.1";
            //hi.ProxyPort = 8888;
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
                    if (passResult.Status == true)
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
                catch (WebException ex)
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }
                catch (Exception ex)
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }

            }
        }
        //通过搜索企业的验证吗
        private bool SimulateLoginEx()
        {
            Console.WriteLine("开始处理当前搜索企业验证码");
            HttpOutput ho = HttpManager.Instance.ProcessRequest(hi);
            Random rand = new Random(Environment.TickCount);
           
            hi.Url = string.Format("http://www.qixin.com/service/gtregister?t=={0}&_={1}", GetTimeLikeJS(), GetTimeLikeJS());
            var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}|%7Cjordan&requestType=search_enterprise";
            var validUrl = "http://www.qixin.com/service/gtvalidate";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl);
            return passResult.Status;
        }


        /// <summary>
        /// 模拟登陆，ip代理可能需要用到
        /// </summary>
        /// <returns></returns>
        public bool SimulateLogin_abort()
        {
            
            //if (!string.IsNullOrEmpty(Settings.LoginAccount))
            //{
            //    if (canSimulateLoginEx)
            //    {
            //        if (SimulateLoginEx())
            //        {
            //            return true;
            //        }
            //    }
            //    else
            //    {
                    
            //        return false;
            //    }
            //}
            // return SimulateLoginEx();
            //Settings.SimulateCookies = "pgv_pvid=2450963536; hide-download-panel=1; _alicdn_sec=576a3ab0af22f4e5ebdbcefe41d61e787594cc18; aliyungf_tc=AQAAAIZesm/zKwsAIkg9O1Mu6IT2uP2O; oldFlag=1; connect.sid=s%3ACds536zVk0sMJohB3xK92r1aqLLzZ2kS.S7xPCWjVBh%2Fv4HTndSuceVBHiz8qzbU4mOW27BoNNlk; hide-index-popup=1; userKey=QXBAdmin-Web2.0_RaVYNd5IpN6lVyiaV9k9vkzHXo5L8gWDaXE0zTdpUUM%3D; userValue=f56373d4-f3bf-74aa-9e60-c2e716af57a7; Hm_lvt_52d64b8d3f6d42a2e416d59635df3f71=1464663566,1464942208,1466478326,1466495053; Hm_lpvt_52d64b8d3f6d42a2e416d59635df3f71=1466579551";//设置cookie值
            //return true;
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
            //if (Settings.IPProxyList == null || Settings.IPProxyList.Count() <= 0)
            //{
            //    Environment.Exit(0);
            //}

           // HttpHelper http = new HttpHelper();
          
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            Random rand = new Random(Environment.TickCount);
            //hi.EnableProxy = true;
            //hi.ProxyIP = "127.0.0.1";
            //hi.ProxyPort = 8888;
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

                
                    // //获取临时验证地址
                    //HttpItem item_login = new HttpItem()
                    //{
                    //    URL = "http://www.qixin.com/",//URL     必需项
                    //    Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                    //    Method = "GET",//URL     可选项 默认为Get   
                    //    ContentType = "text/html",//返回类型    可选项有默认值 
                    //    KeepAlive = true,
                      
                    //};
                    //HttpResult result_login = http.GetHtml(item_login);
                    //if (result_login.StatusCode == HttpStatusCode.OK)
                    //{
                       
                    //    foreach (CookieItem s in HttpCookieHelper.GetCookieList(result_login.Cookie))
                    //    {
                    //        if (!s.Key.Contains("Path"))
                    //        {
                    //            tempCookie += HttpCookieHelper.CookieFormat(s.Key, s.Value);
                    //        }
                    //    }
                    //}

                    //获取临时验证地址
                    HttpItem item = new HttpItem()
                    {
                        URL = "http://120.27.110.11:9600/login_biz/login.oko?uid=01161add5a3c4c55bd9c133baa9effd0",//URL     必需项
                        Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                        Method = "GET",//URL     可选项 默认为Get   
                        ContentType = "text/html",//返回类型    可选项有默认值 
                        KeepAlive = true,
                        Timeout = 9000
                    };
                    HttpResult result = http.GetHtml(item);
                    var needPostUrl = string.Empty;
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        needPostUrl = result.Html;
                        Console.WriteLine("获取打码平台地址:{0}", needPostUrl);
                    }

                    hi.Url = string.Format("http://120.27.110.11:9600/login_biz/query_money.oko?uid=01161add5a3c4c55bd9c133baa9effd0");
                    var ho = HttpManager.Instance.ProcessRequest(hi);
                    if (ho.IsOK)
                    {
                        Console.WriteLine("剩余点数{0}", ho.TxtData);
                    }

                        var postDate = string.Empty;
                    
                    hi.Url = string.Format("http://www.qixin.com/service/gtregister?t={0}", GetTimeLikeJS());
                    ho = HttpManager.Instance.ProcessRequest(hi);
                    if (ho.IsOK)
                    {

                        var ser = new DataContractJsonSerializer(typeof(GtregisterCls));
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(ho.TxtData));
                        GtregisterCls gtregisterResult = (GtregisterCls)ser.ReadObject(ms);
                        if (gtregisterResult != null && gtregisterResult.status == 0)
                        {
                            postDate = string.Format("data={0}|{1}", gtregisterResult.data.gt, gtregisterResult.data.challenge);
                            Console.WriteLine("获取打码post验证码:{0}", postDate);
                        }
                      
                    }


                    HttpItem item3 = new HttpItem()
                    {
                        UserAgent= "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E)",
                        URL = needPostUrl,//URL     必需项
                        Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                        Method = "POST",//URL     可选项 默认为Get   
                        ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值 
                        KeepAlive = true,
                        Timeout = 90000,
                         Postdata = postDate
                        //Postdata = "data=9d80817516218c6af63ab41963087b69|5520e63890a83184ff1bfaa67126575b"
                    };
                    HttpResult result3 = http.GetHtml(item3);
                    var challenge = string.Empty;
                    var validCode = string.Empty;
                    if (result3.StatusCode == HttpStatusCode.OK && result3.Html.Contains("success"))
                    {
                        string[] lastvcode = result3.Html.Replace("success:", string.Empty).Split(new char[] { '|' });
                         validCode = lastvcode[0];
                        challenge = lastvcode[1];
                        Console.WriteLine("提交打码平台成功{0}|{1}", validCode, challenge);
                    }
                    else
                    {
                        continue;
                    }

                 
                    hi.Url = "http://www.qixin.com/service/gtloginvalidate";
                   
                    hi.PostData = string.Format("geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}|jordan", challenge, validCode);
                    ho = HttpManager.Instance.ProcessRequest(hi);

                    if (ho.IsOK)
                    {
                        if (ho.TxtData.Contains("success"))
                        {
                             
                            hi.Url = "http://www.qixin.com/service/login";
                            hi.Refer = "http://www.qixin.com/login?returnURL=http%3A%2F%2Fwww.qixin.com%2Fcompany%2Fae71e9ad-81f8-4400-88bf-042dd547c93d";
                            hi.PostData = string.Format("userAcct={0}&userPassword={1}&token={2}%7Cjordan",userName,passWord,validCode);
                              ho = HttpManager.Instance.ProcessRequest(hi);
                            if (ho.IsOK)
                            {
                                if (ho.TxtData.Contains("成功")) {
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
                    }

                  
                    return false;
                }
                catch (WebException ex)
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }
                catch (Exception ex)
                {
                    canSimulateLoginEx = false;
                    IPInvalidProcess(ipProxy);
                }

            }
        }

        private bool SimulateLoginEx_ablort()
        {

            //HttpInput hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            //Random rand = new Random(Environment.TickCount);

            // 首先调用登录API
            //1:登录打码平台 ,这是我的API接口地址，^_^
            hi.Url = "http://120.27.110.11:9600/login_biz/login.oko?uid=01161add5a3c4c55bd9c133baa9effd0";
            // 请求访问
            //hi.EnableProxy = true;
            //hi.ProxyIP = "127.0.0.1";
            //hi.ProxyPort = 8888;
            HttpOutput ho = HttpManager.Instance.ProcessRequest(hi);

            if (ho.IsOK)
            {
                // 获得了过码接口地址
                string vcode_url = ho.TxtData;

            _rt1:
                // 请求验证码
                //hi.Url = "http://www.qixin.com/service/gtregister?t=14664817340727&_=1466481710608";
                hi.Url = string.Format("http://www.qixin.com/service/gtregister?t=={0}&_={1}", GetTimeLikeJS(), GetTimeLikeJS());
                //hi.Cookies = Settings.SimulateCookies;
                ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    //{"success":1,"gt":"68bb53762881989c3ca8e86c4621dcdb","challenge":"94490fd25aedbc2b83a843a89e2c15ad"}

                    hi.Url = vcode_url;
                    hi.PostData = "data=" + Toolslib.Str.Sub(ho.TxtData, "gt\":\"", "\"") + "|" + Toolslib.Str.Sub(ho.TxtData, "challenge\":\"", "\"");

                    Console.WriteLine("给过码接口的POST数据为:" + hi.PostData);
                    ho = HttpManager.Instance.ProcessRequest(hi);
                    string kkk = ho.TxtData;
                    if (kkk.StartsWith("success:"))
                    {
                        string[] lastvcode = kkk.Replace("success:", string.Empty).Split(new char[] { '|' });
                        //{"success":1,","momo_pic_verify_token":"44fb42329028fd1c40b66ec0a8e08375","ec":200,"em":"ok"}
                        // 登录
                        // hi.Cookies = Settings.SimulateCookies;
                        // hi.Url = "http://www.qixin.com/service/gt-validate-for-chart";
                        hi.Url = "http://www.qixin.com/service/gtvalidate";

                        // hi.Refer = "http://www.qixin.com/company/network/db4bef40-08b3-4b00-8501-fdd6e854fb84?name=%E5%8E%A6%E9%97%A8%E5%B8%82%E8%87%B3%E5%B0%9A%E4%BC%98%E5%93%81%E4%BC%A0%E5%AA%92%E6%9C%89%E9%99%90%E5%85%AC%E5%8F%B8";
                        hi.Refer = "http://www.qixin.com/search?key=%E5%8E%A6%E9%97%A8%E5%AF%8C%E5%85%B0%E5%85%8B%E6%9E%97&type=enterprise&source=&isGlobal=Y";
                        hi.PostData = "geetest_challenge=" + lastvcode[1] + "&geetest_validate=" + lastvcode[0]
                            + "&geetest_seccode=" + lastvcode[0] + "%7Cjordan&requestType=search_enterprise";

                        ho = HttpManager.Instance.ProcessRequest(hi);
                        if (ho.IsOK&&ho.TxtData.Contains("succ"))
                        {
                            return true;
                             
                        }
                    }
                    else
                    {
                        Console.WriteLine("重试");
                        goto _rt1;
                    }
                }
                else
                {
                    Console.WriteLine("重试");
                    goto _rt1;
                }
                

            }
            return false;
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
