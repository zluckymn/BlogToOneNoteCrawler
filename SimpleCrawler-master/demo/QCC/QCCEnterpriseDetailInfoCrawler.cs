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
    public class QCCEnterpriseDetailInfoCrawler : ISimpleCrawler
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
        public string DataTableAccountCookie
        {
            get { return "QCCAccountCookie"; }

        }
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public QCCEnterpriseDetailInfoCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            
            
        }

        /// <summary>
        /// 读取初始化队列
        /// </summary>
        public void InitialUrlQueue()
        {
            ///请注意改detailInfo表guid 为eGuid
            var skipCount = 1000;
            var takeCount = 10000;
            //过滤没有detailInfo的值
            var allCount = dataop.FindCount(DataTableName, Query.And(Query.NE("category", "2"),Query.NE("isUser","1"),Query.Exists("detailInfo", false)));
            Console.WriteLine("待处理个数:{0}", allCount);
            var random = new Random();
          
            if (allCount >= 10000) {
                skipCount = random.Next(1000, 100000);
                    }
            else {
                skipCount = 0;
            }
            //注意 后续需要对为空的在轮询一次因为之前可能有几个有值但被设置为空，需要过滤个人企业
            var allEnterpriseList = dataop.FindLimitByQuery(DataTableName, Query.And(Query.Exists("detailInfo", false), Query.NE("isUser", "1"), Query.NE("category", "2"), Query.NE("isUser", "1")),new MongoDB.Driver.SortByDocument(), skipCount, takeCount).SetFields("eGuid","guid").ToList();
           if (allEnterpriseList.Count() > 0)
            {
                foreach (var enterprise in allEnterpriseList)
                {
                    var key = enterprise.Text("eGuid");
                    if (string.IsNullOrEmpty(key))
                    {
                        key = enterprise.Text("guid");
                    }
                    if (key.Contains("-")) continue;
                    var backDetailInfoUrl = string.Format("http://www.qichacha.com/more_findmuhou?keyNo={0}", key);
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
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
         
            Settings.Timeout = 4500;
            Settings.LastAvaiableTokenTime= DateTime.Now;
            Settings.LastAvaiableTime = DateTime.Now;
            // string connStr = "mongodb://sa:dba@192.168.1.134/SimpleCrawler";
            MongoConnectionStringBuilder builder = new MongoConnectionStringBuilder();
            builder.Server = new MongoServerAddress("192.168.1.134", 27017);
            builder.DatabaseName = "SimpleCrawler";
            builder.Username = "sa";
            builder.Password = "dba";
            builder.SocketTimeout = new TimeSpan(00, 01, 59);
            dataop = new DataOperation(new MongoOperation(builder));
            string cookieConnStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/SimpleCrawler";
            var cookieDataop = new DataOperation(cookieConnStr, true);
            var hitCookie = cookieDataop.FindAllByQuery(DataTableAccountCookie, Query.EQ("ip", "192.168.1.134")).FirstOrDefault();
            Settings.CurWebProxy = GetWebProxy();//使用代理
            EnterpriseInfoMapDic.Add("统一社会信用代码", "credit_no");
            EnterpriseInfoMapDic.Add("组织机构代码", "org_no");
            EnterpriseInfoMapDic.Add("注册号", "reg_no");
            EnterpriseInfoMapDic.Add("经营状态", "status");
            EnterpriseInfoMapDic.Add("公司类型", "type");
            EnterpriseInfoMapDic.Add("成立日期", "date");
            EnterpriseInfoMapDic.Add("法定代表", "oper_name");
            EnterpriseInfoMapDic.Add("营业期限", "limitDate");
            EnterpriseInfoMapDic.Add("注册资本", "reg_capi_desc");
            EnterpriseInfoMapDic.Add("所属行业", "domain");
            EnterpriseInfoMapDic.Add("英文名", "engName");
            EnterpriseInfoMapDic.Add("发照日期", "issueDate");
            EnterpriseInfoMapDic.Add("登记机关", "registrar");
            EnterpriseInfoMapDic.Add("企业地址", "address");
            EnterpriseInfoMapDic.Add("经营范围", "operationDomain");
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
            geetestHelper.GetCapUrl = "http://www.qichacha.com/index_getcap?rand={0}";
            InitialUrlQueue();
             var allAccountList = dataop.FindAllByQuery(DataTableAccount,Query.And(Query.NE("status", "1"),Query.NE("isInvalid", "1"),Query.NE("isBusy", "1"))).ToList();
            var allAccountCount = allAccountList.Count();
            if (allAccountCount <= 0)
            {
                Console.WriteLine("无登陆账号可用");
                Console.ReadKey();
                return;
            }
             Console.WriteLine("初始化数据");

          
            var curRandom = new Random();
            foreach (var account in allAccountList.OrderByDescending(c=>c.Date("createDate")).ThenByDescending(c=> curRandom.Next(0, allAccountCount)))
            {
                AccountQueue.Enqueue(account);
            }
 
            Settings.RegularFilterExpressions.Add("XXXX");

          
            if (false&&SimulateLogin())
            {
                Console.WriteLine("ip登陆成功");
            }
            else
            {
                if (hitCookie != null && !string.IsNullOrEmpty(hitCookie.Text("cookie")))
                {
                    Settings.SimulateCookies = hitCookie.Text("cookie");
                    Console.WriteLine("使用134设定cookie如超时请及时更新");
                    return;

                }
                else
                {
                    Console.WriteLine("ip模拟登陆失败");
                    Environment.Exit(0);
                }
              
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
            if (args.Html.Contains("502 Bad Gateway"))
            {
                return;
            }
            //if (DateTime.Now.Hour >= 23) {//11点退出
            //    Environment.Exit(0);
            //}
            try
            {
                if (UrlQueue.Instance.Count <= Settings.ThreadCount*10)
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
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(args.Html);
                var curUpdateBson = new BsonDocument();
                var guid = GetUrlParam(args.Url, "keyNo");//获取脉络图方式;
                var infoType = GetUrlParam(args.Url, "tab");//获取脉络图方式
                if (string.IsNullOrEmpty(guid))
                {
                    guid = GetUrlParam(args.Url, "unique");
                }
                var message = string.Format("详细信息{0}获取成功剩余url{1}\r{2}", guid, UrlQueue.Instance.Count, args.Url);
                //获取企业信息http://www.qichacha.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=1b9df7af-e7b3-4d45-93ce-8acf02534adb&_=1466587526737
                if (!string.IsNullOrEmpty(guid))
                {

                    #region 基本信息
                    if (args.Url.Contains("getinfos"))
                    {
                        var companyInfo = htmlDoc.DocumentNode.SelectSingleNode("//ul[@class='company-base']");
                        if (companyInfo == null)
                        {
                            ShowMessageInfo("无数据信息" + args.Url);
                            return;
                        }
                        //公司详情
                        var liList = companyInfo.ChildNodes.Where(c => c.Name == "li").ToList();
                        foreach (var li in liList)
                        {

                            var labelText = li.InnerText;
                            var firstIndex = labelText.IndexOf("：");
                            if (firstIndex == -1) continue;
                            var value = labelText.Substring(firstIndex + 1, labelText.Length - firstIndex - 1);
                            var columnName = labelText.Substring(0, firstIndex).Replace(":", "").Trim();
                            if (EnterpriseInfoMapDic.ContainsKey(columnName))
                            {
                                curUpdateBson.Set(EnterpriseInfoMapDic[columnName], value.Trim());
                            }
                            message += labelText + " ";
                        }

                        curUpdateBson.Set("isUserUpdate", "1");
                        #endregion
                        //其他信息
                        var otherInfoList = htmlDoc.DocumentNode.SelectNodes("//section[@class='panel b-a clear']");
                        if (otherInfoList != null)
                        {
                            #region 股东信息
                            //股东
                            var shareHolder = otherInfoList.Where(c => c.InnerText.Contains("股东信息")).FirstOrDefault();
                            if (shareHolder != null)
                            {
                                var shareHolderList = new List<BsonDocument>();
                                foreach (var div in shareHolder.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                                {

                                    if (!string.IsNullOrEmpty(div.InnerText))
                                    {
                                        var curBson = new BsonDocument();
                                        curBson.Add("name", div.InnerText.Replace("\n", " ").Trim());
                                        shareHolderList.Add(curBson);
                                    }
                                }
                                if (shareHolderList.Count() > 0)
                                {
                                    curUpdateBson.Set("shareHolder", shareHolderList.ToJson());
                                }
                            }
                            #endregion
                            #region 高管
                            var holder = otherInfoList.Where(c => c.InnerText.Contains("主要人员")).FirstOrDefault();

                            if (holder != null)
                            {
                                var holderList = new List<BsonDocument>();
                                foreach (var div in holder.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                                {

                                    if (!string.IsNullOrEmpty(div.InnerText))
                                    {
                                        var curBson = new BsonDocument();
                                        var splitArray = div.InnerText.Split(new string[] { " ", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                        if (splitArray.Length >= 2)
                                        {
                                            curBson.Add("name", splitArray[0].Trim());
                                            curBson.Add("type", splitArray[1].Trim());
                                            holderList.Add(curBson);
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(splitArray[0]))
                                                curBson.Add("name", splitArray[0].Trim());
                                        }

                                    }
                                }
                                if (holderList.Count() > 0)
                                {
                                    curUpdateBson.Set("holder", holderList.ToJson());
                                }
                            }
                            #endregion
                            #region 变更记录
                            var changeRecord = otherInfoList.Where(c => c.InnerText.Contains("变更记录")).FirstOrDefault();
                            if (changeRecord != null)
                            {
                                var changeRecordList = new List<BsonDocument>();
                                foreach (var div in changeRecord.ChildNodes.Where(c => c.Attributes["class"] != null && c.Attributes["class"].Value != "panel-heading b-b"))
                                {

                                    if (!string.IsNullOrEmpty(div.InnerText))
                                    {
                                        var curBson = new BsonDocument();
                                        curBson.Set("remark", div.InnerText.Trim());
                                        changeRecordList.Add(curBson);
                                    }
                                }
                                if (changeRecordList.Count() > 0)
                                {
                                    curUpdateBson.Set("changeRecord", changeRecordList.ToJson());
                                }


                            }
                            #endregion
                        }
                    }
                    else
                    {
                        
                        if (hmtl.Contains("KeyNo")&&hmtl.Length >= 150)
                        {
                            curUpdateBson.Set("detailInfo", hmtl);
                            message += "succeed";
                            Settings.LastAvaiableTokenTime = DateTime.Now;
                        }
                        else
                        {
                            curUpdateBson.Set("detailInfo", "2");//2016.8.27更新后为2的是无信息的
                           
                        }
                    }

                    var span = (DateTime.Now - Settings.LastAvaiableTokenTime).TotalSeconds;
                    if (span >= 60)//超secceed过三十秒没有
                    {

                        //Environment.Exit(0);
                    }
                    ShowMessageInfo(message+"guid:"+guid);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Update, Query = Query.EQ("eGuid", guid) });
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
            //Settings.SimulateCookies = "pgv_pvid=1513639250; aliyungf_tc=AQAAADPRHnGrvwQAIkg9OwOqtkYJbU4N; oldFlag=1; CNZZDATA1259577625=112366950-1466409958-%7C1466415358; hide-index-popup=1; hide-download-panel=1; _alicdn_sec=576ba5f9a986fb4802dacf51bc99b1e76724f58e; connect.sid=s%3AeYWXycPKai63BYTmB9d6h-0IM_R2kp6n.EUgfW0AmJ6GB%2F0TamTi4tT53QK4OR4yQtU1I3Ba8Ryo; userKey=QXBAdmin-Web2.0_N3iUdNobAoys4M395Pk5v%2F6Zxcwjt1tiCqeSf3X3ZnI%3D; userValue=bea26f0d-e414-168a-0fe2-b8eb4278ab07; Hm_lvt_52d64b8d3f6d42a2e416d59635df3f71=1464663982,1464775028,1464776749,1465799273; Hm_lpvt_52d64b8d3f6d42a2e416d59635df3f71=1466672591";//设置cookie值
            //return true;
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
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
            return AliyunAutoLoin(userName, passWord);
             
            IPProxy ipProxy = null;
          
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

        private bool AliyunAutoLoin(string nameNormal,string pwdNormal)
        {
            HttpManager.Instance.InitWebClient(hi, true, 30, 30);
            var timeSpan = DateTime.Now - Settings.LastLoginTime;
            if (timeSpan.TotalSeconds < 5)//没限制 15秒没取到数据
            {
                return false;
            }
            
            geetestHelper = new PassGeetestHelper();
          
            if (string.IsNullOrEmpty(nameNormal) || string.IsNullOrEmpty(pwdNormal))
            {
                return false;
            }
            var validUrl = "";
            var postFormat = "";
            bool result = false;
          

            hi.Url = "http://101.200.187.122:9600/passali/fuckali.oko?uid=01161add5a3c4c55bd9c133baa9effd0&data=QNYX|login|http://www.qichacha.com/user_login";
            hi.Refer = "http://www.qichacha.com/user_login";
            var ho = HttpManager.Instance.ProcessRequest(hi);

            // this.textBox4.Text = passResult.LastPoint;
            //this.webBrowser.Refresh();
            if (ho.IsOK)
            {
                //var _url = ho.TxtData;
                // var _result= GetHttpHtml(new UrlInfo(_url), "http://www.qichacha.com/user_login");
                //if (_result.StatusCode==HttpStatusCode.OK)
                {
                    //csessionid
                    var csessionid_one = Toolslib.Str.Sub(ho.TxtData, "csessionid\":\"", "\"");
                    var sig_one = Toolslib.Str.Sub(ho.TxtData, "value\":\"", "\"");
                    var endIndex = ho.TxtData.IndexOf("|");
                    if (endIndex == -1) return false;
                    var token_one = ho.TxtData.Substring(0, endIndex);
                    // hi.Dispose();
                    //HttpManager.Instance.InitWebClient(hi, true, 30, 30);
                    // hi = new HttpInput();
                    hi.Url = "http://www.qichacha.com/user_loginaction";
                    hi.Refer = "http://www.qichacha.com/user_login";
                    // hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&geetest_challenge={2}&geetest_validate={3}&geetest_seccode={3}%7Cjordan", nameNormal, pwdNormal, passResult.Challenge, passResult.ValidCode);
                    hi.PostData = string.Format("nameNormal={0}&pwdNormal={1}&csessionid_one={2}&sig_one={3}&token_one={4}&scene_one=login", nameNormal, pwdNormal, csessionid_one, sig_one, token_one);
                    //ho = HttpManager.Instance.ProcessRequest(hi);
                    var tempResult = GetPostData(new UrlInfo(hi.Url) { PostData = hi.PostData }, hi.Refer, false);
                    if (tempResult.StatusCode == HttpStatusCode.OK)
                    {
                        if (tempResult.Html.Contains("true"))
                        {
                         
                            Settings.SimulateCookies = tempResult.Cookie;
                            ShowMessageInfo("过验证码模拟登陆成功");
                            Settings.LastLoginTime = DateTime.Now;
                         
                            return true;
                        }
                    }
                  
                   
                }
            }
       
            return false;
        }

        private bool SimulateLoginEx()
        {
          
            // var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan&requestType=search_enterprise";
            var postFormat = "geetest_challenge={0}&geetest_validate={1}&geetest_seccode={1}%7Cjordan";
            var validUrl = "http://www.QCC.com/service/gt-validate-for-chart";
            var passResult = geetestHelper.PassGeetest(hi, postFormat, validUrl);
            //在查看一遍防止无线过点
            var item = new HttpItem()
            {
                URL = "http://www.QCC.com/service/getRootNodeInfoByEnterpriseId?enterpriseId=a4001398-6739-4941-a031-5f3cbea5459f&_=1470156562958",//URL     必需项    
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
    }

}
