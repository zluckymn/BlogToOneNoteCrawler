namespace SimpleCrawler.Demo
{
    using HtmlAgilityPack;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver.Builders;
    using Newtonsoft.Json.Linq;
    using SimpleCrawler;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Web;
    using Yinhe.ProcessingCenter;
    using Yinhe.ProcessingCenter.DataRule;

    public class SiMuListCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private BloomFilter<string> filter;
        private BloomFilter<string> urlFilter = new BloomFilter<string>(0x7a1200);
        private const string _DataTableName = "SiMu_Project";
#pragma warning disable CS0414 // 字段“SiMuListCrawler.huxiu_hash_code”已被赋值，但从未使用过它的值
        private string huxiu_hash_code = "6090caa5f7ef6fe849d98aa30b9b8a22";
#pragma warning restore CS0414 // 字段“SiMuListCrawler.huxiu_hash_code”已被赋值，但从未使用过它的值
        private string cookie = "JSESSIONID=3D9611D26264FDB33C8A2AE4C392F97D;quickLogonKey=781f8bcecdf14284a069aabaa67e7101$40FED11B93B7E0691A2BB09B3E6BB2F6";
        private int pageSize = 200;
        private Dictionary<string, string> urlDic = new Dictionary<string, string>();
        private List<BsonDocument> allCityList = new List<BsonDocument>();
        private List<BsonDocument> allNeedRound = new List<BsonDocument>();
        private List<BsonDocument> allIndustry = new List<BsonDocument>();

        public SiMuListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            Newtonsoft.Json.Linq.JObject obj2 = Newtonsoft.Json.Linq.JObject.Parse(args.Html);
            Newtonsoft.Json.Linq.JToken source = obj2["result"];
            int num = int.Parse(obj2["total"].ToString());
            if (source.Count<Newtonsoft.Json.Linq.JToken>() <= 0)
            {
                Console.WriteLine("无获取数据");
            }
            else
            {
                foreach (Newtonsoft.Json.Linq.JToken token2 in (IEnumerable<Newtonsoft.Json.Linq.JToken>) source)
                {
                    try
                    {
                        BsonDocument bsonDoc = BsonSerializer.Deserialize<BsonDocument>(token2.ToString());
                        string guid = bsonDoc.Text("encodeEpNeedId");
                        if (!this.hasExistObj(guid))
                        {
                            bsonDoc.Set("guid", bsonDoc.Text("encodeEpNeedId"));
                            StorageData target = new StorageData {
                                Name = this.DataTableName,
                                Document = bsonDoc,
                                Type = StorageType.Insert
                            };
                            DBChangeQueue.Instance.EnQueue(target);
                            Console.WriteLine(bsonDoc.Text("epNeedCompanyName") + UrlQueue.Instance.Count.ToString());
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
                if (num >= 300)
                {
                    Console.WriteLine("开始翻页");
                    string urlParam = GetUrlParam(args.urlInfo.PostData, "limit");
                    string distinct = GetUrlParam(args.urlInfo.PostData, "epNeedCompanyDistrict");
                    string s = GetUrlParam(args.urlInfo.PostData, "page");
                    string str5 = GetUrlParam(args.urlInfo.PostData, "start");
                    string str6 = GetUrlParam(args.urlInfo.PostData, "epNeedRound");
                    string industry = GetUrlParam(args.urlInfo.PostData, "epNeedIndustry");
                    if (string.IsNullOrEmpty(str6))
                    {
                        foreach (BsonDocument document2 in this.allNeedRound)
                        {
                            string epNeedRound = document2.Text("dicId");
                            this.InitialUrl(urlParam, distinct, "0", "0", epNeedRound, industry);
                        }
                    }
                    else
                    {
                        int num3 = int.Parse(s);
                        int num4 = int.Parse(str5);
                        int num2 = num3;
                        num3 = num2 + 1;
                        this.InitialUrl(urlParam, distinct, num3.ToString(), (num3 * this.pageSize).ToString(), str6, industry);
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

        private static string GetQueryString(string url)
        {
            try
            {
                int index = url.IndexOf("?");
                if (index != -1)
                {
                    return url.Substring(index + 1, (url.Length - index) - 1);
                }
                return url;
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string GetUrlParam(string url, string name)
        {
            try
            {
                NameValueCollection values = HttpUtility.ParseQueryString(GetQueryString(url));
                return ((values[name] != null) ? values[name].ToString() : string.Empty);
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }

        private bool hasExistObj(string guid)
        {
            return (this.dataop.FindCount(this.DataTableName, Query.EQ("guid", guid)) > 0);
        }

        private void InitialUrl(string limit, string distinct, string page, string start, string epNeedRound, string industry)
        {
            string item = string.Format("https://app.pedata.cn/PEDATA_APP_BACK/epneed/list", new object[0]);
            if (!this.filter.Contains(item))
            {
                string str2 = string.Format("limit={0}&epNeedCompanyDistrict={1}&platform=android&umeng_channel=android.myapp.com&page={2}&start={3}&myCollects=&epNeedRound={4}&_listQueryStr=&device_info=MLA-AL10   R8207867910010140897   &platversion=309050&app_name=smt_app&epNeedIndustry={5}", new object[] { limit, distinct, page, start, epNeedRound, industry });
                UrlInfo target = new UrlInfo(item) {
                    Depth = 1,
                    PostData = str2
                };
                UrlQueue.Instance.EnQueue(target);
            }
        }

        private void IPInvalidProcess(IPProxy ipproxy)
        {
            this.Settings.SetUnviableIP(ipproxy);
            if (ipproxy != null)
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
            Newtonsoft.Json.Linq.JToken token = Newtonsoft.Json.Linq.JObject.Parse(args.Html)["success"];
            return ((token == null) || (token.ToString() != "True"));
        }

        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.MaxReTryTimes = 20;
            this.Settings.ThreadCount = 1;
            this.Settings.Accept = "application/json, text/javascript, */*; q=0.01";
            this.Settings.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
            this.Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1068.1 Safari/536.3";
            this.Settings.KeepCookie = true;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
           // this.Settings.SimulateCookies = this.cookie;
           // dictionary.Add("Cookie", this.Settings.SimulateCookies);
            dictionary.Add("Accept-Encoding", "gzip");
            dictionary.Add("Charsert", "UTF-8");
            this.Settings.HeadSetDic = dictionary;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在初始化选择url队列");
            this.allCityList = this.dataop.FindAll(this.DataTableNameCity).ToList<BsonDocument>();
            this.allNeedRound = this.dataop.FindAll(this.DataTableNameNeedRound).ToList<BsonDocument>();
            this.allIndustry = this.dataop.FindAll(this.DataTableNameIndustry).ToList<BsonDocument>();
            foreach (BsonDocument document in this.allCityList)
            {
                string distinct = document.Text("dicId");
                foreach (BsonDocument document2 in this.allIndustry)
                {
                    string industry = document2.Text("dicId");
                    string epNeedRound = "";
                    this.InitialUrl(this.pageSize.ToString(), distinct, "0", "0", epNeedRound, industry);
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
                if (item != null)
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
                return "SiMu_Project";
            }
        }

        public string DataTableNameURL
        {
            get
            {
                return "SiMu_ProjectURL";
            }
        }

        public string DataTableNameCity
        {
            get
            {
                return "SiMu_City";
            }
        }

        public string DataTableNameIndustry
        {
            get
            {
                return "SiMu_Industry";
            }
        }

        public string DataTableNameNeedRound
        {
            get
            {
                return "SiMu_NeedRound";
            }
        }
    }
}

