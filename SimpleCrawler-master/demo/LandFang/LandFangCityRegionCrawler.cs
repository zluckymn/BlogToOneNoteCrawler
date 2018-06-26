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
    public class LandFangCityRegionCrawler : ISimpleCrawler
    {

       
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private const string _DataTableName = "LandFang";//存储的数据库表明

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
        public string DataTableNameCity
        {
            get { return _DataTableName + "CityURL"; }

        }
        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangCityRegionCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }
       
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 5;
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
             cityUrlList = dataop.FindAll(DataTableNameCity).ToList();//城市url
          
            Console.WriteLine("正在初始化选择url队列");

            foreach (var cityUrl in cityUrlList)
            {

                UrlQueue.Instance.EnQueue(new UrlInfo(cityUrl.Text("url")) { Depth = 1 });
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
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var hitCityObj = cityUrlList.Where(c => c.Text("url") == args.Url).FirstOrDefault();
            if (hitCityObj == null)
                hitCityObj = new BsonDocument();
            var searchList = htmlDoc.DocumentNode.SelectNodes("//li[@class='search_city']");//城市筛选
            var cityName = string.Empty;
            var cityCode = string.Empty;
            if (searchList != null&& searchList.Count()>0)
            {
                var cityLi = searchList.Where(c => c.InnerText.Contains("城")&& c.InnerText.Contains("市")).FirstOrDefault();
             
                if (cityLi != null)
                {
                  
                    var hitCity= cityLi.SelectSingleNode("./span/span/a[@class='orange bold']");//城市筛选
                    if (hitCity != null)
                    {
                        cityName = hitCity.InnerText;
                        cityCode = GetCityCode(args.Url);
                        if (!string.IsNullOrEmpty(cityName))
                        {
                            if(hitCityObj.Text("name")!= cityName) { 
                            hitCityObj.Set("name", cityName);
                            }
                            if (hitCityObj.Text("code") != cityCode)
                            {
                                hitCityObj.Set("code", cityCode);
                            }
                            //curLandBson.Add("type", "0");
                            if (string.IsNullOrEmpty(hitCityObj.Text("_id"))) {
                                hitCityObj.Set("type", "1");
                                hitCityObj.Add("url", args.Url);
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = hitCityObj, Name = DataTableNameCity, Type = StorageType.Insert });
                            } else {
                                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = hitCityObj, Query = Query.EQ("url", args.Url), Name = DataTableNameCity, Type = StorageType.Update });
                            }
                           
                        }
                        //获取当前所有县
                        
                    }
                    var regionLi = searchList.Where(c => c.InnerText.Contains("区")&&c.InnerText.Contains("县")).FirstOrDefault();
                    if (regionLi != null)
                    {
                        var childRegionDD = regionLi.ChildNodes.Where(c => c.InnerText.Contains("全部")).FirstOrDefault();
                        if (childRegionDD != null)
                        {
                            var regionNumAList = childRegionDD.SelectNodes("./span/a").Where(c => c.InnerText != "全部").ToList();
                            foreach (var region in regionNumAList)
                            {
                                var regionName = region.InnerText;
                               // var regionBosn = new BsonDocument();
                                var url = region.Attributes["href"];
                                if (url != null) {
                                  
                                    var fixUrl = string.Format("http://fdc.fang.com{0}", url.Value);
                                    var hitRegionObj = cityUrlList.Where(c => c.Text("url") == fixUrl).FirstOrDefault();
                                    if (hitRegionObj == null)
                                        hitRegionObj = new BsonDocument();
                                    var regionCode=GetRegionCode(fixUrl,cityCode);
                                    if (hitRegionObj.Text("name") != regionName)
                                    {
                                        hitRegionObj.Set("name", regionName);
                                    }
                                    if (hitRegionObj.Text("code") != regionCode)
                                    {
                                        hitRegionObj.Set("code", regionCode);
                                    }
                                    if (hitRegionObj.Text("cityCode") != cityCode)
                                    {
                                        hitRegionObj.Set("cityCode", cityCode);
                                    }
                                    //curLandBson.Add("type", "0");
                                    if (string.IsNullOrEmpty(hitRegionObj.Text("_id")))
                                    {
                                        hitRegionObj.Set("type", "2");
                                        hitRegionObj.Set("url", fixUrl);
                                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = hitRegionObj, Name = DataTableNameCity, Type = StorageType.Insert });
                                    }
                                    else
                                    {
                                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = hitRegionObj, Query = Query.EQ("url", fixUrl), Name = DataTableNameCity, Type = StorageType.Update });
                                    }
  
                                    //DBChangeQueue.Instance.EnQueue(new StorageData() { Document = regionBosn,   Name = DataTableNameCity, Type = StorageType.Insert });
                                }
                            }
                        }
                    }
                }

            }
            
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetCityCode(string url)
        {
            var index = url.LastIndexOf("/");
            var endIndex = url.IndexOf("_");
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
            //if (args.Html.Contains("错误"))//需要编写被限定IP的处理
            //{
            //    return true;
            //}
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
            IPProxy ipProxy = null;

            HttpHelper http = new HttpHelper();
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
                    HttpItem item = new HttpItem()
                    {
                        URL = "https://passport.fang.com/login.api",//URL     必需项
                        Encoding = null,//编码格式（utf-8,gb2312,gbk）     可选项 默认类会自动识别
                                        //Encoding = Encoding.Default,
                        Method = "post",//URL     可选项 默认为Get
                                        //Timeout = 100000,//连接超时时间     可选项默认为100000
                                        //ReadWriteTimeout = 30000,//写入Post数据超时时间     可选项默认为30000
                                        //IsToLower = false,//得到的HTML代码是否转成小写     可选项默认转小写
                                        //Cookie = "",//字符串Cookie     可选项
                        UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko",//用户的浏览器类型，版本，操作系统     可选项有默认值
                        Accept = "text/html, application/xhtml+xml, */*",//    可选项有默认值
                        ContentType = "application/x-www-form-urlencoded",//返回类型    可选项有默认值
                        Referer = "https://passport.fang.com/",//来源URL     可选项
                        Postdata = "Uid=luckymn&Pwd=1c523e9b2109407d0857676dfc20af997c14791f495ec8676979628bfef0762ce2679e2f4770d536526bcf00639ec803539f02c54387fbd4a3f159ec5a6185cd46cb139b5c2696c269bce5b7f9c00fb3a9bc58e815773c227b54d4570da0cbee50b47b29c363d398791d3065c0343494aebaa925313e705fd514898e56c2df29&Service=soufun-passport-web&IP=&VCode=&AutoLogin=1",
                        Allowautoredirect = true,
                    };

                    if (ipProxy != null)
                    {
                        item.ProxyIp = ipProxy.IP;
                    }
                    Console.WriteLine(string.Format("尝试登陆{0}", Settings.curIPProxy != null ? Settings.curIPProxy.IP : string.Empty));
                    HttpResult result = http.GetHtml(item);
                    string cookie = string.Empty;
                    foreach (CookieItem s in HttpCookieHelper.GetCookieList(result.Cookie))
                    {
                        //if (s.Key.Contains("24a79_"))
                        {
                            cookie += HttpCookieHelper.CookieFormat(s.Key, s.Value);
                        }
                    }
                    if (result.Html.IndexOf("luckymn") > 0)
                    {
                        Settings.SimulateCookies = cookie;//设置cookie值
                        Console.WriteLine("zluckymn模拟登陆成功");
                        return true;
                    }
                    return false;
                }
                catch (WebException ex)
                {
                    IPInvalidProcess(ipProxy);
                }
                catch (Exception ex)
                {
                    IPInvalidProcess(ipProxy);
                }

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
