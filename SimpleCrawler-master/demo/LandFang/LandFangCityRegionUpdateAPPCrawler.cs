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

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 通过app进行更新,更新最新推出的地块
    /// </summary>
    public class LandFangCityRegionUpdateAPPCrawler : ISimpleCrawler
    {

        
        DataOperation dataop = null;
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
        public string DataTableUpdateCity
        {
            get { return _DataTableName+"CityNeedUpdated"; }

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
        //List<string> updateCityNameList = new List<string>();
        private BloomFilter<string> updateCityNameList=new BloomFilter<string>(1000);
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangCityRegionUpdateAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
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
            proxy.Address = new Uri(string.Format("{0}:{1}", "http://proxy.abuyun.com", "9010"));
            proxy.Credentials = new NetworkCredential("H1538UM3D6R2133P", "511AF06ABED1E7AE");
            return proxy;
        }
        /// <summary>
        /// 代理
        /// </summary>
        /// <returns></returns>
        public string GetWebProxyCurl()
        {
            // 设置代理服务器
            return string.Format("http://{0}:{1}@{2}:{3}", "H1538UM3D6R2133P", "511AF06ABED1E7AE", "proxy.abuyun.com", "9010");
            
        }
       
        List<BsonDocument> cityList = new List<BsonDocument>();
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
            Settings.ThreadCount = 1;
            Settings.DBSaveCountLimit = 1;
            //if (Settings.CurWebProxy != null)
            //{
            //    Settings.CurWebProxy = GetWebProxy();
            //}
            Settings.MaxReTryTimes = 30;
            this.Settings.UserAgent = "android_tudi%7EGT-P5210%7E4.2.2";
            
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


            Console.WriteLine("正在获取已存在的url数据");
#pragma warning disable CS0219 // 变量“partName”已被赋值，但从未使用过它的值
            var partName = "所在地";
#pragma warning restore CS0219 // 变量“partName”已被赋值，但从未使用过它的值
            Console.WriteLine("正在处理更新数据10页每次100个", landUrlList.Count);
            cityList = dataop.FindAllByQuery("LandFangCityEXURL",Query.NE("type","2")).SetFields("name", "cityCode","type", "provinceCode").ToList();
            for (var i= 1;i < 20; i++){ 
                var detailUrl = appHelper.InitPushLandUrl( "100", i.ToString());
                UrlQueue.Instance.EnQueue(new UrlInfo(detailUrl) { Depth = 1 });
            }
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
            if (userCrawlerCountHashTable.ContainsKey(Settings.LandFangIUserId))
            {
               
                var value=int.Parse(userCrawlerCountHashTable[Settings.LandFangIUserId].ToString());
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
            
            JObject jsonObj = JObject.Parse(args.Html);
            var resultDic = jsonObj["resulDic"];
            foreach (var result in resultDic)
            {
                var updateBson = new BsonDocument();
                var guid = result["Id"].ToString();
                var cityName= result["City"].ToString().Replace("[","").Replace("]","").Replace("辖区","").Replace("所辖县","");
                if (cityName == "襄樊")
                {
                    cityName = "襄阳";
                }
                ///添加更新的城市
                if (!updateCityNameList.Contains(cityName)&&!string.IsNullOrEmpty(cityName))
                {
                    updateCityNameList.Add(cityName);
                    var cityBson = new BsonDocument();
                    cityBson.Add("cityName",cityName);
                    cityBson.Add("date", DateTime.Now.ToString("yyyy-MM-dd"));
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = cityBson, Name = DataTableUpdateCity, Type = StorageType.Insert });
                }
                var name= result["Title"].ToString();
                var hitCity = cityList.Where(c => cityName==c.Text("name")).FirstOrDefault();
                var provinceName = string.Empty;
                if (hitCity != null)
                {
                    var provinceObj = cityList.Where(c => c.Int("type") == 0 && c.Text("provinceCode") == hitCity.Text("provinceCode")).FirstOrDefault();
                    if (provinceObj != null)

                    {
                        provinceName = provinceObj.Text("name");
                    }
                }
                var url = string.Format("http://land.fang.com/market/{0}.html", guid.ToLower());
                if (string.IsNullOrEmpty(provinceName))
                {
                    var provinceObj = cityList.Where(c => c.Int("type") == 0 && c.Text("name") == "未知").FirstOrDefault();
                    if (provinceObj != null)
                    {
                        provinceName = provinceObj.Text("name");
                    }
                    if (string.IsNullOrEmpty(provinceName))
                    {
                        Console.WriteLine("省份不存在" + cityName);
                        continue;
                    }
                }
                
               
                var hitLandObj = dataop.FindOneByQuery(DataTableName, Query.Or(Query.EQ("url", url)));
                if (hitLandObj == null)
                {
                    updateBson.Add("guid", guid);
                    updateBson.Add("url", url);
                    updateBson.Add("所在地", cityName);
                    updateBson.Add("name", name);
                    updateBson.Add("needUpdate", "1");
                    Console.WriteLine(string.Format("{0}添加{1}剩余url:{2}", name, Settings.LandFangIUserId,UrlQueue.Instance.Count));
                    //updateBson.Set("url", hitUrl);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Name = DataTableName, Type = StorageType.Insert });
                }
                else
                {
                    if (string.IsNullOrEmpty(hitLandObj.Text("竞得方")) || hitLandObj.Text("竞得方") == "******")
                    {
                        updateBson.Add("needUpdate", "1");
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBson, Query = Query.EQ("url", url), Name = DataTableName, Type = StorageType.Update });
                    }
                    else
                    {
                        Console.WriteLine(string.Format("{0}已存在{1}剩余url:{2}", "", Settings.LandFangIUserId, UrlQueue.Instance.Count));
                    }
                }
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
            
            if (hmtl.Contains("resulDic"))
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
