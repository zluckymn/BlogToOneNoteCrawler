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
    public class SoHuBuildingListCrawler : ISimpleCrawler
    {

        
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> schoolIdFilter=new BloomFilter<string>(8000000);
        private const string _DataTableName = "Focus_Project";//存储的数据库表名

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
            get { return "FocusCity"; }

        }

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCityRegion
        {
            get { return "CityRegionInfo_MT"; }

        }
        private const string StartUrlTable = "Focus_StartUrl";
        private const string ProjectTable = "Focus_Project";
        private const string ProjectPriceTable = "Focus_ProjectPrice";
        private const string ProjectDetail = "Focus_ProjectDetail";
        private const string ProjectDetailPrice = "Focus_DetailPrice";
        private const string PreSaleTable = "Focus_PreSale";
        private const string ProjectOpen = "Focus_ProjectOpen";
        private const string ProjectHouseTable = "Focus_House";
        private const string ProjectHouseStartUrlTable = "Focus_StartHouseUrl";
        private const string LogTable = "Focus_Log";


        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<BsonDocument> regionCityCodes = new List<BsonDocument>();
        List<BsonDocument> areaCityCodes = new List<BsonDocument>();
        

        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public SoHuBuildingListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }

        private void InitialUrl(string cityUrl)
        {
            var url = GetValidUrl(cityUrl);

            UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });//第一页
            
        }
        /// <summary>
        /// 通过url生成待爬取队列
        /// </summary>
        /// <param name="cityUrl"></param>
        private string GetValidUrl(string cityUrl)
        {
            Console.WriteLine("开始处理连接:{0}",cityUrl);
            HttpHelper hh = new HttpHelper();
            //https://xm.focus.cn/loupan/

            HttpItem item = new HttpItem() {
                URL= cityUrl, Method="get"
            };
            item.WebProxy = Settings.CurWebProxy;
            var result = hh.GetHtml(item);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(result.Html);
                Console.WriteLine("获取过滤非售完的连接");
                var aNodes = htmlDoc.DocumentNode.SelectNodes("//a[@rel='nofollow']");
                if (aNodes != null)
                {
                    var hitANode = aNodes.Where(c => c.InnerText.Contains("售完")).FirstOrDefault();
                    if (hitANode != null)
                    {
                        var hrefAttr = hitANode.Attributes["href"];
                        if (hrefAttr != null)
                        {
                            var findUrl = hrefAttr.Value;
                            return findUrl;
                        }
                    }
                }
                
            }
            return string.Empty;
            

        }
        Dictionary<string,string> cityGuidDic = new Dictionary<string, string>();
        private List<BsonDocument> allCityList = new List<BsonDocument>();
        private List<BsonDocument> curCityRegionList = new List<BsonDocument>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //种子地址需要加布隆过滤
            cityGuidDic.Add("厦门", "");
            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =2;
            Settings.CurWebProxy = Settings.CurWebProxy;
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("请输入城市名称");
 
            var cityName = Console.ReadLine();
            allCityList = dataop.FindAll(DataTableNameCity).ToList();
            var hitCityObj = allCityList.Where(c=>c.Text("name")== cityName).FirstOrDefault();

            var curCity = dataop.FindOneByQuery(DataTableNameCityRegion, Query.Or(Query.EQ("name", cityName+"市"),Query.EQ("name", cityName)));
            if (curCity != null)
            {
               var curCityRegionIds = dataop.FindAllByQuery("CityRegionRelationInfo_MT", Query.EQ("parentId", curCity.Text("id")));
                curCityRegionList = dataop.FindAllByQuery(DataTableNameCityRegion, Query.In("id", curCityRegionIds.Select(c => (BsonValue)c.Text("cityId")))).ToList();
            }
            if (curCityRegionList.Count <= 0)
            {
                Console.WriteLine("对应城市不存在");
                return;
            }
            if (hitCityObj != null&&!string.IsNullOrEmpty(hitCityObj.Text("guid")))
            {
                InitialUrl(hitCityObj.Text("href"));
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
                Console.WriteLine("初始化失败");
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
            proxy.Address = new Uri(string.Format("{0}:{1}", ConstParam.proxyHost, ConstParam.proxyPort));
            proxy.Credentials = new NetworkCredential(ConstParam.proxyUser, ConstParam.proxyPass);
            return proxy;
        }
        public string GetWebProxyString()
        {
            return string.Format("{0}:{1}@{2}:{3}", ConstParam.proxyUser, ConstParam.proxyPass, "proxy.abuyun.com", ConstParam.proxyPort);
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
            var preUrl = Toolslib.Str.Sub(args.Url, "https://", "loupan");
            var hitCityObj = allCityList.Where(c => c.Text("href").Contains(preUrl)).FirstOrDefault();
            if (hitCityObj == null)
            {
                Console.WriteLine("该url无对应城市", args.Url);
                return;
            }
            ParseProjectList_New(hitCityObj.Text("guid"), "", args.Html);

            #region 获取分页

             var aNodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (!args.Url.Contains("/loupan/p"))
            {
                GetNextPage(args.Url,aNodes);
            }
            #endregion


        }
        /// <summary>
        /// 获取分页
        /// </summary>
        private void GetNextPage(string url,HtmlNodeCollection aNodes)
        {
              if (!string.IsNullOrEmpty(url)&&aNodes != null)
              {
                        var hitANode = aNodes.Where(c => c.InnerText.Contains("末页")).FirstOrDefault();
                        if (hitANode != null)
                        {
                            //https://xm.focus.cn/loupan/p38/?saleStatus=6
                            var hrefAttr = hitANode.Attributes["href"];
                            if (hrefAttr != null)
                            {
                                var findUrl = hrefAttr.Value;
                                var getPageNum = Toolslib.Str.Sub(findUrl, "loupan/p", "/");
                                var pageNum = 0;
                                if (int.TryParse(getPageNum, out pageNum))
                                {
                                   // UrlQueue.Instance.EnQueue(new UrlInfo(url) { Depth = 1 });//第一页
                                    var oldPageText = string.Format("/loupan/p{0}", getPageNum);
                                    //获取当前页数
                                    for (var i = 2; i <= pageNum; i++)
                                    {
                                        //url https://xm.focus.cn/loupan/?saleStatus=6
                                        var newPageText = string.Format("/loupan/p{0}", i);
                                        var resultUrl = findUrl.Replace(oldPageText, newPageText);
                                        if(!filter.Contains(resultUrl))
                                        UrlQueue.Instance.EnQueue(new UrlInfo(resultUrl) { Depth = 1 });

                                    }
                                }
                                else
                                {
                                    Console.WriteLine("无法获取页数");
                                }
                            }
                        }
             }
 
        }

        /// <summary>
        /// 保存记录
        /// </summary>
        /// <param name="projectDocs"></param>
        /// <returns></returns>
        private InvokeResult SaveProject(List<BsonDocument> projectDocs)
        {
            List<BsonDocument> addDocs = new List<BsonDocument>();
            foreach (var doc in projectDocs)
            {
           
                var oldProjectCount = dataop.FindCount(ProjectTable, Query.EQ("detailUrl", doc["detailUrl"].AsString));
                if (oldProjectCount>0)
                {
                    //this.dataHelper.Update(ProjectTable, Query.EQ("detailUrl", doc["detailUrl"].AsString), doc);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectTable, Document = doc, Query = Query.EQ("detailUrl", doc["detailUrl"].AsString), Type = StorageType.Update });
                    Console.WriteLine("更新数据");
                }
                else
                {
                    
                    doc.Add("homeStatus", 0);
                    doc.Add("priceStatus", 0);
                    doc.Add("detailStatus", 0);
                    addDocs.Add(doc);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectTable, Document = doc, Type = StorageType.Insert });
                }
            }
            
            return new InvokeResult() { Status = Status.Successful };
        }

        /// <summary>
        /// 解析项目列表页面
        /// </summary>
        /// <param name="productType"></param>
        /// <param name="htmlString"></param>
        /// <returns></returns>
        private InvokeResult ParseProjectList_New(string cityGuid,string productType, string htmlString)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString);
            HtmlNode rootNode = document.DocumentNode;
            HtmlNodeCollection nodes = rootNode.SelectNodes("//div[@class='s-lp-all ']");
            List<BsonDocument> projectDocs = new List<BsonDocument>();
            bool isSuccessful = true;
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    try
                    {
                        BsonDocument doc = new BsonDocument();
                        HtmlNode tempNode = node.SelectSingleNode("div[@class='list']/div[@class='txt-center']/div[@class='title']");
                        HtmlNode tn = null;
                        #region 获取项目名称、业态、销售状态
                        if (tempNode != null)
                        {
                            #region 项目名及链接
                            tn = tempNode.SelectSingleNode("a");
                            if (tn != null)
                            {
                                doc.Add("name", tn.InnerText.Trim());
                                if (tn.Attributes.Contains("href") == true)
                                {
                                    string detailUrl = tn.Attributes["href"].Value.Trim();
                                    doc.Add("detailUrl", detailUrl);
                                    #region 解析项目ID
                                    string[] tempArr = detailUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    string fileName = tempArr[tempArr.Length - 1];
                                    doc.Add("projId", fileName.Replace(".html", ""));
                                    #endregion
                                }
                            }
                            #endregion

                            #region 销售状态
                            tn = tempNode.SelectSingleNode("span");
                            if (tn != null)
                            {
                                doc.Add("saleStatus", tn.InnerText.Trim());
                            }
                            #endregion

                            #region 项目业态
                            tn = tempNode.SelectSingleNode("em");
                            if (tn != null)
                            {
                                string tmpString = tn.InnerText.Replace("[", "").Replace("]", "").Trim();
                                string[] arrProductType = (from m in tmpString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                           select m.Trim()).ToArray();
                                doc.Add("productType", string.Join(",", arrProductType));
                            }
                            #endregion
                        }
                        #endregion

                        #region 获取项目地址和所在区
                        tempNode = node.SelectSingleNode("div[@class='list']/div[@class='txt-center']/p[@class='location']");
                        if (tempNode != null)
                        {

                            #region 项目地址和所在区
                            tn = tempNode.SelectSingleNode("span");
                            if (tn != null)
                            {
                                doc.Add("address", tn.InnerText.Trim());
                                string[] arr = tn.InnerText.Trim().Trim().Split(new string[] { " ", " " }, StringSplitOptions.RemoveEmptyEntries);
                                if (arr.Length > 0)
                                {
                                    doc.Add("region", arr[0]);
                                    //是否匹配显示
                                    var hitRegion = curCityRegionList.Where(c => c.Text("name").Contains(arr[0])).Count() > 0;
                                    if (hitRegion==false&& curCityRegionList.Count()>1)
                                    {
                                        Console.WriteLine("{0}不是{1}的县市", arr[0], cityGuid);
                                        continue;
                                    }

                                }
                            }
                            #endregion

                            #region 地图经纬度
                            tn = tempNode.SelectSingleNode("a");
                            if (tn != null)
                            {
                                if (tn.Attributes.Contains("data-lon") == true)
                                {
                                    doc.Add("x", tn.Attributes["data-lon"].Value.Trim());
                                }
                                if (tn.Attributes.Contains("data-lat") == true)
                                {
                                    doc.Add("y", tn.Attributes["data-lat"].Value.Trim());
                                }
                            }
                            #endregion
                        }
                        #endregion

                        if (doc.Elements.Count() > 0)
                        {
                            //厦门站包含泉州漳州楼盘
                            var xmDistionct = new string[] { "思明", "翔安", "湖里", "同安", "海沧", "集美" };
                            if (cityGuid == "XM-44FBDA82-53FB-4206-B209-2B394B54F1FF" && !xmDistionct.Contains(doc.Text("region")))
                            {
                                continue;
                            }
                            doc.Add("cityGuid", cityGuid);
                            doc.Add("dataSource", "focus");
                            doc.Add("isNeedUpdate","1");//需要爬取详细信息
                            projectDocs.Add(doc);
                        } else
                        {
                            Console.WriteLine("无获取到任何数据");
                        }
                    }
                    catch (Exception ex)
                    {
                        isSuccessful = false;
                        Console.WriteLine("解析起始页出现异常:" + ex.Message);
                    }
                }
            }
            if (projectDocs.Count() > 0)
            {
                Console.WriteLine("获取项目：" + projectDocs.Count().ToString() + " 并开始保存到数据库中");
                this.SaveProject(projectDocs);
            }
            result.Status = isSuccessful == true ? Status.Successful : Status.Failed;
            return result;
        }


        /// <summary>
        /// 解析项目列表页面
        /// </summary>
        /// <returns></returns>
        private InvokeResult ParseProjectList(string cityGuid, string productType, string htmlString)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Failed };
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString);
            HtmlNode rootNode = document.DocumentNode;
            HtmlNode node = rootNode.SelectSingleNode("//div[@class='s-lp-list']");
            List<BsonDocument> projectDocs = new List<BsonDocument>();
            bool isSuccessful = true;
            if (node != null)
            {
                HtmlNodeCollection childNodes = node.SelectNodes("div[@class='lp-list-li _item_li']");
                if (childNodes == null)
                {

                    return result;
                }
                foreach (var child in childNodes)
                {
                    try
                    {
                        BsonDocument doc = new BsonDocument();
                        HtmlNode tempNode = null;
                        if (child.Attributes.Contains("projId") == true)
                        {
                            doc.Add("projId_key", child.Attributes["projId"].Value.Trim());
                        }
                        if (child.Attributes.Contains("groupId") == true)
                        {
                            doc.Add("groupId_key", child.Attributes["groupId"].Value.Trim());
                            doc.Add("projId", child.Attributes["groupId"].Value.Trim());
                        }
                        #region 项目名称、项目详情URL
                        tempNode = child.SelectSingleNode("div/div/div[@class='s-lp-txt-center']/div/a");
                        if (tempNode != null)
                        {
                            doc.Add("name", tempNode.InnerText.Trim());
                            if (tempNode.Attributes.Contains("href") == true)
                            {
                                doc.Add("detailUrl", tempNode.Attributes["href"].Value.Trim());
                            }
                        }
                        #endregion

                        #region 项目销售状态
                        tempNode = child.SelectSingleNode("div/div/div[@class='s-lp-txt-center']/div/span[@class='lp-state zs']");
                        if (tempNode != null)
                        {
                            doc.Add("saleStatus", tempNode.InnerText.Trim());
                        }
                        #endregion

                        #region 项目业态
                        tempNode = child.SelectSingleNode("div/div/div[@class='s-lp-txt-center']/div/em[@class='lp-type']");
                        if (tempNode != null)
                        {
                            string tmpString = tempNode.InnerText.Replace("[", "").Replace("]", "").Trim();
                            string[] arrProductType = (from m in tmpString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                                       select m.Trim()).ToArray();
                            doc.Add("productType", string.Join(",", arrProductType));
                        }
                        #endregion

                        #region 获取项目地址和所在区
                        tempNode = child.SelectSingleNode("div/div/div[@class='s-lp-txt-center']/p[1]/span");
                        if (tempNode != null)
                        {
                            if (tempNode.Attributes.Contains("title") == true)
                            {
                                doc.Add("address", tempNode.Attributes["title"].Value.Trim());
                                string[] arr = tempNode.Attributes["title"].Value.Trim().Split(new string[] { " ", " " }, StringSplitOptions.RemoveEmptyEntries);
                                if (arr.Length > 0)
                                {
                                    doc.Add("region", arr[0]);
                                }

                            }
                        }
                        #endregion

                        if (doc.Elements.Count() > 0)
                        {
                            doc.Add("cityGuid", cityGuid);
                            doc.Add("dataSource","focus");
                            projectDocs.Add(doc);
                        }
                    }
                    catch (Exception ex)
                    {
                        isSuccessful = false;
                        Console.WriteLine("解析起始页出现异常:" + ex.Message);
                    }
                }
            }
            if (projectDocs.Count() > 0)
            {
                Console.WriteLine("获取项目：" + projectDocs.Count().ToString() + " 并开始保存到数据库中");

                this.SaveProject(projectDocs);
            }
            result.Status = isSuccessful == true ? Status.Successful : Status.Failed;
            return result;
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
