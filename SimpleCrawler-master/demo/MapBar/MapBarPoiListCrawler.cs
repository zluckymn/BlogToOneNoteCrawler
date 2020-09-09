namespace SimpleCrawler.Demo
{
    using Helper;
    using HtmlAgilityPack;
    using MongoDB.Bson;
    using MongoDB.Driver.Builders;
    using SimpleCrawler;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Toolslib;
    using Yinhe.ProcessingCenter;
    using Yinhe.ProcessingCenter.DataRule;
    using Yinhe.SearchEngine.BusinessLogic;

    public class MapBarPoiListCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private BloomFilter<string> filter;
        private BloomFilter<string> idFilter = new BloomFilter<string>(0x7a1200);
        private const string _DataTableName = "MapBar_";
        private List<BsonDocument> allCityList = new List<BsonDocument>();
        private List<BsonDocument> allCategoryList = new List<BsonDocument>();
        private bool isUpdatePageItem = true;//第一页是否爬取
        public MapBarPoiListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            this.Settings = _Settings;
            this.filter = _filter;
            this.dataop = _dataop;
        }

        public bool CanAddUrl(AddUrlEventArgs args)
        {
            return true;
        }

        public string DataTableName
        {
            get
            {
                return "MapBar_Poi";
            }
        }

        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.MaxReTryTimes = 20;
            this.Settings.ThreadCount = 2;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在初始化选择url队列");
            this.allCityList = this.dataop.FindAll(this.DataTableNameCity).ToList<BsonDocument>();
            this.allCategoryList = this.dataop.FindAll(this.DataTableNameCategory).ToList<BsonDocument>();
            var canContinue = false;
            foreach (BsonDocument document in this.allCityList)
            {
                //if (document.Text("cityCode") == "chongqing")
                //{
                //    canContinue = true;

                //}
                //if (!canContinue) continue;
                foreach (BsonDocument document2 in this.allCategoryList)//农贸数据.Where(c=>c.Text("code")=="548")
                {
                    string item = string.Format("http://poi.mapbar.com/{0}/{1}/", document.Text("cityCode"), document2.Text("code"));
                    if (!this.filter.Contains(item))
                    {
                        UrlInfo target = new UrlInfo(item)
                        {
                            Depth = 1, UniqueKey= "1", extraData=document2.Text("code"), 
                        };
                        UrlQueue.Instance.EnQueue(target);
                    }
                }
            }
            this.Settings.RegularFilterExpressions.Add("XXX");
            if (!this.SimulateLogin())
            {
                Console.WriteLine("初始化失败");
            }
        }

        public void DataReceive(DataReceivedEventArgs args)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(args.Html);
            HtmlNode documentNode = document.DocumentNode;
            string cityCode = Str.Sub(args.Url, "http://poi.mapbar.com/", "/");
            // string cityCode = args.urlInfo.UniqueKey;
            var totalPageCountStr = "1";
            if (args.Html.Contains("var pageNum"))
            {
                 totalPageCountStr = Toolslib.Str.Sub(args.Html, "var pageNum = '", "';");
            }
            string catCode = args.urlInfo.extraData;
            int.TryParse(totalPageCountStr, out int totalPageCount);
            var pageNo = args.urlInfo.UniqueKey;


            if ((from c in this.allCityList
                where c.Text("cityCode") == cityCode.Trim()
                select c).FirstOrDefault<BsonDocument>() == null)
            {
                Console.WriteLine("无对应城市");
            }
            else
            {
                BsonDocument bsonDoc = (from c in this.allCategoryList
                    where c.Text("code") == catCode.Trim()
                    select c).FirstOrDefault<BsonDocument>();
                if (bsonDoc == null)
                {
                    Console.WriteLine("无对应目录");
                }
                else
                {

                    if (isUpdatePageItem==false|| (isUpdatePageItem && pageNo !="1")) {

                        string str = bsonDoc.Text("catName");
                        int addCount = 0;
                        int updateCount = 0;
                        if (args.Url.Contains("poi.mapbar.com"))
                        {
                            HtmlNodeCollection nodes = documentNode.SelectNodes("//div[@class='sortC']/dl/dd/a");
                            if (nodes != null)
                            {
                                foreach (HtmlNode node2 in (IEnumerable<HtmlNode>)nodes)
                                {
                                    string str2 = node2.Attributes["href"].Value;
                                    if (!str2.Contains("http://poi.mapbar.com"))
                                    {
                                        Console.WriteLine("错误连接");
                                    }
                                    else
                                    {

                                        string innerText = node2.InnerText;
                                        BsonDocument addDoc = new BsonDocument().Add("name", innerText.Trim());
                                        addDoc.Add("url", str2);
                                        string guid = str2.GetHashCode().ToString();

                                        addDoc.Add("guid", guid);
                                        addDoc.Add("cityCode", cityCode);
                                        addDoc.Add("catCode", catCode);

                                        addCount++;
                                        if (!idFilter.Contains(guid) && !hasExistObj(guid, "guid"))
                                        {
                                            this.idFilter.Add(guid);
                                            StorageData target = new StorageData
                                            {
                                                Document = addDoc,
                                                Name = this.DataTableName,
                                                Type = StorageType.Insert
                                            };
                                            DBChangeQueue.Instance.EnQueue(target);
                                        }
                                        else
                                        {
                                            updateCount++;
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine("{0}添加{1}更新", addCount, updateCount);
                    }
                   
                    //获取分页
                
                    //var pageNum = '3';
              
                    if (pageNo == "1"&& totalPageCount>1) {
                        for (var pageIndex = 2; pageIndex <= totalPageCount; pageIndex++) {
                         
                            if (pageIndex != 1)
                            {
                                var cityCodeExtra = $"{catCode}_{pageIndex}";
                                var url = args.Url.Replace(catCode, cityCodeExtra);
                                if (!this.filter.Contains(url))
                                {
                                    UrlInfo target = new UrlInfo(url)
                                    {
                                        Depth = 1,
                                        UniqueKey = pageIndex.ToString(),
                                        extraData = catCode,
                                    };
                                    UrlQueue.Instance.EnQueue(target);
                                }
                            }
                        }
                       
                    }
                   

                }
            }
        }

        public void ErrorReceive(CrawlErrorEventArgs args)
        {
        }

        public string GetInnerText(HtmlNode node)
        {
            if ((node == null) || string.IsNullOrEmpty(node.InnerText))
            {
                throw new NullReferenceException();
            }
            return node.InnerText;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool hasExistObj(string guid, string fieldName)
        {
            return (this.dataop.FindCount(this.DataTableName, Query.EQ(fieldName, guid)) > 0);
        }

        private void IPInvalidProcess(IPProxy ipproxy)
        {
            this.Settings.SetUnviableIP(ipproxy);
            if (ipproxy !=null)
            {
                StorageData target = new StorageData {
                    Name = "IPProxy",
                    Document = new BsonDocument().Add("status", "1"),
                    Query = Query.EQ("ip", ipproxy.IP)
                };
                DBChangeQueue.Instance.EnQueue(target);
                this.StartDBChangeProcess();
            }
        }

        public bool IPLimitProcess(DataReceivedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))
            {
                return true;
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(args.Html);
            if (document.GetElementbyId("sortSide") == null)
            {
                Console.WriteLine("url不存在");
                return true;
            }
            return false;
        }

      

        public bool SimulateLogin()
        {
            return true;
        }

        private void StartDBChangeProcess()
        {
            List<StorageData> source = new List<StorageData>();
            while (DBChangeQueue.Instance.Count > 0)
            {
                StorageData item = DBChangeQueue.Instance.DeQueue();
                if (item !=null)
                {
                    source.Add(item);
                }
            }
            if ((source.Count<StorageData>() > 0) && (this.dataop.BatchSaveStorageData(source).Status > Status.Successful))
            {
                foreach (StorageData data2 in source)
                {
                    DBChangeQueue.Instance.EnQueue(data2);
                }
            }
        }


        public string DataTableNameURL
        {
            get
            {
                return "MapBar_URL";
            }
        }

        public string DataTableNameCity
        {
            get
            {
                return "MapBar_City";
            }
        }

        public string DataTableNameCategory
        {
            get
            {
                return "MapBar_Category";
            }
        }
    }
}

