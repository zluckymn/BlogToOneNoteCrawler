using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Yinhe.ProcessingCenter;

using Yinhe.ProcessingCenter.DataRule;
using System.Collections;
using Newtonsoft.Json.Linq;
using LibCurlNet;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 慧聪网络App 数据采集
    /// 类别 var url = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0";
    ///  authoration D1A4976615B875529F63090417A286C9";
    /// http://openapi.m.hc360.com/openapi/v1/productDetail/getSameProduct/621618969?page=1&pagesize=9相似产品
    /// http://openapi.m.hc360.com/openapi/v1/company/getInfo/wbz8 供应商信息 联系人电话所在城市
    /// http://detail.b2b.hc360.com/detail/turbine/template/moblie,vmoblie,getcredit_files.html?username=wxjinpeng&callback=jQuery17109421258288211527_1504584128563&_=1504584128681
    ///{"companyName":"宜兴市金鹏印铁包装制品有限公司","creditTitle":"信用档案","memberInfo":{"age":"11","creditStar":"5","level":"银牌会员",
    /// "levelSign":"6","mmtIndex":"381"},"memberTypeId":"6","mmtAge":"11","mmtlevel":"银牌会员","nodeIp":"10.7.3.28","registerInfo":
    /// {"certification":"邓白氏","certificationField":[{"name":"经营地址","value":"江苏省宜兴市万石镇工业园区"},{"name":"成立时间","value":"2001-04-18"},
    /// {"name":"邓氏编码","value":"545537912"},{"name":"联 系 人","value":"周建华"},{"name":"部门职位","value":"总经理"},
    /// {"name":"认证时间","value":"2006年10月16日"}]},"userid":"2783201"})
    ///  http://wxjinpeng.b2b.hc360.com/shop/mmtdocs.html  买卖通会员档案信息
    ///  </summary>
    public class HuiCongMaterialDetailAPPCrawler : ISimpleCrawler
    {

       
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> companyGuidFilter;
        private BloomFilter<string> materialGuidFilter;
        private   string _DataTableName = "Material_HuiCong_WLM";//存储的数据库表明
      

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }

        }
        /// <summary>
        /// 详细表
        /// </summary>
        public string DataTableNameDetail
        {
            get { return "MaterialDetail_HuiCong_WLM"; }

        }
        /// <summary>
        /// 详细表
        /// </summary>
        public string DataTableNameCompany
        {
            get { return "MaterialCompany_HuiCong"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameURL
        {
            get { return _DataTableName + "URL"; }

        }
        public string DataTableNameSpecialURL
        {
            get { return _DataTableName + "SpecialURL"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_MT"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameCityCategory
        {
            get { return "CityCategoryInfo_MT"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        ///// <summary>
        /////  分类信息
        ///// </summary>
        //public string DataTableNameCategory
        //{
        //    get { return "CategoryInfo_MT"; }

        //}

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public HuiCongMaterialDetailAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            companyGuidFilter = new BloomFilter<string>(9000000);
            materialGuidFilter= new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;

        string proxyHost = "http://proxy.abuyun.com";
        string proxyPort = "9010";
        // 代理隧道验证信息
        //string proxyUser = "H1880S335RB41F8P";
        //string proxyPass = "ECB2CD5B9D783F4E";
        string proxyUser = "H1538UM3D6R2133P";//"H1880S335RB41F8P";////HVW8J9B1F7K4W83P
        string proxyPass = "511AF06ABED1E7AE";//"ECB2CD5B9D783F4E";////C835A336CD070F9D
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", proxyHost, proxyPort));
            proxy.Credentials = new NetworkCredential(proxyUser, proxyPass);
            return proxy;
        }
        public string GetWebProxyString()
        {
           return string.Format("{0}:{1}@{2}:{3}", proxyUser, proxyPass, proxyHost.Replace("http://",""), proxyPort);
        }

        public string GetCurrentIPProxyString()
        {
            var curProxy = Settings.GetIPProxy();
            if (curProxy != null)
            {
                var ipProxyString = string.Format("{0}:{1}", curProxy.IP, curProxy.Port);
                Console.WriteLine("当前:{0}", ipProxyString);
                return ipProxyString;
            }
            else
            {
                return string.Empty;
            }
        
        }

        int pageSize = 100;//24
        int pageBeginNum = 1;
        string materialUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w={0}&v=59&e={1}&c=供应信息&n={2}&m=2&H=1&bt=0";
        HuiCongAppHelper appHelper = new HuiCongAppHelper();

        /// <summary>
        /// 初始化url每次取1000
        /// </summary>
        public void InitialUrlQueue()
        {
            var skipCount = 1000;
            var takeCount = 1000;
            var query = Query.And( Query.NE("isUpdate", "2"), Query.NE("isEmpty", "1"));
            //过滤没有detailInfo的值
            var allCount = dataop.FindCount(DataTableName, query);
            Console.WriteLine("待处理个数:{0}", allCount);
            var random = new Random();

            if (allCount > 1000)
            {
                skipCount = random.Next(1000, allCount);
            }
            else
            {
                skipCount = 0;
            }
            //注意 后续需要对为空的在轮询一次因为之前可能有几个有值但被设置为空，需要过滤个人企业
            var materialList = dataop.FindLimitByQuery(DataTableName, query, new MongoDB.Driver.SortByDocument(), skipCount, takeCount).SetFields("searchResultfoId").ToList();
           
           // var materialList = dataop.FindAllByQuery(_DataTableName, Query.NE("isUpdate", "2")).SetFields("searchResultfoId").Take(10000).ToList();
            foreach (var material in materialList)
            {
                var url = "http://openapi.m.hc360.com/openapi/v1/productDetail/getProductDetail/" + material["searchResultfoId"].AsString;
                var authorizationCode = appHelper.GetHuiCongAuthorizationCode(url);
                UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1, Authorization = authorizationCode });
            }
            
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {

            //种子地址需要加布隆过滤
            //Console.WriteLine("开始获取代理iP");
            ////Settings.Depth = 4;
            ////代理ip模式
            //Settings.IPProxyList =IPProxyHelper.GetIpProxyList("0");//获取代理ip列表
            //Console.WriteLine("获得ip:{0}", Settings.IPProxyList.Count);
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount =1;
            Settings.DBSaveCountLimit = 1;
            //Settings.UseSuperWebClient = true;
            Settings.MaxReTryTimes = 10;
            Settings.IgnoreFailUrl = true;
            Settings.CrawlerClassName = "HuiCongMaterial";//需要进行token替换
            // Settings.AutoSpeedLimit = true;
            // Settings.AutoSpeedLimitMaxMSecond = 1000;
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded";
            this.Settings.UserAgent = "AiMeiTuan /samsung-4.4.2-GT-I9300-900x1440-320-5.5.4-254-864394010401414-qqcpd";
            //Settings.hi = new HttpInput();
            //HttpManager.Instance.InitWebClient(Settings.hi, true, 30, 30);
            Console.WriteLine("是否使用代理 0不使用 其他使用");
            var useWebProxy = Console.ReadLine();
            if (useWebProxy != "0")
            {
                // Settings.hi.CurlObject.SetOpt(LibCurlNet.CURLoption.CURLOPT_PROXY, GetWebProxyString());
                Settings.CurWebProxy = GetWebProxy();
            }
            var headSetDic = new Dictionary<string, string>();
            // Settings.hi.HeaderSet("Authorization", authorizationCode);
            headSetDic.Add("If-Modified-Since", "0");
            //headSetDic.Add("User-Agent", "56");
            //headSetDic.Add("Host", "z.hc360.com");
            //headSetDic.Add("Content-Type", "text/html;charset=gb2312");
            //Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2
            Settings.ContentType = "text/html;charset=gb2312";
            Settings.UserAgent = "56";
            InitialUrlQueue();
            Console.WriteLine("开始获取已存在公司");
            //公司guid获取
            var existCompanyList = dataop.FindFieldsByQuery(DataTableNameCompany, Query.Exists("username", true), new List<string> { "username" }).ToList();
            foreach(var company in existCompanyList)
            {
                if (!companyGuidFilter.Contains(company.Text("username")))
                {
                    companyGuidFilter.Add(company.Text("username"));
                }
            }
            Console.WriteLine("当前公司:{0}", existCompanyList.Count());
            Console.WriteLine("开始获取已存在材料");
            //var existMaterialDetailList = dataop.fin(DataTableNameDetail, Query.EQ("searchResultfoId", true), new List<string> { "searchResultfoId" }).ToList();
            //foreach (var material in existMaterialDetailList)
            //{
            //    if (!materialGuidFilter.Contains(material.Text("searchResultfoId")))
            //    {
            //        materialGuidFilter.Add(material.Text("searchResultfoId"));
            //    }
            //}
           // Console.WriteLine("当前材料:{0}", existMaterialDetailList.Count());


            //var testUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=外墙面砖&v=59&e=100&c=供应信息&n=3101&m=2&H=1&bt=0";
            //var testAuthorization = appHelper.GetHuiCongAuthorizationCode(testUrl);
            //UrlQueue.Instance.EnQueue(new UrlInfo(testUrl) { Depth = 1, Authorization = testAuthorization });
            Console.WriteLine("正在加载账号数据");
           //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的

            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            ////是否guid
            ///不进行地址爬取
            Settings.RegularFilterExpressions.Add(@"luckymnXXXXXXXXXXXXXXXXXX");

            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("模拟登陆失败");
            }

        }

        private BsonDocument GetObj(string guid)
        {
            return dataop.FindOneByQuery(DataTableName, Query.EQ("guid", guid));
        }
        private bool hasExistObjDetail(string guid)
        {
            return dataop.FindCount(DataTableNameDetail, Query.EQ("searchResultfoId", guid)) > 0;
        }

        private bool hasExistObj(string tableName,string guidName,string guid)
        {
            return dataop.FindCount(tableName, Query.EQ(guidName, guid)) > 0;
        }

        private BsonDocument GetExistObj(string tableName, string guidName, string guid)
        {
            return dataop.FindAllByQuery(tableName, Query.EQ(guidName, guid)).SetFields("guid","name","userName").FirstOrDefault();
        }
        private string TrimStr(string str)
        {
            return str.Replace(" ", "").Replace("\"", "").Trim();
        }

        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
          
            try
            {
                if (UrlQueue.Instance.Count <= Settings.ThreadCount * 10+50)
                {
                    if ((DateTime.Now - Settings.LastAvaiableTime).TotalSeconds >= 60)
                    {
                        Console.WriteLine("url剩余少于40");
                        Settings.LastAvaiableTime = DateTime.Now;
                        Console.WriteLine("开始获取url");
                        InitialUrlQueue();

                    }
                }

                var searchResultfoId = GetGuid(args.Url);
                if (string.IsNullOrEmpty(searchResultfoId))
                {
                    Console.WriteLine("searchResultfoId不存在{0}", args.Url);
                    return;
                }
                if (string.IsNullOrEmpty(args.Html))
                {
                    var updateBson = new BsonDocument().Add("isUpdate", "2").Add("isEmpty", "1");
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = updateBson, Type = StorageType.Update, Query = Query.EQ("searchResultfoId", searchResultfoId) });
                    return;
                }
                 

                 var materialDetail = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(args.Html);

                //保存公司信息MaterialCompany_HuiCong
                var companyBson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(materialDetail["companyInfo"].AsBsonDocument);
                var companyGuid = string.Empty;
                if (!companyGuidFilter.Contains(companyBson["username"].AsString) && !hasExistObj(DataTableNameCompany, "username", companyBson["username"].AsString))
                {
                    var uuidN = Guid.NewGuid().ToString("N");
                    companyBson.Set("guid", uuidN);
                    var companyInfo = companyBson;
                    companyGuid = uuidN;
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableNameCompany, Document = companyInfo, Type = StorageType.Insert });
                    Console.WriteLine("添加公司{0}", companyBson["username"].AsString);
                }
                else
                {
                    var existCompanyBson = GetExistObj(DataTableNameCompany, "username", companyBson["username"].AsString);
                    if (existCompanyBson != null)
                    {
                        companyGuid = existCompanyBson.Text("guid");
                    }
                    else
                    {
                        Console.Write("公司不存在:{0}", companyBson["username"].AsString);
                    }
                }
                if (!materialGuidFilter.Contains(searchResultfoId)&&!hasExistObjDetail(searchResultfoId))
                {
                    //bapcun回写材料公司信息MaterialDetail_HuiCong
                    materialDetail.Set("companyGuid", companyGuid);
                    materialDetail.Set("name", companyBson["name"].AsString);//公司名
                    materialDetail.Set("username", companyBson["username"].AsString);
                    materialDetail.Set("searchResultfoId", searchResultfoId);
                    materialGuidFilter.Add(searchResultfoId);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableNameDetail, Document = materialDetail, Type = StorageType.Insert });
                }
                else
                {
                    Console.Write("当前材料已存在:{0}", searchResultfoId);
                }
                var updateMaterialDoc = new BsonDocument().Add("isUpdate", "2").Add("noImg", "1").Add("isNew", "1");
                DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = updateMaterialDoc, Type = StorageType.Update, Query = Query.EQ("searchResultfoId", searchResultfoId) });
                Console.WriteLine("更新{0}成功剩余：{1}", searchResultfoId,UrlQueue.Instance.Count);

                //采集图片
                //var product = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(materialDetail["product"].AsBsonDocument);
                //var picAddrs = product["picpath"].AsBsonArray;
                //var picCount = picAddrs.Count();
                //var companyGuid = companyBson["guid"].AsString;
                #region 图片下载 暂时先不下载
                //for (var i = 0; i < picCount; i++)
                //{
                //    var picAddr = picAddrs[i].AsString;
                //    var picName = searchResultfoId + "_" + i.ToString() + "." + picAddrs[i].AsString.Split('/')[picAddrs[i].AsString.Split('/').Count() - 1].Split('.')[1];
                //    try
                //    {
                //        var curDirPath = System.AppDomain.CurrentDomain.BaseDirectory;
                //        var curDir = new System.IO.DirectoryInfo(curDirPath);

                //        curDir = new System.IO.DirectoryInfo(curDir + @"\HuiCongImages\" + companyGuid);
                //        //var filePath = System.Web.HttpContext.Current.Server.MapPath(@"\HuiCongImages\" + companyGuid);
                //        if (!curDir.Exists)//如果不存在就创建file文件夹
                //        {
                //            curDir.Create();
                //        }

                //        using (System.Net.WebClient wc = new System.Net.WebClient())
                //        {
                //            wc.Headers.Add("User-Agent", "Chrome");
                //            //wc.Proxy = GetWebProxy();
                //            wc.DownloadFile(picAddr, curDir + @"\" + picName);
                //        }

                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine(ex.Message);
                //    }
                //}
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private void InitNextUrl(string keyWord,int allRecordCount,int curRecordIndex, int pageSize)
        {
            /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=24&c=供应信息&n=1&m=2&H=1&bt=0
            while (curRecordIndex < allRecordCount)
           {
                curRecordIndex = curRecordIndex + pageSize;
                var curUrl = string.Format(materialUrl, keyWord, pageSize, curRecordIndex);
                var authorization = appHelper.GetHuiCongAuthorizationCode(curUrl);
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, Authorization = authorization });
                //return;
            }
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
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetGuid(string url)
        {
            var queryStrIndex = url.LastIndexOf("/");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
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


        public string GetXYValue(int startIndex, int allLength, string html)
        {
            var hitResult = new StringBuilder();
            if (startIndex >= allLength) return string.Empty;
            var curChart = html[++startIndex];
            while (curChart != '"')
            {
                hitResult.AppendFormat(curChart.ToString());
                if (++startIndex < allLength)
                {
                    curChart = html[startIndex];
                }
                else
                {
                    break;
                }
            }
            return hitResult.ToString();
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
            var html = args.Html;
            //if (string.IsNullOrEmpty(html))
            //{
            //    return true;
            //}
            if (html.Contains("Object moved")|| html.Contains("Service Unavailable") )//需要编写被限定IP的处理
            {
                return true;
            }

            if (!html.Contains("It is not legal"))
            {
                 return false;
              
            }
            
            return true;
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
            if (Settings.LandFangIUserId == 0)
            {
                var hitAccount = dataop.FindOneByQuery(DataTableNameAccount, Query.EQ("userName", "savegod523"));
                if (hitAccount != null)
                {
                    Settings.LandFangIUserId = hitAccount.Int("LandFangIUserId");
                }
                if (Settings.LandFangIUserId == 0)
                {
                    Settings.LandFangIUserId = 42638;//初始化
                }
            }
            // Settings.LandFangIUserId = Settings.LandFangIUserId + 1;
            Settings.LandFangIUserId = new Random().Next(3333, 143630);
            Settings.MaxAccountCrawlerCount = new Random().Next(50,200);
            DBChangeQueue.Instance.EnQueue(new StorageData()
            {
                Name = DataTableNameAccount,
                Document = new BsonDocument().Add("LandFangIUserId", Settings.LandFangIUserId.ToString()),
                Query = Query.EQ("userName", "savegod523"), Type=StorageType.Update
            });
            StartDBChangeProcess();
            return true;
             
        }

        /// <summary>
        /// ip无效处理
        /// </summary>
        private void IPInvalidProcess(IPProxy ipproxy)
        {
           
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
