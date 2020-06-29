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
    ///http://api.jgjapp.com/jlforemanwork/findhelper?cityno=110100&role_type=1&work_type=4&pg=1&pageSize=10&platform=w&os=A&token=901f82dd2d2a7b4b1f2c64860279bdcf 9相似产品
    ///  
    /// 
    ///  </summary>
    public class JGJUserAPPCrawler : ISimpleCrawler
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
        private BloomFilter<string> guidFilter;
        private   string _DataTableName = "jgj_UserInfo";//存储的数据库表明
       

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
            get { return "jgj_CityInfo"; }

        }
        /// <summary>
        /// 城市信息
        /// </summary>
        public string DataTableNameProvinceCity
        {
            get { return "jgj_ProvinceInfo"; }

        }
        /// <summary>
        /// 模拟登陆账号
        /// </summary>
        public string DataTableNameAccount
        {
            get { return _DataTableName + "Account"; }

        }
        /// <summary>
        ///  分类信息
        /// </summary>
        public string DataTableNameWorkType
        {
            get { return "jgj_WorkType"; }

        }

        /// <summary>
        ///  构造函数
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public JGJUserAPPCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            guidFilter = new BloomFilter<string>(9000000);
        }
        public bool isSpecialUrlMode = false;
      

#pragma warning disable CS0414 // 字段“JGJUserAPPCrawler.pageSize”已被赋值，但从未使用过它的值
        int pageSize = 100;//24
#pragma warning restore CS0414 // 字段“JGJUserAPPCrawler.pageSize”已被赋值，但从未使用过它的值
#pragma warning disable CS0414 // 字段“JGJUserAPPCrawler.pageBeginNum”已被赋值，但从未使用过它的值
        int pageBeginNum = 1;
#pragma warning restore CS0414 // 字段“JGJUserAPPCrawler.pageBeginNum”已被赋值，但从未使用过它的值
        List<BsonDocument> allCityList = new List<BsonDocument>();
        List<BsonDocument> allWorkTypeList = new List<BsonDocument>();
        //将z.hc360改成 s.hc360 可用
        HuiCongAppHelper appHelper = new HuiCongAppHelper();
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
            Settings.ThreadCount = 5;
            Settings.DBSaveCountLimit = 1;
            //Settings.UseSuperWebClient = true;
            Settings.MaxReTryTimes = 10;
           
             
            //Settings.CurWebProxy = GetWebProxy();
            Settings.ContentType = "application/x-www-form-urlencoded";
            Settings.Accept = "application/json";
            this.Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36";
            Settings.Referer = "http://wx1.jgjapp.com/helper/list/110100?city_name=%E5%8C%97%E4%BA%AC%E5%B8%82&city_no=110100&role_type=2&work_space=&work_type_id=5&work_type_name=%E6%B3%A5%E6%B0%B4%E5%B7%A5";
            var headSetDic = new Dictionary<string, string>();
            
            headSetDic.Add("Origin", "http://wx1.jgjapp.com");
            Settings.HeadSetDic = headSetDic;
            //date=&end_date=&title=&content=&key=%E5%85%AC%E5%8F%B8&database=saic&search_field=all&search_type=yes&page=2

              allCityList = dataop.FindAll(DataTableNameCity).ToList();
              allWorkTypeList = dataop.FindAll(DataTableNameWorkType).ToList();
            var allGuidList = dataop.FindFieldsByQuery(DataTableName, null, new string[] { "guid" }).ToList();
            foreach (var obj in allGuidList)
            {
                if(!guidFilter.Contains(obj.Text("guid")))
                guidFilter.Add(obj.Text("guid"));
            }
            foreach (var city in allCityList) {
                var cityCode = city.Text("city_code");
                //foreach (var workType in allWorkTypeList)
                {
                   // var code = workType.Text("code");
                    var url = string.Format("http://api.jgjapp.com/jlforemanwork/findhelper?cityno={0}&role_type=2&pg=1&pageSize=10&platform=w&os=A&token=901f82dd2d2a7b4b1f2c64860279bdcf", cityCode);
                    if (!filter.Contains(url))
                    {
                        filter.Add(url);
                        UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                    }
                }

            }
          
   


           
            Console.WriteLine("正在加载账号数据");


            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的

            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            ////是否guid
            ///不进行地址爬取
            Settings.RegularFilterExpressions.Add(@"luckymnXXXXXXXXXXXXXXXXXX");

            if (SimulateLogin())
            {
                 Console.WriteLine("开始读取数据");
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
        private bool hasExistObj(string guid)
        {
            return dataop.FindCount(DataTableName, Query.EQ("guid", guid)) > 0;
        }
        private string TrimStr(string str)
        {
            return str.Replace(" ", "").Replace("\"", "").Trim();
        }
        public string ToUnicodeString(string str)
        {
            StringBuilder strResult = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    strResult.Append("\\u");
                    strResult.Append(((int)str[i]).ToString("x"));
                }
            }
            return strResult.ToString();
        }

        public string FromUnicodeString(string str)
        {
            //最直接的方法Regex.Unescape(str);
            StringBuilder strResult = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                string[] strlist = str.Replace("\\", "").Split('u');
                try
                {
                    for (int i = 1; i < strlist.Length; i++)
                    {
                        int charCode = Convert.ToInt32(strlist[i], 16);
                        strResult.Append((char)charCode);
                    }
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (FormatException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    return Regex.Unescape(str);
                }
            }
            return strResult.ToString();
        }
        /// <summary>
        /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=1&c=供应信息&n=2&m=2&H=1&bt=0
        ///  </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            JObject jsonObj = JObject.Parse(FromUnicodeString(args.Html));
            var curPageSize = GetUrlParam(args.Url, "pageSize");//每页个数默认10
            var pageIndex=HttpUtility.UrlDecode(GetUrlParam(args.Url, "pg"));//每页个数默认24
            var cityno = HttpUtility.UrlDecode(GetUrlParam(args.Url, "cityno"));//城市代码
            var workType = HttpUtility.UrlDecode(GetUrlParam(args.Url, "work_type"));//工作代码
            var role_type = GetUrlParam(args.Url, "role_type");
            var cityObj = allCityList.Where(c => c.Text("city_code") == cityno).FirstOrDefault();
            var workTypeObj = allWorkTypeList.Where(c => c.Text("code") == workType).FirstOrDefault();
            if (cityObj == null )
            {
                Console.WriteLine("参数不正确");
                return;
            }
            if (workTypeObj == null)
            {
                workTypeObj = new BsonDocument();
            }
            var data = jsonObj["values"];
            var insert = 0;
            var update = 0;
            if (data != null)
            {
                var allRecordCount = data.ToList().Count;
                Console.WriteLine("获得数据:{0}",data.ToList().Count);
                List<BsonDocument> documentList = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<List<BsonDocument>>(data.ToString());
                foreach (var document in documentList)
                {

                    var guid = document.Text("uid");
                    document.Set("cityno", cityno);//目录名
                    document.Add("role_type", role_type);
                    if (!guidFilter.Contains(guid) && !hasExistObj(guid))
                    {
                        document.Set("guid", guid);
                       
                        insert++;
                        guidFilter.Add(guid);
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = document, Name = DataTableName, Type = StorageType.Insert });
                    }
                    else
                    {
                       
                        update++;
                    }
                }
                Console.WriteLine("获得数据{3},添加：{0} cityName:{4} worType:{5} 更新{1}剩余url:{2}", insert, update,UrlQueue.Instance.Count, allRecordCount, cityObj.Text("city_name"), workTypeObj.Text("name"));
            }
            if (data.ToList().Count >= 10)
            {
                InitNextUrl(args.Url);
            }
            else
            {
                Console.WriteLine("当前记录个数为:{0},无法进行翻页操作", data.ToList().Count);
                //Console.Read();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allRecordCount"></param>
        /// <param name="curNum"></param>
        /// <returns></returns>
        private void InitNextUrl(string  url)
        {
            /// http://z.hc360.com/getmmtlast.cgi?dt=1&w=单开&v=59&e=24&c=供应信息&n=1&m=2&H=1&bt=0
            var pageIndex = GetUrlParam(url, "pg");//每页个数默认10
            var curPageIndex = 1;
            if (int.TryParse(pageIndex, out curPageIndex))
            {
                var oldPageIndexStr = string.Format("&pg={0}&", pageIndex);
                var newPageIndexStr= string.Format("&pg={0}&", curPageIndex + 1);
                var newUrl = url.Replace(oldPageIndexStr, newPageIndexStr);
                if (!filter.Contains(newUrl))
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(newUrl) { Depth = 1 });
                    Console.WriteLine("当前PageIndex:{0},{1}", curPageIndex + 1,newUrl);
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
            var html = args.Html;
            if (args.Html.Contains("\"state\":1"))
            {
                return false;
            }
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
#pragma warning disable CS0162 // 检测到无法访问的代码
            if (Settings.LandFangIUserId == 0)
#pragma warning restore CS0162 // 检测到无法访问的代码
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
