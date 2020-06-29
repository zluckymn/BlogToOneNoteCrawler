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
    public class RegisterEnterpriseAddInfoCrawler : ISimpleCrawler
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
        private   BloomFilter<string>  filter;
        private BloomFilter<string> pageUrlfilter;
        private   BloomFilter<string> urlfilter;
        private const string _DataTableName = "RegisterEnterpriseDetailInfo";//存储的数据库表明
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
        public string DataTableNamePageURL
        {
            get { return _DataTableName + "PageURL"; }

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
        public string DataTableAccountCookie
        {
            get { return "QCCAccountCookie"; }

        }

        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public RegisterEnterpriseAddInfoCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;

              pageUrlfilter=new BloomFilter<string>(2000000);
              urlfilter = new BloomFilter<string>(2000000);
        }

        /// <summary>
        /// 读取初始化队列
        /// </summary>
        private bool ExistName(string name)
        {
            return dataop.FindCount(DataTableName, Query.EQ("name", name)) > 0;
        }

        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
         
            Settings.Timeout = 4500;
            Settings.LastAvaiableTokenTime= DateTime.Now;
            Settings.LastAvaiableTime = DateTime.Now;
            Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.Accept = "text/html, application/xhtml+xml, */*";

            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式

            //SimulateLogin();
            //return;
            this.Settings.Timeout = 6000;
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount =1;
            //Settings.AutoSpeedLimit = true;
            Settings.DBSaveCountLimit = 1;
            Settings.IgnoreFailUrl = true;//失败数据不进行继续爬取，多几次就好，因为有些数据获取的为空与无登陆一样无法判断
            #region 初始化
            Settings.UseSuperWebClient = true;
            LibCurlNet.HttpInput hi = new LibCurlNet.HttpInput();
            LibCurlNet.HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
            hi.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            hi.HeaderSet("Content-Type", "application/x-www-form-urlencoded");
            hi.HeaderSet("Accept", "text/html, application/xhtml+xml, */*");
            hi.HeaderSet("Accept-Language", "zh-CN");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            hi.HeaderSet("Accept-Encoding", "gzip");
            hi.HeaderSet("Host", "gzhd.saic.gov.cn");
            hi.Cookies = "yunsuo_session_verify=760b6fed201cabba9dbbc08d6ee95433; yoursessionname1=217703E3DF30F6B367DEC0B3B4EBD13F; yoursessionname0=2ACF87288AD50420E1507A8F20B69186";
            hi.Refer = "http://gzhd.saic.gov.cn/saicsearch/qyjindex.jsp";
            Settings.hi = hi;
           
            var allEnterpriseUrl = dataop.FindAllByQuery(DataTableNameURL,Query.NE("status","1")).SetFields("url").ToList();
            foreach (var urlObj in allEnterpriseUrl)
            {
                if (!urlfilter.Contains(urlObj.Text("url")))
                {
                    urlfilter.Add(urlObj.Text("url"));
                }
            }
            
           // return ho.TxtData;

            #endregion
            Settings.RegularFilterExpressions.Add("XXXX");

             UrlQueue.Instance.EnQueue(new UrlInfo("http://gzhd.saic.gov.cn/saicsearch/qyjindex.jsp") { Depth = 1, PostData = string.Format("date=&end_date=&title=&content=&key=%E6%A0%B8%E5%87%86%E5%85%AC%E5%91%8A&database=qyj&search_field=all&search_type=yes&page={0}", "1") });
            //UrlQueue.Instance.EnQueue(new UrlInfo("http://qyj.saic.gov.cn/ggxx/201005/t20100511_84621.html") { Depth = 1, PostData=""});
       
            if (SimulateLogin())
            {
                Console.WriteLine("ip登陆成功");
            }
            else
            {
                Console.WriteLine("ip模拟登陆失败");
                    Environment.Exit(0);
        
              
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
       // DateTime lasFitchDate = DateTime.Now;
        
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            try
            {
                var curPage = string.Empty;
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(args.Html);
                if (!args.Url.Contains("qyj.saic.gov.cn/ggxx"))
                {
                 
                    var searchPageDiv = htmlDoc.GetElementbyId("search_page");
                    var allPage = 0;
                    if (searchPageDiv != null && searchPageDiv.ParentNode != null)
                    {
                        var pageSize = Toolslib.Str.Sub(searchPageDiv.ParentNode.InnerText, "共", "页");
                        curPage = Toolslib.Str.Sub(searchPageDiv.ParentNode.InnerText, "当前第", "页");
                        if (!int.TryParse(pageSize, out allPage))
                        {
                            allPage = 212;
                        }
                    }
                    if (curPage == "1")
                    {
                        for (var index = 2; index <= allPage; index++)
                        {
                           
                            UrlQueue.Instance.EnQueue(new UrlInfo("http://gzhd.saic.gov.cn/saicsearch/qyjindex.jsp") { Depth = 1, PostData = string.Format("date=&end_date=&title=&content=&key=%E6%A0%B8%E5%87%86%E5%85%AC%E5%91%8A&database=qyj&search_field=all&search_type=yes&page={0}", index) });
                        }
                        //添加到待爬取队列
                    }
                    ///获取url列表
                    ///
                    var searchResultDiv = htmlDoc.GetElementbyId("documentContainer");
                    if (searchResultDiv == null) return;
                    var searchResultList = searchResultDiv.SelectNodes("./div/a");
                    if (searchResultList == null) return;
                    //http://qyj.saic.gov.cn/ggxx/ 规则匹配
                    foreach (var aNode in searchResultList)
                    {
                        if (aNode.Attributes["href"] != null && aNode.Attributes["href"].Value.Contains("qyj.saic.gov.cn/ggxx/"))
                        {
                            //详细信息页面
                            UrlQueue.Instance.EnQueue(new UrlInfo(aNode.Attributes["href"].Value) { Depth = 1 });
                            if (!urlfilter.Contains(aNode.Attributes["href"].Value)) { 
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("url", aNode.Attributes["href"].Value), Name = DataTableNameURL, Type = StorageType.Insert });
                                urlfilter.Add(aNode.Attributes["href"].Value);
                            }
                        }
                    }
                    Console.Write("page:{0} count:{1} ", curPage, searchResultList.Count());
                }
                else//数据处理
                {
                    var searchResultDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='TRS_Editor']");
                    if (searchResultDiv == null)
                    {
                        searchResultDiv = htmlDoc.GetElementbyId("zoom");
                    }
                    if (searchResultDiv != null)
                    {
                        try
                        {
                            if (searchResultDiv.ChildNodes.Count() > 0&& searchResultDiv.InnerText.Contains("style"))
                            {
                                searchResultDiv.FirstChild.Remove();

                            }
                        }
                        catch (Exception ex)
                        {
                            ShowMessageInfo(ex.Message + "searchResultDiv.FirstChild.Remove");
                        }
                        //2016-10-13&nbsp;(国)登记内名预核字[2016]第11223号&nbsp;新星联盟影业有限公司&nbsp;&nbsp;<br>
                        //2016-10-13&nbsp;(国)登记内名预核字[2016]第11421号&nbsp;鸿庆楼博物馆有限公司&nbsp;&nbsp;<br>
                        var result = searchResultDiv.InnerText;
                        var splitArray = result.Split(new string[] { "<br>", "\n", "\r", "&nbsp;&nbsp;" }, StringSplitOptions.RemoveEmptyEntries);
                        var error = false;
                        foreach (var enterpriseInfo in splitArray)
                        {
                            var newBsonDoc = new BsonDocument();
                            var columnInfoArr = enterpriseInfo.Split(new string[] { "&nbsp;" }, StringSplitOptions.RemoveEmptyEntries);
                            if (columnInfoArr.Length == 3)
                            {
                                var date = columnInfoArr[0];
                                var info = columnInfoArr[1];
                                var name = columnInfoArr[2];
                                ShowMessageInfo(String.Format("{0}{1}{2}\n\r", date, info, name));
                                newBsonDoc.Add("name", name.Trim());
                                newBsonDoc.Add("info", info.Trim());
                                newBsonDoc.Add("date", date.Trim());
                                newBsonDoc.Add("url", args.Url);
                                if (!ExistName(name))
                                {
                                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = newBsonDoc, Name = DataTableName, Type = StorageType.Insert });

                                }
                            }
                            else {
                                error = true;
                            }

                        }
                        if (!error)
                        {
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Query = Query.EQ("url", args.Url), Name = DataTableNameURL, Type = StorageType.Update });
                        }
                        else {
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1").Add("error","1"), Query = Query.EQ("url", args.Url), Name = DataTableNameURL, Type = StorageType.Update });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

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
            var avaiableCount = Settings.IPProxyList!=null? Settings.IPProxyList.Where(c => c.Unavaiable == false).Count():0;

            if (args.Html.Length<=100&&args.Html.Contains("您使用验证码过于频繁")|| args.Html.Contains("请求的网址（URL）无法获取")|| args.Html.Contains("上限"))
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
            try
            {
                if (args.Exception!=null&&(args.Exception.Message.Contains("超时")|| args.Exception.Message.Contains("连接尝试失败")))
                {
                    var guid = GetUrlParam(args.Url, "keyNo");//获取脉络图方式;
                    if (string.IsNullOrEmpty(guid))
                    {
                        guid = GetUrlParam(args.Url, "unique");
                    }
                    var curUpdateBson = new BsonDocument().Add("detailInfo", "2").Add("isTimeOut", "1");
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update, Query = Query.EQ("eGuid", guid) });
                    Console.WriteLine(string.Format("发生超时操作:{0}{1}", args.Exception.Message,args.Url));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("进行错误处理时候发生了如下错误:{0}{1}", ex.Message));
            }

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
            //return true;
            Settings.SimulateCookies = "yunsuo_session_verify=760b6fed201cabba9dbbc08d6ee95433; yoursessionname1=217703E3DF30F6B367DEC0B3B4EBD13F; yoursessionname0=2ACF87288AD50420E1507A8F20B69186";//设置cookie值
             return true;
#pragma warning disable CS0162 // 检测到无法访问的代码
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
#pragma warning restore CS0162 // 检测到无法访问的代码
            {
                return true;
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
           // return AliyunAutoLoin(userName, passWord);
             
#pragma warning disable CS0219 // 变量“ipProxy”已被赋值，但从未使用过它的值
            IPProxy ipProxy = null;
#pragma warning restore CS0219 // 变量“ipProxy”已被赋值，但从未使用过它的值
          
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            Random rand = new Random(Environment.TickCount);

            var nameNormal = userName;
            var pwdNormal = passWord;
            if (string.IsNullOrEmpty(nameNormal) || string.IsNullOrEmpty(pwdNormal))
            {
                return false;
            }
            var validUrl = "";
            var postFormat = "";
            bool result = false;
          
            // var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&requestType=search_enterprise";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl, Settings.SimulateCookies);
            result = passResult.Status;
            // this.richTextBoxInfo.Document.Blocks.Clear();
            
            //this.webBrowser.Refresh();

            if (passResult.Status)
            {

                hi.Url = "http://www.qichacha.com/user_loginaction";
                hi.Refer = "http://www.qichacha.com/user_login";
                hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan", nameNormal, pwdNormal, passResult.Challenge, passResult.ValidCode);
                var ho = HttpManager.Instance.ProcessRequest(hi);
                if (ho.IsOK)
                {
                    if (ho.TxtData.Contains("true"))
                    {
                        Settings.SimulateCookies = ho.Cookies;
                        Console.WriteLine("过验证码模拟登陆成功");
                  
                        return true;
                    }
                }
                var resultText = geetestHelper.GetLastPoint(hi);
                Console.WriteLine(resultText);

            }
            return false;
        }

        public HttpResult GetPostData(UrlInfo curUrlObj, string refer = "", bool useProxy = true)
        {
           
            //创建Httphelper参数对象
            HttpItem item = new HttpItem()
            {
                URL = curUrlObj.UrlString,//URL     必需项    

                ContentType = "application/x-www-form-urlencoded; charset=UTF-8",//返回类型    可选项有默认值 

                Timeout = 1500,
                Accept = "*/*",
                Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                                //Encoding = Encoding.Default,
                Method = "post",//URL     可选项 默认为Get
                                //Timeout = 100000,//连接超时时间     可选项默认为100000
                                //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                //Cookie = "",//字符串Cookie     可选项
                UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko",//用户的浏览器类型，版本，操作系统     可选项有默认值
                Referer = "http://www.qichacha.com/",//来源URL     可选项
                Postdata = curUrlObj.PostData,
                Allowautoredirect = true,
                Cookie = Settings.SimulateCookies
            };
            item.PostEncoding = System.Text.Encoding.GetEncoding("utf-8");
            if (!string.IsNullOrEmpty(refer))
            {
                item.Referer = refer;
            }
          

            //item.Header.Add("Accept-Encoding", "gzip, deflate");
            //item.Header.Add("Accept-Language", "zh-CN");
            //item.Header.Add("charset", "UTF-8");
            //item.Header.Add("X-Requested-With", "XMLHttpRequest");
            //请求的返回值对象
            var result = http.GetHtml(item);
            return result;
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
