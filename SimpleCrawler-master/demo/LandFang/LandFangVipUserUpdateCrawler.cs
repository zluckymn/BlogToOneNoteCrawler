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
    /// 信息更新模拟用户登录信息更新，主要用于Vip查询
    /// </summary>
    public class LandFangVipUserUpdateCrawler : ISimpleCrawler
    {

       
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
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
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public LandFangVipUserUpdateCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            // Settings.IPProxyList.AddRange(ipProxyList.Select(c => new IPProxy(c.Text("ip"))).Distinct());
            // Settings.IPProxyList.Add(new IPProxy("1.209.188.180:8080"));
            Settings.IgnoreSucceedUrlToDB = true;
            Settings.ThreadCount = 1;
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url//         "中山", 
            var cityStr = "南京,苏州,常州,无锡,南通,西安,烟台,佛山,泉州,广州,深圳,成都,昆明,大连,青岛,哈尔滨,沈阳,日照, 南宁,武汉,长沙,合肥,济南,郑州,南昌,杭州,兰州,长春,海口,西宁,石家庄,宁波,贵阳,西宁,乌鲁木齐,呼和浩特,银川,拉萨,福州,厦门,东莞";
            var distinctAreaStr = cityStr.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            var orQuery = Query.In("所在地", distinctAreaStr.Select(c => (BsonValue)c));

#pragma warning disable CS0219 // 变量“partName”已被赋值，但从未使用过它的值
            var partName = "所在地";
#pragma warning restore CS0219 // 变量“partName”已被赋值，但从未使用过它的值
            //var orQuery = Query.In("地区", "天津");
             landUrlList = dataop.FindAllByQuery(DataTableName, Query.And(orQuery, Query.EQ("isDiamon", "1"), Query.Or(Query.EQ("竞得方", "******")))).Take(10000).ToList();//土地url
            //landUrlList = dataop.FindAllByQuery(DataTableName, Query.And(Query.EQ("isDiamon", "1"), Query.Or(Query.EQ("isTradeStatusChange", "1")))).Take(10000).ToList();//土地url
            //var allAccountList = dataop.FindAllByQuery(DataTableNameAccount,Query.EQ("userName", "18900372887")).SetFields("userName", "postData", "status", "passWord").ToList();
            var allAccountList = dataop.FindAll(DataTableNameAccount).ToList();
            Console.WriteLine("待处理数据{0}个", landUrlList.Count);
            foreach (var cityUrl in landUrlList)
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(cityUrl.Text("url")) { Depth = 1 });
            }
            Console.WriteLine("正在加载账号数据");
            foreach (var account in allAccountList.Where(c => c.Int("status") != 1&&c.Int("isVip")==1))
            {

                AccountQueue.Instance.EnQueue(account);
            }
          
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
            if (root == null) return;
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
            //确定对象
            var curLandObj = landUrlList.Where(c => c.Text("url") == args.Url).FirstOrDefault();
            if (curLandObj == null) return;
            var isUpdate = false;
            foreach (var LandInfoTable in landInfoTables)
            {
                foreach (var table in LandInfoTable.ChildNodes.Where(c => c.InnerHtml.Contains("tr")))
                {
                    foreach (var tr in table.ChildNodes.Where(c => c.Name == "tr"))
                    {
                        foreach (var td in tr.ChildNodes.Where(c => c.Name == "td"))
                        {


                            var displayName = td.InnerText;
                            var curValue = string.Empty;
                            var valuArr = GetStrSplited(displayName);
                            if (displayName.Contains("地区")&& !string.IsNullOrEmpty(curLandObj.Text("地区")))
                            {
                                continue;//不更新所在地，防止类似所在地被增城区被更新为增城区而非广州
                            }
                            if (displayName.Contains("所在地")&&!string.IsNullOrEmpty(curLandObj.Text("所在地")))
                            {
                                continue;//不更新所在地，防止类似所在地被增城区被更新为增城区而非广州
                            }
                            if (valuArr.Length >= 2)
                            {
                                curValue = ValeFix(valuArr[1]);
                                if (valuArr[0] == "四至")
                                {
                                    var title = string.Empty;
                                    if (td.Attributes.Contains("title"))
                                    {
                                        title = td.Attributes[title] != null ? td.Attributes[title].ToString() : string.Empty;
                                    }
                                    if (!string.IsNullOrEmpty(title))
                                    {
                                        curValue = title;
                                    }
                                }

                                if (curLandObj.Text(ValeFix(valuArr[0])) != curValue)
                                {
                                    curLandBson.Set(ValeFix(valuArr[0]), curValue);
                                    isUpdate = true;
                                }
                            }

                        }
                    }

                }
            }
            ///2016.6.20添加更新xy轴
            var xBenStr = "var pointX = ";
            var yBenStr = "var pointY = ";
            var xBeginIndex = args.Html.IndexOf(xBenStr);
            var yBeginIndex = args.Html.IndexOf(yBenStr);
            if (xBeginIndex != -1 && yBeginIndex != -1)
            {
                var xValue = GetXYValue(xBeginIndex + xBenStr.Length, hmtl.Length, hmtl);
                var yValue = GetXYValue(yBeginIndex + xBenStr.Length, hmtl.Length, hmtl);
                if (!string.IsNullOrEmpty(xValue) && !string.IsNullOrEmpty(yValue))
                    if (curLandObj.Text("x") != xValue && curLandObj.Text("y") != xValue)
                    {
                        isUpdate = true;
                        curLandBson.Add("x", xValue);
                        curLandBson.Add("y", yValue);
                    }

            }
           
            if (isUpdate)
            {
                if (curLandObj.Text("竞得方") != "******")
                {
                    curLandBson.Add("isDataUpdate", "1");//更新了数据
                }
                curLandBson.Add("isUserUpdated", "1");//更新了数据
                Console.WriteLine(string.Format("{0}更新", landName));
                // curLandBson.Set("url", args.Url);
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curLandBson, Name = DataTableName, Query = Query.EQ("url", args.Url), Type = StorageType.Update });
            }
            else
            {

                if (curLandObj.Text("needUpdate") == "1")
                {
                    var updateBosn=new BsonDocument().Add("needUpdate", "0");//更新了数据
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateBosn, Name = DataTableName, Query = Query.EQ("url", args.Url), Type = StorageType.Update });
                }
                if (args.Html.Contains("钻石会员"))
                {
                    Console.WriteLine("钻石会员专属");
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curLandBson.Add("isDiamon", "1"), Name = DataTableName, Query = Query.EQ("url", args.Url), Type = StorageType.Update });
                }
            }
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
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var curLandBson = new BsonDocument();
            var root = htmlDoc.GetElementbyId("printData1");
            if (root == null) return true;
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
            //之前似乎否已经登陆过了
            if (!string.IsNullOrEmpty(Settings.LoginAccount))
            {
                DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add(DataTableName + "status", "1"), Name = DataTableNameAccount, Query = Query.EQ("userName", Settings.LoginAccount), Type = StorageType.Update });
                StartDBChangeProcess();
            }
            var accountBson = AccountQueue.Instance.DeQueue();
            if (accountBson == null)
            {
                Console.WriteLine("账号已用完");
                Environment.Exit(0);
                return false;
            }

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
                        Allowautoredirect = true,
                    };

                    if (accountBson != null)
                    {
                        item.Postdata = accountBson.Text("postData");
                    }

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
                    if (accountBson != null)
                    {
                        var account = accountBson.Text("userName");
                        if (result.Html.IndexOf("Success") > 0)
                        {
                            Settings.SimulateCookies = cookie;//设置cookie值
                            Settings.LoginAccount = account;
                            Console.WriteLine(string.Format("{0}模拟登陆成功", account));
                            return true;
                        }
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("status", "1"), Name = DataTableNameAccount, Query = Query.EQ("userName", account), Type = StorageType.Update });
                        StartDBChangeProcess();
                    }
                    else
                    {

                        Settings.SimulateCookies = cookie;//设置cookie值
                        Console.WriteLine("登陆失败");
                        return false;
                    }
                    return false;
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (WebException ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {
                    IPInvalidProcess(ipProxy);
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
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
                    Document = new BsonDocument().Add(DataTableName + "_status", "1"),
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
