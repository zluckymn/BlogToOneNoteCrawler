using DotNet.Utilities;
using HtmlAgilityPack;
using LibCurlNet;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
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
    public class QCCEnterpriseCrawler_FOSHAN : ISimpleCrawler
    {

         

        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        Queue<BsonDocument> AccountQueue=new  Queue<BsonDocument>();
        HttpInput hi = new HttpInput();
      
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> idFilter=new BloomFilter<string>(40000000);
        private const string _DataTableName = "QCCEnterprise_FoShan";//存储的数据库表明
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
            get { return "QCCEnterprise_FoShan"; }

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
        public QCCEnterpriseCrawler_FOSHAN(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
           
            
        }

        /// <summary>
        /// 读取初始化队列
        /// </summary>
        public void InitialUrlQueue()
        {
            //var maxPage = 679;//3393
            //var pageSize = 500;//100
            var maxPage = 340;//
            var pageSize = 1000;//100
            for (var page=1;page<= maxPage; page++)
            {

                    var backDetailInfoUrl = string.Format("http://119.145.135.173:8080/newFsCredit/ajax.do?method=ajaxMapQueryCompany&callback=jsonp1471495254477&_=1471495261148&regType=0&page={0}&pagesize={1}&submittype=reg", page, pageSize);
                if (!filter.Contains(backDetailInfoUrl)) { 
                    UrlQueue.Instance.EnQueue(new UrlInfo(backDetailInfoUrl) { Depth = 1 });
                }
            }

           
        }

        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
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
            Settings.ThreadCount =2;
            //Settings.AutoSpeedLimit = true;
            Settings.DBSaveCountLimit = 50;
           // Settings.IgnoreFailUrl = true;//失败数据不进行继续爬取，多几次就好，因为有些数据获取的为空与无登陆一样无法判断
            geetestHelper.GetCapUrl = "http://www.qichacha.com/index_getcap?rand={0}";
            var allEnterpriseList = dataop.FindAll(DataTableName).SetFields("id").ToList();
            foreach (var enterprise in allEnterpriseList)
            {
                if(!idFilter.Contains(enterprise.Text("id")))
                idFilter.Add(enterprise.Text("id"));
            }
            var allUrlList = dataop.FindAllByQuery(DataTableNameURL,Query.EQ("succeed","1")).SetFields("url").ToList();
            foreach (var urlObj in allUrlList)
            {
                if (!filter.Contains(urlObj.Text("url")))
                    filter.Add(urlObj.Text("url"));
            }
            
            InitialUrlQueue();
            
             Console.WriteLine("初始化数据");

          
            var curRandom = new Random();
            //foreach (var account in allAccountList.OrderByDescending(c=>c.Date("createDate")).ThenByDescending(c=> curRandom.Next(0, allAccountCount)))
            //{
            //    AccountQueue.Enqueue(account);
            //}
 
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
            try
            {
                var hmtl = args.Html;
                var beginIndex = hmtl.IndexOf("{");
                var endIndex = hmtl.LastIndexOf("}");
                if (beginIndex == -1 || endIndex == -1) return;
                var fixHtml = hmtl.Substring(beginIndex, endIndex - beginIndex + 1);
                fixHtml = fixHtml.Replace("{\"", "{|H|").Replace("\"}", "|H|}").Replace("\":\"", "|H|:|H|").Replace("\",\"", "|H|,|H|");
                fixHtml = fixHtml.Replace("success\"", "success|H|");
                fixHtml = fixHtml.Replace("\"list\"", "|H|list|H|");
                fixHtml = fixHtml.Replace("\"len\"", "|H|len|H|");
                fixHtml = fixHtml.Replace("\"page\"", "|H|page|H|");
                fixHtml = fixHtml.Replace("\"", "|").Replace("\\","|");
                
                fixHtml = fixHtml.Replace("|H|", "\"");
                JObject jsonObj = JObject.Parse(fixHtml);
                var dataInfo = jsonObj["list"];
                if (dataInfo == null) return;
                foreach (var enterprise in dataInfo)
                {
                    var id = GetString(enterprise, "id");
                    if (idFilter.Contains(id))
                    {
                        continue;
                    }
                    var curUpdateBson = new BsonDocument();
                    var name = GetString(enterprise, "title");
                    var la = GetString(enterprise, "la");
                    var type = GetString(enterprise, "type");
                    var address = GetString(enterprise, "address");
                    var zczbbz = GetString(enterprise, "zczbbz");
                    var qylx = GetString(enterprise, "qylx");
                    var fddbr = GetString(enterprise, "fddbr");
                    var qydm = GetString(enterprise, "qydm");
                    var cid = GetString(enterprise, "cid");
                    var rel = GetString(enterprise, "rel");
                    curUpdateBson.Add("name", name);
                    curUpdateBson.Add("la", la);
                    curUpdateBson.Add("type", type);
                    curUpdateBson.Add("address", address);
                    curUpdateBson.Add("zczbbz", zczbbz);
                    curUpdateBson.Add("qylx", qylx);
                    curUpdateBson.Add("id", id);
                    curUpdateBson.Add("fddbr", fddbr);
                    curUpdateBson.Add("qydm", qydm);
                    curUpdateBson.Add("cid", cid);
                    curUpdateBson.Add("rel", rel);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curUpdateBson, Name = DataTableName, Type = StorageType.Insert });

                }
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("url", args.Url).Add("succeed", "1"), Name = DataTableNameURL, Type = StorageType.Insert });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("url", args.Url), Name = DataTableNameURL, Type = StorageType.Insert });
            }
       }
        public string GetString(JToken node, string columnName)
        {


            if (node != null && node.ToString().Contains(string.Format("\"{0}\":", columnName)))
            {
                return node[columnName].ToString();
            }
            else
            {
                return string.Empty;
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
             return true;
            
            // return SimulateLoginEx();
          
#pragma warning disable CS0162 // 检测到无法访问的代码
            var userName = string.Empty;
#pragma warning restore CS0162 // 检测到无法访问的代码
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
