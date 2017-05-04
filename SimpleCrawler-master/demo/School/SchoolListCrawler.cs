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
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 用于城市与区域代码初始化
    /// </summary>
    public class SchoolListCrawler : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/WorkPlanManage";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> schoolIdFilter=new BloomFilter<string>(8000000);
        private const string _DataTableName = "CitySchool_School";//存储的数据库表名

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
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCityRegion
        {
            get { return "CityRegionInfo_School"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCity
        {
            get { return "CityInfo_School"; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCityArea
        {
            get { return "CityAreaInfo_School"; }

        }
        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<BsonDocument> regionCityCodes = new List<BsonDocument>();
        List<BsonDocument> areaCityCodes = new List<BsonDocument>();
        

        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public SchoolListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }
        Dictionary<string,string> jiaoyuCategoryDic = new Dictionary<string, string>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =5;
            Settings.CurWebProxy = GetWebProxy();
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            
            Console.WriteLine("正在初始化选择url队列");
            jiaoyuCategoryDic.Add("youeryuan", "幼儿园");
            jiaoyuCategoryDic.Add("xiaoxue", "小学");
            jiaoyuCategoryDic.Add("daxue", "大学");
            jiaoyuCategoryDic.Add("zhongxue", "中学");
            jiaoyuCategoryDic.Add("zhixiao", "职校/中专");

            jiaoyuCategoryDic.Add("zhiye", "职业培训");
            jiaoyuCategoryDic.Add("IT", "IT培训");
            jiaoyuCategoryDic.Add("liuxue", "留学");
            jiaoyuCategoryDic.Add("MBA", "MBA培训");
            jiaoyuCategoryDic.Add("meifa", "美发培训");
            jiaoyuCategoryDic.Add("taiquandao", "跆拳道");
            jiaoyuCategoryDic.Add("xiaoyuzhong", "小语种培训");
            jiaoyuCategoryDic.Add("chengrenjiaoyu", "成人教育");
            jiaoyuCategoryDic.Add("zaojiaozhongxin", "早教中心");
            jiaoyuCategoryDic.Add("waiyu", "外语培训");
            jiaoyuCategoryDic.Add("zhongxiaoxuefudao", "中小学辅导");
            jiaoyuCategoryDic.Add("yishu", "艺术培训");
            jiaoyuCategoryDic.Add("jiaxiao", "驾校");
            jiaoyuCategoryDic.Add("guojixuexiao", "国际学校");
            jiaoyuCategoryDic.Add("kaoyan", "考研培训");
            jiaoyuCategoryDic.Add("leqi", "乐器培训");
            var allSchoolList = dataop.FindAll(DataTableName).SetFields("schoolId").ToList();
            foreach (var shchool in allSchoolList)
            {
                if (!schoolIdFilter.Contains(shchool.Text("schoolId")))
                {
                    schoolIdFilter.Add(shchool.Text("schoolId"));
                }
            }
            cityUrlList = dataop.FindAll(DataTableNameCity).ToList();//城市url
              regionCityCodes = dataop.FindAll(DataTableNameCityRegion).SetFields("pinyin","cityCode","name").ToList();//区
              areaCityCodes = dataop.FindAll(DataTableNameCityArea).SetFields("pinyin","regionCode","cityCode", "name").ToList();//县市
            var deleteRegionCode = areaCityCodes.Select(c => c.Text("regionCode")).Distinct().ToList();
            var hitNoRegionCityCodes = regionCityCodes.Where(c => !deleteRegionCode.Contains(c.Text("pinyin"))).ToList();
            foreach (var areaObj in areaCityCodes)
            {
                foreach (var cat in jiaoyuCategoryDic.Keys)
                {
                    var url = string.Format("http://www.todgo.com/{0}/{1}/{2}/?isArea=1", areaObj.Text("cityCode"), areaObj.Text("pinyin"), cat);
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                }
            }

            foreach (var reguibCode in hitNoRegionCityCodes)
            {
                foreach (var cat in jiaoyuCategoryDic.Keys)
                {
                    var url = string.Format("http://www.todgo.com/{0}/{1}/{2}/", reguibCode.Text("cityCode"), reguibCode.Text("pinyin"), cat);
                    UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });
                }
            }
            //Settings.RegularFilterExpressions.Add(@".*?market/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}).html");
            //Settings.RegularFilterExpressions.Add(@".*?data/land.*?.html");
            //广州_440105________1_1.html
            //Settings.RegularFilterExpressions.Add(@".*?data/land/.*?_.*?________.*?_1.html");
            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/CitySelect.aspx"));
            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("zluckymn模拟登陆失败");
            }

        }
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
        public string GetWebProxyString()
        {
            return string.Format("{0}:{1}@{2}:{3}", "H1538UM3D6R2133P", "511AF06ABED1E7AE", "proxy.abuyun.com", "9010");
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

        private bool hasExistObj(string guid)
        {
            return dataop.FindCount(DataTableName, Query.EQ("schoolId", guid)) > 0;
        }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var strSplitArray = args.Url.Replace("http://", "").Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (strSplitArray.Length < 4)
            {
                Console.WriteLine("url出错{0}", args.Url);
                return;
            }
            var cityCode = strSplitArray[1];
            var distionCode = strSplitArray[2];
            var regionCode = string.Empty;
            var areaCode = string.Empty;
            var catCode = strSplitArray[3];
            var isArea = GetUrlParam(args.Url, "isArea");//是否最下级的县市
            #region  初始化regionCode
            if (isArea == "1")// 代表最下级的area
            {
                var hitArea = areaCityCodes.Where(c => c.Text("pinyin") == distionCode).FirstOrDefault();
                if (hitArea != null)
                {
                    regionCode = hitArea.Text("regionCode");
                    areaCode = distionCode;
                }
                else
                {
                    Console.WriteLine("查找不到所属区域" + args.Url);
                    return;
                }
            }
            else
            {
                regionCode = distionCode;
            }
            #endregion 

            var firstDiv = htmlDoc.GetElementbyId("list");
            if (firstDiv == null)
            {
                Console.Write("数据异常"+args.Url);
                var url = string.Format("http://www.todgo.com/{0}/{1}/{2}/?isArea=1",cityCode,cityCode+areaCode,catCode);
                UrlQueue.Instance.EnQueue(new UrlInfo(url));
                return;
            }

            var pageCount = 0;//2/3代表2页
            var pageIndex = 0;//2/3代表2页
            #region 当前页数
            var sNode = htmlDoc.DocumentNode.SelectSingleNode("//s");
            if (sNode != null)
            {
                var pageTextArray = sNode.InnerText.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                if (pageTextArray.Length >= 2) {
                    if (int.TryParse(pageTextArray[1], out pageCount))
                    {
                        pageCount -= 0;
                        if (pageCount < 0)
                        {
                            pageCount = 0;
                        }
                    }
                    else
                    {
                        Console.WriteLine("pageCount页数转换出错");
                    }
                    if (int.TryParse(pageTextArray[0], out pageIndex))
                    {
                        
                    }
                    else
                    {
                        Console.WriteLine("pageIndex页数转换出错");
                    }
                }
            }
            #endregion

            var searchList = firstDiv.SelectNodes("./div[@class='list_row']");//城市筛选
             
            if (searchList != null&& searchList.Count()>0)
            {
                
                foreach (var  listNode in searchList)
                {
                   
                    var hrefNode = listNode.SelectSingleNode("./div[@class='info']/h2/a");
                    var href = hrefNode.Attributes["href"].Value;
                    var schoolId = GetSchoolKey(href);
                    if (schoolIdFilter.Contains(schoolId)|| hasExistObj(schoolId))
                    {
                       
                        Console.Write("已添加");
                        continue;
                    }
                    schoolIdFilter.Add(schoolId);
                    var name = hrefNode.InnerText;
                    var detailInfoNode = listNode.SelectSingleNode("./div[@class='info']/p");
                    var detailInfo = detailInfoNode.InnerText;
                    var address = Toolslib.Str.Sub(detailInfo, "地址：", "电话").Replace("\"","").Trim();
                    var tel = Toolslib.Str.Sub(detailInfo, "电话：", "\t").Replace("\"", "").Trim();
                    var doc = new BsonDocument().Add("schoolId", schoolId).Add("name", name.Trim()).Add("href", href.Trim());
                    doc.Add("address", address);
                    doc.Add("tel", tel);
                    doc.Add("cityCode", cityCode);
                    doc.Add("regionCode", regionCode);
                    doc.Add("catCode", catCode);
                    if(jiaoyuCategoryDic.ContainsKey(catCode))
                    doc.Add("catName", jiaoyuCategoryDic[catCode]);
                    if (isArea == "1")
                    {
                        doc.Add("areaCode", areaCode);
                        var areaObj = areaCityCodes.Where(c => c.Text("pinyin") == areaCode&&c.Text("regionCode")==regionCode&&c.Text("cityCode")==cityCode).FirstOrDefault();
                        doc.Add("areaName", areaObj.Text("name"));
                    }
                    doc.Add("isArea", isArea);

                    var cityObj = cityUrlList.Where(c => c.Text("pinyin") == cityCode).FirstOrDefault();
                    doc.Add("cityName", cityObj.Text("name"));
                    var regionObj = regionCityCodes.Where(c => c.Text("pinyin") == regionCode&&c.Text("cityCode")==cityCode).FirstOrDefault();
                    doc.Add("regionName", regionObj.Text("name"));

                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = doc, Type = StorageType.Insert });

                }
                Console.WriteLine("添加{0}", searchList.Count() - 1);
            }

            //添加分页信息 第二页G1 pageCount=2
            if (pageIndex == 1&&pageCount>1&& searchList.Count>0)
            {
               
               
                for (var i = 2; i <= pageCount; i++)
                {
                    var curUrl = args.Url;
                    if (isArea == "1")
                    {
                        curUrl=args.Url.Replace("?isArea=1", "");
                    }
                    curUrl = string.Format("{0}g{1}_/", curUrl, i - 1);

                    if (isArea == "1")
                    {
                        curUrl = curUrl+"?isArea=1";
                    }
                    if (!filter.Contains(curUrl))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { });
                    }
                }
                Console.Write("添加页数{0}个last:{1}", pageCount, args.Url);
            }
            
        }

        /// <summary>
        /// /jiaoyu/1607709.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetSchoolKey(string url)
        {
            
            var endIndex = url.IndexOf(".");
            var index = url.LastIndexOf("/");
            var cityCode = string.Empty;
            if (index != -1 && endIndex != -1)
            {
                cityCode = url.Substring(index + 1, endIndex - index - 1);

            }
            return cityCode;
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetCityCode(string url)
        {
            url = url.Replace("jiaoyu/", "").Replace("http://www.todgo.com","");
            var endIndex = url.LastIndexOf("/");
            var index = url.IndexOf("/");
            var cityCode = string.Empty;
            if (index!=-1&&endIndex!=-1)
            {
                cityCode = url.Substring(index + 1, endIndex- index-1);

            }
            return cityCode;
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetRegionCode(string url,string cityCode)
        {
            var fixUrl = url.Replace(cityCode+"_", "");
            return GetCityCode(fixUrl);
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
             if (string.IsNullOrEmpty(args.Html)||args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
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
            Settings.SetUnviableIP(ipproxy);//设置为无效代理
            if (ipproxy != null)
            {
                DBChangeQueue.Instance.EnQueue(new StorageData()
                {
                    Name = "IPProxy",
                    Document = new BsonDocument().Add("status", "1"),
                    Query = Query.EQ("ip", ipproxy.IP)
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
