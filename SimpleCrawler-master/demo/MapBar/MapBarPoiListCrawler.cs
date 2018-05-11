namespace SimpleCrawler.Demo
{
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

    public class MapBarPoiListCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private BloomFilter<string> filter;
        private BloomFilter<string> idFilter = new BloomFilter<string>(0x7a1200);
        private const string _DataTableName = "MapBar_";
        private List<BsonDocument> allCityList = new List<BsonDocument>();
        private List<BsonDocument> allCategoryList = new List<BsonDocument>();

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

        public void DataReceive(DataReceivedEventArgs args)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(args.Html);
            HtmlNode documentNode = document.DocumentNode;
            string cityCode = Str.Sub(args.Url, "http://poi.mapbar.com/", "/");
            string catCode = Str.Sub(args.Url, string.Format("http://poi.mapbar.com/{0}/", cityCode), "/");
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
                    string str = bsonDoc.Text("catName");
                    int num = 0;
                    int num2 = 0;
                    if (args.Url.Contains("poi.mapbar.com"))
                    {
                        HtmlNodeCollection nodes = documentNode.SelectNodes("//div[@class='sortC']/dl/dd/a");
                        if (nodes !=null)
                        {
                            foreach (HtmlNode node2 in (IEnumerable<HtmlNode>) nodes)
                            {
                                string str2 = node2.Attributes["href"].Value;
                                if (!str2.Contains("http://poi.mapbar.com"))
                                {
                                    Console.WriteLine("错误连接");
                                }
                                else
                                {
                                    string innerText = node2.InnerText;
                                    BsonDocument document4 = new BsonDocument().Add("name", innerText.Trim());
                                    document4.Add("url", str2);
                                    string str4 = str2.GetHashCode().ToString();
                                    document4.Add("guid", str4);
                                    document4.Add("cityCode", cityCode);
                                    document4.Add("catCode", catCode);
                                    Console.WriteLine(innerText);
                                    int num3 = num;
                                    num = num3 + 1;
                                    this.idFilter.Add(str4);
                                    StorageData target = new StorageData {
                                        Document = document4,
                                        Name = this.DataTableName,
                                        Type = StorageType.Insert
                                    };
                                    DBChangeQueue.Instance.EnQueue(target);
                                }
                            }
                        }
                    }
                    Console.WriteLine("{0}添加{1}更新", num, num2);
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

        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.MaxReTryTimes = 10;
            this.Settings.ThreadCount = 1;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在初始化选择url队列");
            this.allCityList = this.dataop.FindAllByQuery(this.DataTableNameCity, Query.NE("isUpdate", 1)).ToList<BsonDocument>();
            this.allCategoryList = this.dataop.FindAll(this.DataTableNameCategory).ToList<BsonDocument>();
            foreach (BsonDocument document in this.allCityList)
            {
                foreach (BsonDocument document2 in this.allCategoryList)
                {
                    string item = string.Format("http://poi.mapbar.com/{0}/{1}/", document.Text("cityCode"), document2.Text("code"));
                    if (!this.filter.Contains(item))
                    {
                        UrlInfo target = new UrlInfo(item) {
                            Depth = 1
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

        public string DataTableName
        {
            get
            {
                return "MapBar_Poi";
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

