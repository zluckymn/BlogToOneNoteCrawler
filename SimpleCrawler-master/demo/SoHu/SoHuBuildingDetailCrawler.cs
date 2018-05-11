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
    public class SoHuBuildingDetailCrawler : ISimpleCrawler
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
        public SoHuBuildingDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =10;
            Settings.CurWebProxy = Settings.CurWebProxy;
            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");
            
            //var allNeedDataList = dataop.FindAllByQuery(DataTableName,Query.Exists("priceText", false)).ToList();
            var allNeedDataList = dataop.FindAllByQuery(DataTableName, Query.NE("isUpdate", "2")).SetFields("detailUrl", "projId","cityGuid").ToList();
            foreach (var data in allNeedDataList)
            {
                var indexUrl = data.Text("detailUrl");
                var detailUrl = indexUrl.Replace(".html", "/xiangqing.html");
                //if (!filter.Contains(indexUrl))//详情添加
                //{
                //    UrlQueue.Instance.EnQueue(new UrlInfo(indexUrl) { Depth = 1 , Authorization=data.Text("projId")});
                //}
                if (!filter.Contains(detailUrl))//详情添加
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(detailUrl) { Depth = 1, Authorization = data.Text("projId") });
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
                Console.WriteLine("初始化失败");
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

        private bool hasExistObj(string guid)
        {
           
            return dataop.FindCount(ProjectDetail, Query.EQ("projId", guid)) > 0;
        }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var projId = args.urlInfo.Authorization;
            if (string.IsNullOrEmpty(projId))
            {
                return;
            }
            if (args.Url.Contains("xiangqing"))
            {
                var newDetailDoc = new BsonDocument();
                newDetailDoc.Add("projId", projId);
                ParseProjectDetailPage(args.Html, newDetailDoc);
            }
            else
            {
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(args.Html);
                HtmlNode rootNode = document.DocumentNode;
                var updateBson = new BsonDocument();
                ///获取价格
                var priceDiv = rootNode.SelectSingleNode("//div[@class='mli-item mi-line loupan-price-tag']");
                if (priceDiv != null)
                {
                    var priceText = priceDiv.InnerText.Trim();
                    var validDataBeginTxt ="(" +Toolslib.Str.Sub(priceText, "(", ")")+")";
                    updateBson.Add("priceText", priceText.Replace(validDataBeginTxt,""));
                }
                var telDiv = document.GetElementbyId("hDperson");
                if (telDiv != null)
                {
                    var hitDivTxt = telDiv.InnerText.Trim();
                    updateBson.Add("salePhoneNum", hitDivTxt);
                }
                //获取
                if (hasExistObj(projId))
                {
                   DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectDetail, Document = updateBson, Query = Query.EQ("projId", projId), Type = StorageType.Update });
                }
                else
                {
                    //DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectDetail, Document = updateBson, Type = StorageType.Insert });
                }
            }
       }
        #region 通过数据节点获取对应信息数据
        /// <summary>
        /// 通过数据节点获取对应信息数据
        /// </summary>
        /// <param name="tableNode"></param>
        /// <param name="curBsondocument"></param>
        /// <returns></returns>
        private BsonDocument InitialBuildingDoc(HtmlNode tableNode, BsonDocument curBsondocument)
        {
            if (tableNode != null)
            {
                foreach (var dataRow in tableNode.ChildNodes.Where(c=>c.Name=="tbody").FirstOrDefault().ChildNodes.Where(c => c.Name == "tr"))
                {
                    var dataTdList = dataRow.ChildNodes.Where(c => c.Name == "td");
                    var index = 1;
                    var curColumnName = string.Empty;
                    var curColumnValue = string.Empty;
                    foreach (var td in dataTdList)
                    {
                        if (index % 2 != 0)
                        {
                            curColumnName = td.InnerText.Replace(":","").Replace("：", "").Trim();
                        }
                        if (index % 2 == 0)
                        {
                            curColumnValue = td.InnerText.Replace(":", "").Replace("：", "").Trim();
                            curBsondocument.Set(curColumnName, curColumnValue);
                            curColumnName = string.Empty;
                            curColumnValue = string.Empty;
                        }
                        index++;
                    }
                }
            }
            return curBsondocument;
        }
        #endregion
        #region 楼盘详情

        /// <summary>
        /// 文字价格转换
        /// </summary>
        /// <returns></returns>
        private int PriceParseToInt(string priceText)
        {
            var price = 0;
            priceText = priceText.Replace("￥", "").Replace("元/㎡", "").Replace(",", "").Replace("\"", "");
            if (int.TryParse(priceText, out price))
            {
                return price;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 解析项目详情
        /// </summary>
        /// <param name="htmlString"></param>
        /// <param name="doc"></param>
        private void ParseProjectDetailPage(string htmlString, BsonDocument doc)
        {
            InvokeResult result = new InvokeResult() { Status = Status.Successful };
            BsonDocument detailDoc = new BsonDocument();
            List<BsonDocument> detailPriceDocs = new List<BsonDocument>();
            List<BsonDocument> openDocs = new List<BsonDocument>();
            List<BsonDocument> preSaleDocs = new List<BsonDocument>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString);
            HtmlNode rootNode = document.DocumentNode;
            #region 提取基本信息
            var baseInfoDiv = document.GetElementbyId("baseinfo");
            if (baseInfoDiv == null)
            {
                Console.WriteLine("获取不到基本信息");
                return;
            }
            var tableNode= baseInfoDiv.SelectSingleNode("./table[@class='table-noline']");
            if (tableNode == null)
            {
                Console.WriteLine("获取不到基本信息Table");
                return;
            }
           
            //初始化基本信息
            doc = InitialBuildingDoc(tableNode, doc);
            #endregion
            //#region 提取经纬度
            //HtmlNode node = rootNode.SelectSingleNode("//a[@id='mapSideImg']");

            //if (node != null)
            //{
            //    if (node.Attributes.Contains("data-location") == true)
            //    {
            //        string dataLocation = node.Attributes["data-location"].Value.Trim();
            //        string[] arrP = dataLocation.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
            //        if (arrP.Length == 2)
            //        {
            //            detailDoc.Add("x", arrP[0]);
            //            detailDoc.Add("y", arrP[1]);
            //        }
            //    }
            //}
            //#endregion

            #region 提取预售许可证
            var saleinfoDiv = document.GetElementbyId("saleinfo");
            if (saleinfoDiv == null)
            {
                Console.WriteLine("获取不到销售信息");
                return;
            }
            var saleinfoTableNode = saleinfoDiv.SelectSingleNode("./table[@class='table-noline']");
            if (saleinfoTableNode == null)
            {
                Console.WriteLine("获取不到基本信息SaleInfoTable");
                return;
            }
            ///获取销售基本信息
            doc = InitialBuildingDoc(saleinfoTableNode, doc);

            HtmlNode preSaleTableNode = rootNode.SelectSingleNode("//div[@class='table-wrapper saleLicense']/div[@class='table-body']/div/table");
            if (preSaleTableNode != null)
            {
                List<HtmlNode> trPreSales = preSaleTableNode.SelectNodes("tbody/tr").ToList();
                if (trPreSales != null && trPreSales.Count() > 0)
                {
                    foreach (var tr in trPreSales)
                    {
                        BsonDocument preSaleDoc = new BsonDocument();
                        List<HtmlNode> tdNodes = tr.SelectNodes("td").ToList();
                        if (tdNodes != null && tdNodes.Count() > 0)
                        {
                            preSaleDoc.Add("preSaleNo", tdNodes[0].InnerText.Trim());
                            preSaleDoc.Add("pushDate", tdNodes[1].InnerText.Trim());
                            preSaleDoc.Add("buildings", tdNodes[2].InnerText.Trim());
                        }
                        if (preSaleDoc.Elements.Count() > 0)
                        {
                            preSaleDoc.Add("projId", doc["projId"].AsString);
                            if (doc.Contains("projectGuid") == true)
                            {
                                preSaleDoc.Add("projectGuid", doc["projectGuid"].AsString);
                            }
                            preSaleDocs.Add(preSaleDoc);
                        }
                    }
                }

            }
            #endregion

            #region 提取价格
            HtmlNode priceTable = rootNode.SelectSingleNode("//div[@class='table-wrapper charge-info']/div[@class='table-body']/div/table");
            if (priceTable != null)
            {
                List<HtmlNode> trPrices = priceTable.SelectNodes("tbody/tr").ToList();
                if (trPrices != null && trPrices.Count() > 0)
                {
                    #region 价格详情
                    foreach (var tr in trPrices)
                    {
                        BsonDocument priceDoc = new BsonDocument();
                        List<HtmlNode> tdNodes = tr.SelectNodes("td").ToList();
                        if (tdNodes != null && tdNodes.Count() > 0)
                        {
                            priceDoc.Add("priceDate", tdNodes[0].InnerText.Trim());
                            priceDoc.Add("maxPrice", tdNodes[1].InnerText.Trim());
                            priceDoc.Add("avgPrice", tdNodes[2].InnerText.Trim());
                            priceDoc.Add("minPrice", tdNodes[3].InnerText.Trim());
                            //priceDoc.Add("minTotalPrice", tdNodes[4].InnerText.Trim());
                            var maxPrice = 0;
                            var avgPrice = 0;
                            var minPrice = 0;
                            if (priceDoc.Text("maxPrice").Contains("元/㎡"))
                            {

                                maxPrice = PriceParseToInt(priceDoc.Text("maxPrice"));
                            }
                            if (priceDoc.Text("avgPrice").Contains("元/㎡"))
                            {
                                avgPrice = PriceParseToInt(priceDoc.Text("avgPrice"));
                            }
                            if (priceDoc.Text("minPrice").Contains("元/㎡"))
                            {
                                minPrice = PriceParseToInt(priceDoc.Text("minPrice"));
                            }
                            if (avgPrice == 0)
                            {
                                if (maxPrice != 0 && minPrice != 0)
                                {
                                    avgPrice = ((maxPrice + minPrice) / 2) / 100 * 100;//百位清零
                                }
                                if (maxPrice != 0 && minPrice == 0)
                                {
                                    avgPrice = maxPrice;
                                }
                                if (maxPrice == 0 && minPrice != 0)
                                {
                                    avgPrice = minPrice;
                                }
                            }
                            if (avgPrice != 0)
                            {
                                priceDoc.Add("price", avgPrice.ToString());
                            }
                            priceDoc.Add("remark", tdNodes[4].InnerText.Trim());
                        }
                        if (priceDoc.Elements.Count() > 0)
                        {
                            priceDoc.Add("projId", doc["projId"].AsString);
                            if (doc.Contains("projectGuid") == true)
                            {
                                priceDoc.Add("projectGuid", doc["projectGuid"].AsString);
                            }
                            detailPriceDocs.Add(priceDoc);
                        }
                    }
                    #endregion
                }
            }
            #endregion

          
            #region 提取交通信息
            var trafficinfoDiv = document.GetElementbyId("trafficinfo");
            if (trafficinfoDiv == null)
            {
                Console.WriteLine("获取不到交通信息信息");
                return;
            }
            var trafficinfoTableNode = trafficinfoDiv.SelectSingleNode("./table[@class='table-noline']");
            if (trafficinfoTableNode == null)
            {
                Console.WriteLine("获取不到基本信息SaleInfoTable");
                return;
            }
            doc = InitialBuildingDoc(trafficinfoTableNode, doc);
            #endregion

            #region 规划信息
            var supportinfoDiv = document.GetElementbyId("supportinfo");
            if (supportinfoDiv == null)
            {
                Console.WriteLine("获取不到规划信息信息");
                return;
            }
            var supportinfoTableNode = supportinfoDiv.SelectSingleNode("./table[@class='table-noline']");
            if (supportinfoTableNode == null)
            {
                Console.WriteLine("获取不到基本信息supportinfo");
                return;
            }
            doc = InitialBuildingDoc(supportinfoTableNode, doc);
            #endregion

            #region 规划信息
            var descDiv = document.GetElementbyId("lpdes");
            if (descDiv == null)
            {
                Console.WriteLine("获取不到规划信息信息");
                return;
            }
            var descP = supportinfoDiv.SelectSingleNode("//p[@class='panel-para']");
            if (descP == null)
            {
                Console.WriteLine("获取不到基本信息supportinfo");
                // return;
            }
            else
            {
                doc.Set("remark", descP.InnerText.Trim());
            }
            #endregion

            #region 更新到数据库
            doc.Add("isNeedUpdate", "0");
            var projQuery = Query.EQ("projId", doc["projId"].AsString);
            if (doc.Elements.Count() > 0)
            {

                if (!hasExistObj(doc["projId"].AsString))
                {
                    #region 新增
                   /// dataop.Insert(ProjectDetail, detailDoc);
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectDetail, Document = doc, Type = StorageType.Insert });
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = new BsonDocument().Add("isUpdate","2"),Query= projQuery, Type = StorageType.Update });
                    Console.WriteLine("新增detail");
                    #endregion
                }
                else
                {
                   DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectDetail, Document = doc, Query=Query.EQ("projId", doc["projId"].AsString), Type = StorageType.Update });
                    //更新 未处理
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Name = DataTableName, Document = new BsonDocument().Add("isUpdate", "2"), Query = projQuery, Type = StorageType.Update });
                }
                 
                
                if (detailPriceDocs.Count() > 0)
                {
                    //删除价格信息
                   
                    dataop.Delete(ProjectDetailPrice, projQuery);
                    foreach (var detailPriceDoc in detailPriceDocs)
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectDetailPrice, Document = detailPriceDoc, Type = StorageType.Insert });
                    }
                }
                if (openDocs.Count() > 0)
                {
                    dataop.Delete(ProjectOpen, projQuery);
                    foreach (var openDoc in openDocs)
                    {
                        DBChangeQueue.Instance.EnQueue(new StorageData() { Name = ProjectOpen, Document = openDoc, Type = StorageType.Insert });
                    }

                }

                if (preSaleDocs.Count() > 0)
                {
                    dataop.Delete(PreSaleTable, projQuery);
                    foreach (var preSaleDoc in preSaleDocs)
                    {
                       DBChangeQueue.Instance.EnQueue(new StorageData() { Name = PreSaleTable, Document = preSaleDoc, Type = StorageType.Insert });
                    }

                }

            }
            #endregion
       }
        #endregion


        #region method

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
        #endregion
    }

}
