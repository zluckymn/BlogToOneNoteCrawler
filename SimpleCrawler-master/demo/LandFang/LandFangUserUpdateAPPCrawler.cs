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
using System.Threading;
using Helper;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 通过app进行更新
    /// </summary>
    public class LandFangUserUpdateAPPCrawler : ISimpleCrawler
    {

       
        DataOperation dataop = null;
        MongoOperation mongoOp = null;
        private CrawlSettings Settings = null;
        LandFangAppHelper appHelper = new LandFangAppHelper();
        private Dictionary<string, string> columnMapDic = new Dictionary<string, string>();
      
        private Hashtable  userCrawlerCountHashTable = new Hashtable();
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private const string _DataTableName = "LandFang";//存储的数据库表明
        List<BsonDocument> landUrlList = new List<BsonDocument>();

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
        /// 返回
        /// </summary>
        public string DataTableNameCity
        {
            get { return _DataTableName + "CityURL"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        private void InitMapDic()
        {
            columnMapDic.Add("sParcelID", "guid");
            columnMapDic.Add("sParcelName", "name");
            columnMapDic.Add("sParcelSN", "地块编号");
            columnMapDic.Add("sParcelArea", "地区");
            columnMapDic.Add("sParcelAreaCity", "所在地");
            columnMapDic.Add("sParcelAreaDis", "县市");
            columnMapDic.Add("fparcelarea", "总面积");
            columnMapDic.Add("fcollectingarea", "代征面积");

            columnMapDic.Add("fbuildarea", "建设用地面积");
            columnMapDic.Add("fplanningarea", "规划建筑面积");
            columnMapDic.Add("sPlotratio", "容积率");
            columnMapDic.Add("sremiseway", "出让形式");
            columnMapDic.Add("sservicelife", "出让年限");
            columnMapDic.Add("sparcelplace", "位置");
            columnMapDic.Add("sparcelextremes", "四至");
            columnMapDic.Add("sConforming", "规划用途");

            columnMapDic.Add("sdealstatus", "交易状况");
            columnMapDic.Add("istartdate", "起始日期");
            columnMapDic.Add("ienddate", "截止日期");
            columnMapDic.Add("dAnnouncementDate", "公告日期");
            columnMapDic.Add("finitialprice", "起始价");
            columnMapDic.Add("sbidincrements", "最小加价幅度");
            columnMapDic.Add("sperformancebond", "保证金");
            columnMapDic.Add("fInitialFloorPrice", "推出楼面价");
            columnMapDic.Add("stransactionsites", "交易地点");
            columnMapDic.Add("sconsulttelephone", "咨询电话");
            columnMapDic.Add("fcoordinateax", "x");
            columnMapDic.Add("fcoordinateay", "y");
            columnMapDic.Add("Land_fAvgPremiumRate", "溢价率");
            columnMapDic.Add("Land_sParcelMemo", "备注");
            columnMapDic.Add("Land_sTransferee", "竞得方");
            columnMapDic.Add("Land_fInitialUnitPrice", "土地单价");
            columnMapDic.Add("icompletiondate", "成交日期");
            columnMapDic.Add("fclosingcost", "成交价");
            columnMapDic.Add("fprice", "楼面地价");
            columnMapDic.Add("sGreeningRate", "绿化率");
            columnMapDic.Add("Land_sCommerceRate", "商业比例");
            columnMapDic.Add("Land_sBuildingDensity", "建筑密度");
            columnMapDic.Add("Land_sLimitedHeight", "限制高度");
            columnMapDic.Add("Land_bIsSecurityHousing", "配建保障房建设");
            columnMapDic.Add("sAnnouncementNo", "公告编号");
        }
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangUserUpdateAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            mongoOp = MongoOpCollection.GetNew121MongoOp_MT("LandFang");
            dataop =new DataOperation(mongoOp);
            Settings = _Settings; filter = _filter; 
            //dataop = _dataop;
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
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public string GetWebProxyCurl()
        {
            // 设置代理服务器
            return string.Format("http://{0}:{1}@{2}:{3}", ConstParam.proxyUser, ConstParam.proxyPass, "proxy.abuyun.com", ConstParam.proxyPort);
            
        }
        int CurThreadId = 0;
        internal bool CanLoadNewData()
        {
            if (CurThreadId == 0)
            {
                CurThreadId = Thread.CurrentThread.ManagedThreadId;
            }
            if (UrlQueue.Instance.Count <= 10 && Thread.CurrentThread.ManagedThreadId == CurThreadId)
            {
                return true;
            }
            return false;
        }
        int allCount = 1;
        public void InitialUrl()
        {
            Console.WriteLine("正在获取已存在的url数据");
            var partName = "所在地";
            //注意需要定时爬去isSpecialUrl 为1 的url 这些url需要用无账号登陆进行使用
            //布隆url初始化,防止重复读取url//         "中山", 
            // ,
            //var distinctAreaStr = new string[] {"长沙","成都","大连","佛山","福州","广州","杭州","黄山","济南","昆明","龙岩","南昌","南京","宁波","泉州","深圳","苏州","武汉","西安","厦门","烟台","镇江","郑州" };
            //var distinctAreaStr = new string[] { "北京", "上海", "重庆" };
            //var distinctAreaStr = new string[] { "长沙", "成都", "大连", "佛山", "福州", "广州", "杭州","佛山", "南京", "深圳", "武汉","西安" };
            // var cityStr = "南京,苏州,常州,无锡,南通,西安,烟台,佛山,泉州,广州,深圳,成都,昆明,大连,青岛,哈尔滨,沈阳,日照, 南宁,武汉,长沙,合肥,济南,郑州,南昌,杭州,兰州,长春,海口,西宁,石家庄,宁波,贵阳,西宁,乌鲁木齐,呼和浩特,银川,拉萨,福州,厦门,东莞";
            //var cityStr = "东莞，上海，深圳，武汉，成都，重庆";
            // var cityStr = "东莞，上海，深圳，武汉，成都，重庆";
            var cityStr = "保定,成都,广州,杭州,济宁,南宁,宁波,石家庄,潍坊,徐州,烟台,长春,西安,福州,自贡,株洲,漳州,西安,唐山,上饶,厦门,泉州,三亚,梅州,惠州,济南,合肥";
            //var cityStr = "南京";//,
            var distinctAreaStr = cityStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var orQuery = Query.In(partName, distinctAreaStr.Select(c => (BsonValue)c));
            var distincorQuery = Query.In("地区", "北京,上海,天津,重庆".Select(c => (BsonValue)c));
            isSpecialUrlMode = false;//使用特殊模式表是某些url 需要进行不登陆进行爬去，true代表不登陆爬取
            var specialUrlQuery = Query.And(Query.NE("isSpecialUrl", "1"));
            if (isSpecialUrlMode == true)
            {
                specialUrlQuery = Query.EQ("isSpecialUrl", "1");
            }
            //var orQuery = Query.In("地区", " ");deleteStatus Query.EQ("竞得方", ""),Query.EQ("竞得方", "暂无")
            var takeCount = 10000;
            // var query = Query.And( Query.NE("deleteStatus", "1"), specialUrlQuery, Query.Or(Query.EQ("竞得方", "暂无"),  Query.Exists("竞得方", false), Query.EQ("竞得方", "******")));
            //Query.Or(orQuery, distincorQuery),
            var query = Query.And(Query.Or( Query.Exists("竞得方", false),Query.EQ("竞得方", "暂无")), Query.NE("updateMonth", DateTime.Now.Month % 6), Query.NE("deleteStatus", "1"));
            //(Query.NE("deleteStatus", "1"), 
            if (allCount <=1)
            {
                //allCount = 10001;
               allCount= (int)mongoOp.FindCount(DataTableName, query);
            }
           
            if (allCount <= 10000)
            {
                takeCount = allCount;
            }
            allCount -= takeCount;
            var fields = new string[] { "url", "guid", "updateMonth" };
            landUrlList = mongoOp.FindAll(DataTableName,query).SetFields(fields).SetLimit(takeCount).ToList();//土地url

            //未更新的地块
            //landUrlList = dataop.FindAllByQuery(DataTableName, Query.And(Query.NE("deleteStatus", "1"), specialUrlQuery, Query.Or( Query.EQ("竞得方", "")))).Take(10000).ToList();//土地url

            //landUrlList = dataop.FindAllByQuery(DataTableName, Query.And( specialUrlQuery, Query.EQ("needUpdate", "1"))).Take(100000).ToList();//土地url
            //landUrlList = dataop.FindAllByQuery(DataTableName, Query.Or(Query.Exists("竞得方", false))).Take(10000).ToList();//土地url
            //  var allAccountList = dataop.FindAllByQuery(DataTableNameAccount,Query.EQ("userName", "18900372887")).SetFields("userName", "postData", "status", "passWord").ToList();
            Console.WriteLine("待处理数据{0}个", allCount);

            foreach (var cityObj in landUrlList)
            {
              
                var url = cityObj.Text("url");//http://land.fang.com/market/2e81878c-eb62-4687-971f-01b174817207.html
                var guid = GetGuidFromUrl(url);
                // var guid = cityObj.Text("sParcelID");
                if (!string.IsNullOrEmpty(guid))
                {
                    var detailUrl = appHelper.InitLandDetailUrl(guid, "143636", "true");
                    if (!filter.Contains(url))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(detailUrl) { Depth = 1, UniqueKey = url });
                        filter.Add(url);
                    }
                }
            }
        }
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            //Settings.IgnoreFailUrl = false;
            Settings.MaxReTryTimes = 100;
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
           // Settings.CurWebProxy = GetWebProxy();
           
            this.Settings.UserAgent = "android_tudi%7EGT-P5210%7E4.2.2";
            InitMapDic();
            var headSetDic = new Dictionary<string, string>();
            // headSetDic.Add("Accept-Encoding", "gzip");
            // hi.HeaderSet("Content-Length","154");
            // hi.HeaderSet("Connection","Keep-Alive");
            headSetDic.Add("imei", "133524413725754");
            // headSetDic.Add("Host", "appapi.3g.fang.com");
            headSetDic.Add("version", "2.5.0");
            // headSetDic.Add("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
            //hi.HeaderSet("user-agent", "android_tudi%7EGT-P5210%7E4.2.2");
            headSetDic.Add("ispos", "1");
            headSetDic.Add("app_name", "android_tudi");
            headSetDic.Add("iscard", "1");
            headSetDic.Add("connmode", "Wifi");
            headSetDic.Add("model", "GT-P5210");
            headSetDic.Add("posmode", "gps%2Cwifi");
            headSetDic.Add("company", "-10000");
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2

            InitialUrl();


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

        private static string GetGuidFromUrl(string url)
        {
            var beginStrIndex = url.LastIndexOf("/");
            var endStrIndex = url.LastIndexOf(".");
            if (beginStrIndex != -1 && endStrIndex != -1)
            {
                if (beginStrIndex > endStrIndex)
                {
                    var temtp = beginStrIndex;
                    beginStrIndex = endStrIndex;
                    endStrIndex = temtp;
                }
                var queryStr = url.Substring(beginStrIndex + 1, endStrIndex - beginStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// <sParcelID><![CDATA[de6aebb8-d7c7-4067-8c73-12eb0836b1ae]]></sParcelID><sParcelName><![CDATA[松山湖金多港]]></sParcelName>
        ///{{ "sParcelID" : "de6aebb8-d7c7-4067-8c73-12eb0836b1ae", "sParcelName" : "松山湖金多港", "sParcelSN" : "2014WT038", "sParcelArea" : "广东省", "sParcelAreaCity" : "东莞市", "sParcelAreaDis" : "", "fparcelarea" : "35916.27㎡", "fcollectingarea" : "暂无", "fbuildarea" : "35916.27㎡", "fplanningarea" : "71832.54㎡", "sPlotratio" : "≤2", "sremiseway" : "挂牌", "sservicelife" : "50年", "sparcelplace" : "松山湖金多港", "sparcelextremes" : "松山湖金多港", "sConforming" : "其它用地", "sdealstatus" : "中止交易", "istartdate" : "2014-07-02", "ienddate" : "2014-07-16", "dAnnouncementDate" : "2014-06-12", "finitialprice" : "2874.00万元", "sbidincrements" : "50万元", "sperformancebond" : "750万元", "fInitialFloorPrice" : "400.10元/㎡", "stransactionsites" : "东莞市国土资源局", "sconsulttelephone" : "076926983723", "fcoordinateax" : "", "fcoordinateay" : "", "mapurl" : "https://api.map.baidu.com/staticimage?markers=&width=500&height=500&zoom=12&scale=1", "Land_fAvgPremiumRate" : "暂无", "Land_sParcelMemo" : "暂无", "Land_sTransferee" : "暂无", "Land_fInitialUnitPrice" : "800.19万元", "icompletiondate" : "1900-01-01", "fclosingcost" : "0.00万元", "fprice" : "0.00元/㎡", "sGreeningRate" : "暂无", "Land_sCommerceRate" : "暂无", "Land_sBuildingDensity" : "≤35", "Land_sLimitedHeight" : "暂无", "Land_bIsSecurityHousing" : "无", "sAnnouncementNo" : "WGJ2014050", "readcount" : "12", "isread" : "1", "isfavorite" : "0", "sImages" : "", "sImages_o" : "", "usertype" : "", "message" : "了解房企拿地状况，地块项目进展等信息，请加入数据库会员，更多专享服务为您量身打造！" }}
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            //if (CanLoadNewData())
            //{
            //    InitialUrl();
            //}
            var urlKey = args.urlInfo.UniqueKey;
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var updateBson = new BsonDocument();
            var curLandBson = new BsonDocument();
            var allBson = new BsonDocument();
            var landName = string.Empty;
            //return;
            var root = "<root>" + Toolslib.Str.Sub(args.Html, "<root>", "</root>") + "</root>";
            if (string.IsNullOrEmpty(root)) return;
            if (userCrawlerCountHashTable.ContainsKey(Settings.LandFangIUserId))
            {

                var value = int.Parse(userCrawlerCountHashTable[Settings.LandFangIUserId].ToString());
                userCrawlerCountHashTable[Settings.LandFangIUserId] = value + 1;
                if (value >= Settings.MaxAccountCrawlerCount)
                {
                    SimulateLogin();//更换账号
                }
            }
            else
            {
                userCrawlerCountHashTable.Add(Settings.LandFangIUserId, 1); ;
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(root);
            var doc = xmlDoc.DocumentElement;
            if (doc == null) return;

            foreach (XmlNode elem in doc.ChildNodes)
            {

                var elemName = elem.Name;
                if (elemName == "message") continue;
                var value = elem.InnerXml.Replace("<![CDATA[", "").Replace("]]>", "");
                curLandBson.Set(elemName, value);
            }
            landName = curLandBson.Text("sParcelName");
            var hitUrl = string.Format("http://land.fang.com/market/{0}.html", curLandBson.Text("sParcelID"));
            var hitObj = landUrlList.Where(c => c.Text("url") == hitUrl).FirstOrDefault();
            if (hitObj == null)
            {   //此处可能url 为大写，但是数据库保存在小写
               var tempHitUrl = string.Format("http://land.fang.com/market/{0}.html", curLandBson.Text("sParcelID").ToLower());
                hitObj = landUrlList.Where(c => c.Text("url") == tempHitUrl).FirstOrDefault();
                if (hitObj == null)
                {
                    Console.WriteLine("对象不存在");
                    //return;
                }
                else
                {
                   // updateBson.Set("url", hitUrl);
                    hitUrl = tempHitUrl;
                }
            }
            foreach (var column in columnMapDic)//映射
            {
                var appColumn = column.Key;
                var mongoColumn = column.Value;
                var value = curLandBson.Text(appColumn);
                if (value.Contains("******"))
                {
                    Console.WriteLine("账号出错******");
                    return;
                }
                //allBson.Set(mongoColumn, value);
                if (!string.IsNullOrEmpty(value) && value != hitObj.Text(mongoColumn)) {
                    switch (mongoColumn)
                    {
                        case "地区":
                            //value = value.TrimEnd(new char[] { '省' });
                            //if (!string.IsNullOrEmpty(value) && value != hitObj.Text("地区"))
                            //{
                            //    updateBson.Set(mongoColumn, value);
                            //}
                            break;
                        case "所在地":
                            value = value.TrimEnd(new char[] { '市' });
                            if (!string.IsNullOrEmpty(value) && value != hitObj.Text("所在地"))
                            {
                                updateBson.Set(mongoColumn, value);
                                Console.WriteLine("{0}更新了所在地为{1}>{2}", landName, hitObj.Text("所在地"), value);
                            }
                            break;
                        case "县市":
                            if (!string.IsNullOrEmpty(value) && value != hitObj.Text("县市"))
                            {
                                updateBson.Set(mongoColumn, value);
                                Console.WriteLine("{0}更新了县市为{1}>{2}", landName, hitObj.Text("县市"), value);
                            }
                            break;
                        default:
                            updateBson.Set(mongoColumn, value);
                            break;
                    }

                }
            }

            if (hitObj.Text("isTradeStatusChange") == "1")
            {
                updateBson.Add("isTradeStatusChange", "0");
            }
            if (hitObj.Text("needUpdate") == "1")
            {
                updateBson.Add("needUpdate", "0");
            }
            updateBson.Add("updateMonth", DateTime.Now.Month % 6);//6个月只更新一次
            updateBson.Add("isUserUpdated", "1");//更新了数据
            Console.WriteLine($"{landName},{Settings.LandFangIUserId}更新 竞得方:{updateBson.Text("竞得方")}{urlKey}");
            //updateBson.Set("url", hitUrl);
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Name = DataTableName, Query = Query.EQ("url", urlKey), Type = StorageType.Update });


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
            if (args.Html.Contains("Object moved"))//需要编写被限定IP的处理
            {
                return true;
            }

            var hmtl = args.Html;
            if (hmtl.Contains("未找到该信息"))
            {
                var sParcelID = GetUrlParam(args.Url, "sParcelID");
                var hitUrl = string.Format("http://land.fang.com/market/{0}.html", sParcelID);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("deleteStatus","1"), Name = DataTableName, Query = Query.EQ("url", hitUrl), Type = StorageType.Update });
                Console.WriteLine("未找到该信息");
                return false;
            }
            if (hmtl.Contains("******")) {
                Console.WriteLine("账号ip限制");
                return true;
            }
            if (hmtl.Contains("error"))
                return true;
            if (hmtl.Contains("sParcelID"))
                return false;
            else
            {
                return true;
            }
             
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
