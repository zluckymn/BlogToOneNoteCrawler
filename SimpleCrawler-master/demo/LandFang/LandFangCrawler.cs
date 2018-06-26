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
    public class LandFangCrawler : ISimpleCrawler
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
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }

        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            //var ipProxyList = dataop.FindAllByQuery("IPProxy", Query.NE("status", "1")).ToList();
            //Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            //Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.ThreadCount = 10;
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            var hitUrl = dataop.FindAll(DataTableNameURL).SetFields("url").Select(c => c["url"]).ToList();//执行过的
            var cityUrlList = dataop.FindAll(DataTableNameCity).SetFields("url","type").ToList();//城市url
            var landUrlList = dataop.FindAll(DataTableName).SetFields("url").Select(c => c["url"]).ToList();//土地url
            Console.WriteLine("正在初始化布隆选择器");
            //foreach (var needUrl in hitUrl)
            //{
            //    var curUrl = needUrl.ToString();
            //    if (!filter.Contains(curUrl))
            //    {
            //        filter.Add(curUrl);// 防止执行2次
            //    }
            //}
            Console.WriteLine("正在添加地块初始化布隆选择器");
            foreach (var landUrl in landUrlList)
            {
                var curUrl = landUrl.ToString();
                if (!filter.Contains(curUrl))
                {
                    filter.Add(curUrl);// 防止执行2次
                }
            }

            Console.WriteLine("正在初始化选择url队列");

            foreach (var cityUrl in cityUrlList.Where(c=>c.Int("type")==2))
            {

                UrlQueue.Instance.EnQueue(new UrlInfo(cityUrl.Text("url")) { Depth = 1 });
            }


            Settings.RegularFilterExpressions.Add(@".*?market/(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1}).html");
            //Settings.RegularFilterExpressions.Add(@".*?data/land.*?.html");
            //广州_440105________1_1.html
            Settings.RegularFilterExpressions.Add(@".*?data/land/.*?_.*?________.*?_1.html");

            //Settings.HrefKeywords.Add(string.Format("/market/"));//先不加其他的
            //Settings.HrefKeywords.Add(string.Format("data/land/_________0_"));//先不加其他的
            //Settings.SeedsAddress.Add(string.Format("http://fdc.fang.com/data/land/440100_________0_1.html"));
            ////是否guid

            //Settings.RegularFilterExpressions.Add("xxxx");
            //Settings.HrefKeywords.Add(string.Format("market"));//先不加其他的
            //Settings.HrefKeywords.Add(string.Format("data/land"));//先不加其他的
            //Settings.HrefKeywords.Add(string.Format("/market/"));

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
            var curLandBson = new BsonDocument();
            var root = htmlDoc.GetElementbyId("printData1");
            if (root == null)
            {
                #region  手动添加页数
                 
                var pageCountHtmls = htmlDoc.DocumentNode.SelectNodes("//div[@class='search_tip']");
                foreach (var pageCountDiv in pageCountHtmls)
                {
                   var pageCountHtml = pageCountDiv.ChildNodes.Where(c => c.Name == "span").FirstOrDefault();
                    if (pageCountHtml == null) continue;
                    var pageCountContent = pageCountHtml.InnerText;
                    if (pageCountContent.Contains("共有"))
                    {
                        var contentCount = pageCountContent.Replace("共有", "").Replace("条信息", "");
                        var recordCount = 0;//总个数
                        if (int.TryParse(contentCount, out recordCount))
                        {
                            var pageCount = recordCount / 15;
                            if (pageCount <= 0)
                                pageCount = 1;
                            var _index = args.Url.LastIndexOf("_");
                            var preUrl = args.Url.Substring(0, _index);
                            var curIndex = args.Url.Substring(_index + 1, args.Url.Length- _index - 1).Replace(".html", "");
                            var curIndexInt = 1;//当前页码
                            int.TryParse(curIndex, out curIndexInt);
                            if (curIndexInt >= 2) return;//之前已经添加过了保存一次,需要考虑页面截获2页的与这里生成2页重复是否有问题，理论上没有问题

                            for (; curIndexInt <= pageCount; )//添加页数
                            {
                                var url = string.Format("{0}_{1}.html", preUrl, ++curIndexInt);
                                if (!filter.Contains(url)) { 
                                    UrlQueue.Instance.EnQueue(new UrlInfo(url));
                                    filter.Add(url);
                                    //return;//测试只添加一次
                                }
                            }

                        }
                        return;
                    }
                }
                #endregion
                return;
             
            }
            var land = root.SelectSingleNode("//div[@class='tit_box01']");
            var landName = GetInnerText(land);//地块名
            curLandBson.Add("name", landName);
            //编号
            var code = root.SelectSingleNode("//div[@class='menubox01 mt20']");
            var codeArray = GetStrSplited(GetInnerText(code));
            if (codeArray.Length >= 2)
            {
                curLandBson.Add(ValeFix(codeArray[0].Replace("\n", "")), ValeFix(codeArray[1]));
            }
            var landInfoTables = root.SelectNodes("//table[@class='tablebox02 mt10']");
            if (landInfoTables == null)
            {
                return;

            }
            foreach (var LandInfoTable in landInfoTables)
            {
                foreach (var table in LandInfoTable.ChildNodes.Where(c => c.InnerHtml.Contains("tr")))
                {
                    foreach (var tr in table.ChildNodes.Where(c => c.Name == "tr"))
                    {
                        foreach (var td in tr.ChildNodes.Where(c => c.Name == "td"))
                        {
                            var displayName = td.InnerText;
                            var valuArr = GetStrSplited(displayName);
                            if (valuArr.Length >= 2)
                            {
                                curLandBson.Set(ValeFix(valuArr[0]), ValeFix(valuArr[1]));
                            }

                        }
                    }

                }
            }
            curLandBson.Set("url", args.Url);
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curLandBson, Name = DataTableName, Type = StorageType.Insert });
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
