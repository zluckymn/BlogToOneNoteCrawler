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
using System.Security.Cryptography;

namespace SimpleCrawler.Demo
{
    ///
    /// 
    ///  </summary>
    public class WenShuAPPCrawler : ISimpleCrawler
    {

       // private   string connStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/SimpleCrawler";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
         
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> guidFilter;
        private   string _DataTableName = "WenShuCollection";//存储的数据库表明
       

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
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameCourt
        {
            get { return "wenshuCourt"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameReason
        {
            get { return "WenShuReason"; }

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
        public WenShuAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public WebProxy GetWebProxy()
        {
            // 设置代理服务器
            var proxy = new WebProxy();
            proxy.Address = new Uri(string.Format("{0}:{1}", ConstParam.proxyHost, ConstParam.proxyPort));
            proxy.Credentials = new NetworkCredential(ConstParam.proxyUser, ConstParam.proxyPass);
            return proxy;
        }
        public string GetWebProxyString()
        {
           return string.Format("{0}:{1}@{2}:{3}", ConstParam.proxyUser, ConstParam.proxyPass, "proxy.abuyun.com", ConstParam.proxyPort);
        }
        
        int pageSize = 20;//24
#pragma warning disable CS0414 // 字段“WenShuAPPCrawler.pageSkipNum”已被赋值，但从未使用过它的值
        int pageSkipNum = 0;
#pragma warning restore CS0414 // 字段“WenShuAPPCrawler.pageSkipNum”已被赋值，但从未使用过它的值
        string cityName = "厦门";
        string materialUrl = "http://wenshuapp.court.gov.cn/MobileServices/GetLawListData";
        string curUrl = string.Empty;
        HuiCongAppHelper appHelper = new HuiCongAppHelper();
        string reqtoken = "7B16F92A0F582B309DB03D7E46364711";
        List<BsonDocument> reasonList = new List<BsonDocument>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            string curUrl = string.Format(materialUrl + "?cityName=" + HttpUtility.UrlEncode(cityName));
            //种子地址需要加布隆过滤
            reqtoken = WenShuAppHelper.GetRequestToken();
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount = 5;
            Settings.DBSaveCountLimit = 1;
            Settings.MaxReTryTimes = 10;
            Settings.IgnoreFailUrl = true;
            Settings.AutoSpeedLimit = true;
            Settings.AutoSpeedLimitMaxMSecond = 1000;
            //Settings.CurWebProxy = GetWebProxy();
            Settings.AccessToken = reqtoken;
            Settings.CrawlerClassName = "WenShuAPPCrawler";//需要进行token替换
            Settings.ContentType = "application/json";
            this.Settings.UserAgent = "Dalvik/1.6.0 (Linux; U; Android 4.4.2; GT-I9300 Build/KOT49H)";
            Settings.PostEncoding = Encoding.UTF8;
            Settings.Referer = "wenshuapp.court.gov.cn";
         
            var allCourtList = dataop.FindAllByQuery(DataTableNameCourt,Query.And(Query.NE("isUpdate","1"), Query.Matches("region",cityName))).SetFields("court", "leval").OrderByDescending(c=>c.Int("leval")).ToList();
            reasonList = dataop.FindAllByQuery(DataTableNameReason,Query.EQ("isLeaf","1")).ToList();
            foreach (var court in allCourtList)//法庭
            {
                
                var courtCondition = GenConditionStr("法院名称", court.Text("court"));
                //foreach (var fileType in fileTypeList)//文书类型10
                {
                  //  var fileTypeCondition=GenConditionStr("文书类型", fileType);
                   // foreach (var caseType in caseTypeList)//案件类型5
                    {
                      //  var caseTypeCondition = GenConditionStr("案件类型", caseType);
                        foreach (var reasonDoc in reasonList)//20
                        {
                            var reason = reasonDoc.Text("name");
                            var conditionList = new List<string>();
                            var keyWordCondition= GenConditionStr("案由", reason);
                            conditionList.Add(courtCondition);
                            //conditionList.Add(fileTypeCondition);
                            //conditionList.Add(caseTypeCondition);
                            conditionList.Add(keyWordCondition);
                            var conditionStr = GenConditionStr(conditionList);
                            var searchDoc = GenSearchStr(conditionStr);
                            var postData = searchDoc.ToJson();
                            UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                        }
                    }
                }

            }


            //var testUrl = "http://z.hc360.com/getmmtlast.cgi?dt=1&w=外墙面砖&v=59&e=100&c=供应信息&n=3101&m=2&H=1&bt=0";
            //var testAuthorization = appHelper.GetHuiCongAuthorizationCode(testUrl);
            //UrlQueue.Instance.EnQueue(new UrlInfo(testUrl) { Depth = 1, Authorization = testAuthorization });
            Console.WriteLine("正在加载账号数据");
            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的
            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            //是否guid
            //不进行地址爬取
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
        
        string[] fileTypeList = new string[] { "判决书", "裁定书", "调解书", "决定书", "通知书", "批复", "答复", "函", "令", "其他" };//文书类型
        string[] caseTypeList = new string[] { "1", "2", "3", "4", "5" };//案件类型
        string[] courtLevelList = new string[] { "1", "2", "3", "4" };//法院层级
        string[] judgeLayerList = new string[] { "一审", "二审", "再审", "非诉执行审查", "复核", "刑罚变更", "再审审查与审判监督", "其他" };
        //时间 1985-2017

        //法院名称 @DocContent关键字
        public BsonDocument GenSearchStr(string conditonStr)
        {
            return GenSearchStr(conditonStr,"0", pageSize.ToString());
        }
        /// <summary>
        /// 生成搜索字符串
        /// </summary>
        /// <param name="conditonList"></param>
        /// <returns></returns>
        public BsonDocument GenSearchStr(List<string> conditonList)
        {
            var conditonStr = GenConditionStr(conditonList);
            return GenSearchStr(conditonStr, "0", pageSize.ToString());
        }
        /// <summary>
        /// 生成搜索字符串
        /// </summary>
        /// <param name="conditonStr"></param>
        /// <param name="skip"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public BsonDocument GenSearchStr(string conditonStr,string skip,string limit)
        {
            var searchDoc = new BsonDocument();
            searchDoc.Add("dicval", "asc");
            searchDoc.Add("condition", conditonStr);
            searchDoc.Add("reqtoken", reqtoken);
            searchDoc.Add("skip", skip);
            searchDoc.Add("limit", limit);
            searchDoc.Add("dickey", "/CaseInfo/案/@法院层级");
            return searchDoc;
        }

       

        //法院名称
        /// <summary>
        /// 生成条件
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GenConditionStr(List<string> conditonList)
        {
            var sb = new StringBuilder();
            var index = 1;
            foreach (var condition in conditonList)
            {
                sb.Append(condition);
                if (index++ < conditonList.Count)
                {
                    sb.Append(" AND ");
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// 生成条件 @DocContent关键字
        /// </summary>
        /// <param name="name"></param>
        /// <param name="court"></param>
        /// <returns></returns>
        public string GenConditionStr(string name,string court)
        {
            var str = "/CaseInfo/案/@"+name+"="+ court ;
            return str;
        }
        /// <summary>
        /// 生成条件  
        /// </summary>
        /// <param name="name"></param>
        /// <param name="court"></param>
        /// <returns></returns>
        public string GenConditionDateStr(string name, string beginDate,string endDate)
        {
            var str = string.Format("/CaseInfo/案/@裁判日期=[{0} TO {1}]");
            return str;
        }
        /// <summary>
        /// 通过条件字符串省城搜索字符串
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public List<string> GetConditionList(string condition)
        {

            var ConditionList = condition.Split(new string[] { "AND" }, StringSplitOptions.RemoveEmptyEntries);
            return ConditionList.ToList();
        }
        /// <summary>
        /// 获取条件参数值
        /// /CaseInfo/案/@法院层级=4 AND /CaseInfo/案/@案件类型=5 AND /CaseInfo/案/@文书类型=其他
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetConditionParam(List<string> conditionList, string name)
        {
            ///拆分条件列表
            var hitCondition = conditionList.Where(c => c.Contains(name)).FirstOrDefault();
            if (hitCondition != null)
            {
                return Toolslib.Str.Sub(hitCondition.Trim(), "=", "");
            }
            return string.Empty;
        }
        /// <summary>
        /// 获取条件参数值
        /// /CaseInfo/案/@法院层级=4 AND /CaseInfo/案/@案件类型=5 AND /CaseInfo/案/@文书类型=其他
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetConditionParam(string condition, string name)
        {
            ///拆分条件列表
            var conditionList = GetConditionList(condition);
            return GetConditionParam(conditionList, name);
        }
        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
      
            var html = WenShuAppHelper.GetWenShuDecode(args.Html.Replace("JSON=", "").Replace("\"", ""));
            //修正为Jobject可用的对象
            html = "{\"data\":" + html + "}";
            JObject jsonObj = JObject.Parse(html);
            //获取查询条件
            var searchCondition = args.urlInfo.PostData;
            JObject searchJsonObj = JObject.Parse(searchCondition);
            var conditon = searchJsonObj["condition"].ToString();
          
            var court = GetConditionParam(conditon, "法院名称").Trim();
            var reason = GetConditionParam(conditon, "案由").Trim();
            var caseType = GetConditionParam(conditon, "案件类型").Trim();
            var fileType = GetConditionParam(conditon, "文书类型").Trim();
            var judgeLayer= GetConditionParam(conditon, "审判程序").Trim();
            
            var data = jsonObj["data"];
            if (data != null)
            {
                var insert = 0;
                var update = 0;
                Console.WriteLine("获得数据:{0}",data.ToList().Count);
                foreach (var entInfo in data.ToList())
                {

                    BsonDocument document  = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(entInfo.ToString());
                    var guid = document.Text("文书ID");
                    if (!guidFilter.Contains(guid) && !hasExistObj(guid))
                    {
                        document.Set("guid", guid);
                        document.Set("cityName", cityName);
                        document.Set("reason", reason);
                        insert++;
                        guidFilter.Add(guid);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = document, Name = DataTableName, Type = StorageType.Insert });
                        //增加reason匹配
                        var hitReason = reasonList.Where(c => c.Text("name").Trim() == reason).FirstOrDefault();
                        if (hitReason != null)
                        {
                            var hitCount = hitReason.Int("count") + 1;
                            hitReason.Set("count", hitCount);
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("count", hitCount), Name = DataTableNameReason, Type = StorageType.Update, Query = Query.EQ("guid", hitReason.Text("guid")) });
                        }
                   }
                    else//更新目录
                    {
                        update++;
                    }
                }
             
             
             
                var skip = (int)searchJsonObj["skip"];
                var limit = (int)searchJsonObj["limit"];
                Console.WriteLine("获得{4}skip:{5}keyword:{6}{7}{8}|数据{3},添加：{0} 更新{1}剩余url:{2}", insert, update, UrlQueue.Instance.Count, data.ToList().Count, court.Replace("人民", "").Replace("法院", ""), skip, reason, caseType, fileType);
                if (data.ToList().Count >= pageSize&& skip<200)
                {
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("currenNum", skip.ToString()), Name = DataTableNameCourt, Type = StorageType.Update, Query = Query.EQ("court", court) });
                    skip = skip + pageSize;
                    searchJsonObj["skip"] = skip.ToString();
                    var postData = searchJsonObj.ToString();
                    UrlQueue.Instance.EnQueue(new UrlInfo(args.Url) { Depth = 1, PostData = postData });
                  
                }
                else
                {
                    if (skip >= 200)//增加筛选关键字》案件类型》文书类型
                    {
                        var isNewUrl= InitNextUrl(searchJsonObj);
                        if (!isNewUrl)
                        {
                            //条件增加时间筛选
                            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("condition", args.urlInfo.PostData), Name = DataTableNameURL, Type = StorageType.Insert });
                        }
                    }
                    else
                    {
                        //Console.WriteLine("{0}爬取结束", court);
                        //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isUpdate", "1"), Name = DataTableNameCourt, Type = StorageType.Update, Query = Query.EQ("court", court) });
                    }
                }
            }
           
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private bool InitNextUrl(JObject searchJsonObj)
        {
            var conditon = searchJsonObj["condition"].ToString();
            var conditonList = GetConditionList(conditon);
            var caseTypeStr = GetConditionParam(conditonList, "案件类型").Trim();
            var fileTypeStr = GetConditionParam(conditonList, "文书类型").Trim();
            var judgeLayer = GetConditionParam(conditon, "审判程序").Trim();
            var  dateStr = GetConditionParam(conditonList, "裁判日期").Trim();//[1985-03-24 TO 1990-03-24]
            if (string.IsNullOrEmpty(caseTypeStr))
            {
                foreach (var _caseType in caseTypeList)//根据案件类型过滤
                {
                    var caseTypeCondition = GenConditionStr("案件类型", _caseType);
                    conditonList.Add(caseTypeCondition);
                    var searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                    var postData = searchDoc.ToJson();
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                }
                return true;
            }
            if (string.IsNullOrEmpty(fileTypeStr))
            {
                foreach (var _fileType in fileTypeList)
                {
                  
                    var fileTypeCondition = GenConditionStr("文书类型", _fileType);
                    conditonList.Add(fileTypeCondition);
                    var searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                    var postData = searchDoc.ToJson();
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                }
                return true;
            }
            if (string.IsNullOrEmpty(fileTypeStr))
            {
                foreach (var _fileType in fileTypeList)
                {

                    var fileTypeCondition = GenConditionStr("文书类型", _fileType);
                    conditonList.Add(fileTypeCondition);
                    var searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                    var postData = searchDoc.ToJson();
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                }
                return true;
            }

            if (string.IsNullOrEmpty(judgeLayer))
            {
                foreach (var _judgeLayer in judgeLayerList)
                {

                    var judgeLayerCondition = GenConditionStr("案由", _judgeLayer);
                    conditonList.Add(judgeLayerCondition);
                    var searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                    var postData = searchDoc.ToJson();
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                }
                return true;
            }
            if (string.IsNullOrEmpty(dateStr))//[1985-03-24 TO 1990-03-24]
            {
               var _dateStr=GenConditionDateStr("裁判日期", "1985-01-01", "2012-01-01");
                conditonList.Add(_dateStr);
                var searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                var postData = searchDoc.ToJson();
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                var beginDate = DateTime.Parse("2012-01-01");
                while (beginDate < DateTime.Now)
                {
                    var newConditonList =  GetConditionList(conditon);
                    var endDate = beginDate.AddYears(1);
                    var _otherDateStr = GenConditionDateStr("裁判日期", beginDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                    conditonList.Add(_otherDateStr);
                    var _searchDoc = GenSearchStr(conditonList);//生成搜索字符串
                    var _postData = searchDoc.ToJson();
                    UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { Depth = 1, PostData = postData });
                    beginDate = endDate.AddDays(1);
                   
                }
                return true;
            }
            return false;
        }


        private BsonDocument GetObj(string guid)
        {
            return dataop.FindOneByQuery(DataTableName, Query.EQ("guid", guid));
        }
        private bool hasExistObj(string guid)
        {
            return dataop.FindCount(DataTableName, Query.EQ("guid", guid)) > 0;
        }
        private string TrimStr(string str)
        {
            return str.Replace(" ", "").Replace("\"", "").Trim();
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
            if (string.IsNullOrEmpty(html))
            {
                return true;
            }
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
