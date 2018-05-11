namespace SimpleCrawler.Demo
{
    using HtmlAgilityPack;
    using MongoDB.Bson;
    using MongoDB.Driver.Builders;
    using Newtonsoft.Json.Linq;
    using SimpleCrawler;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Yinhe.ProcessingCenter;
    using Yinhe.ProcessingCenter.DataRule;

    public class HuXiuListCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private BloomFilter<string> filter;
        private BloomFilter<string> urlFilter = new BloomFilter<string>(0x7a1200);
        private const string _DataTableName = "HuXiuProject";
        private string huxiu_hash_code = "6090caa5f7ef6fe849d98aa30b9b8a22";
        private string cookie = "huxiu_analyzer_wcy_id=8ehxzchebcamo4hrtht; gr_user_id=195bbdae-dae2-4c5e-aa58-8cfb1fb2f983; b6a739d69e7ea5b6_gr_last_sent_cs1=0; _ga=GA1.2.31956796.1521192557; screen=%7B%22w%22%3A1440%2C%22h%22%3A900%2C%22d%22%3A1%7D; aliyungf_tc=AQAAAEBxQxdFggcAIkg9OyZRDExnOTml; _gid=GA1.2.973452414.1521708084; Hm_lvt_324368ef52596457d064ca5db8c6618e=1521192557,1521195814,1521708085; b6a739d69e7ea5b6_gr_session_id=a92c45d9-1bf3-47df-b216-e40667b82c3f; b6a739d69e7ea5b6_gr_last_sent_sid_with_cs1=a92c45d9-1bf3-47df-b216-e40667b82c3f; Hm_lpvt_324368ef52596457d064ca5db8c6618e=1521771401; b6a739d69e7ea5b6_gr_cs1=0; SERVERID=6d35b07e250aadfe8b4fbf72aeadecb9|1521772014|1521771265";
        private Dictionary<string, string> urlDic = new Dictionary<string, string>();
        public static object lockThis = new object();

        public HuXiuListCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            int pageCount = int.Parse(obj2["total_page"].ToString());
            Newtonsoft.Json.Linq.JToken token = obj2["data"];
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(token.ToString());
            HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//div[@class='cy-cp-box transition']");
            foreach (HtmlNode node in (IEnumerable<HtmlNode>) nodes)
            {
                HtmlNode node2 = (from c in node.SelectNodes("./a")
                    where c.InnerText.Trim() != ""
                    select c).FirstOrDefault<HtmlNode>();
                if ((node2 != null) && (node2.Attributes["href"] != null))
                {
                    string str = node2.InnerText.Trim();
                    BsonDocument bsonDoc = new BsonDocument {
                        { 
                            "name",
                            str.Trim()
                        },
                        { 
                            "url",
                            string.Format("{0}", node2.Attributes["href"].Value.ToString())
                        }
                    };
                    HtmlNode node3 = node.SelectSingleNode("./div[@class='cy-cp-info']");
                    if (node3 != null)
                    {
                        bsonDoc.Add("info", node3.InnerText.Trim());
                    }
                    HtmlNodeCollection nodes3 = node.SelectNodes("./div[@class='cy-cp-tag']/ul/li");
                    foreach (HtmlNode node4 in (IEnumerable<HtmlNode>) nodes3)
                    {
                        string[] separator = new string[] { "：", ":" };
                        string[] strArray = node4.InnerText.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        if (strArray.Length >= 2)
                        {
                            bsonDoc.Set(strArray[0].Trim(), strArray[1].Trim());
                        }
                    }
                    if (!this.urlFilter.Contains(bsonDoc.Text("url")) && !this.hasExistObj(bsonDoc.Text("url")))
                    {
                        Console.WriteLine(bsonDoc.Text("name"));
                        StorageData target = new StorageData {
                            Document = bsonDoc,
                            Name = this.DataTableName,
                            Type = StorageType.Insert
                        };
                        DBChangeQueue.Instance.EnQueue(target);
                        this.urlFilter.Add(bsonDoc.Text("url"));
                    }
                }
            }
            if ((pageCount >= 2) && args.urlInfo.PostData.EndsWith("&page=1"))
            {
                this.InitialUrl(2, pageCount);
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

        private bool hasExistObj(string guid)
        {
            return (this.dataop.FindCount(this.DataTableName, Query.EQ("url", guid)) > 0);
        }

        private void InitialUrl(int startIndex, int pageCount)
        {
            int num2;
            for (int i = startIndex; i <= pageCount; i = num2 + 1)
            {
                string item = string.Format("https://www.huxiu.com/chuangye/ajax_home", new object[0]);
                if (!this.filter.Contains(item))
                {
                    string str2 = string.Format("is_ajax=1&huxiu_hash_code={1}&order=&page={0}", i, this.huxiu_hash_code);
                    UrlInfo target = new UrlInfo(item) {
                        Depth = 1,
                        PostData = str2
                    };
                    UrlQueue.Instance.EnQueue(target);
                }
                num2 = i;
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
            Newtonsoft.Json.Linq.JObject obj2 = Newtonsoft.Json.Linq.JObject.Parse(args.Html);
            Newtonsoft.Json.Linq.JToken token = obj2["jsonObj"];
            Newtonsoft.Json.Linq.JToken token2 = obj2["result"];
            return ((token2 == null) || (token2.ToString() != "1"));
        }

        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.MaxReTryTimes = 20;
            this.Settings.ThreadCount = 1;
            this.Settings.Accept = "application/json, text/javascript, */*; q=0.01";
            this.Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.146 Safari/537.36";
            this.Settings.KeepCookie = true;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            this.Settings.SimulateCookies = this.cookie;
            dictionary.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            dictionary.Add("Cookie", this.Settings.SimulateCookies);
            dictionary.Add("Accept-Encoding", "gzip, deflate, br");
            this.Settings.HeadSetDic = dictionary;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在初始化选择url队列");
            this.InitialUrl(1, 1);
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
                if (item!= null)
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

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功");
        }

        public string DataTableName
        {
            get
            {
                return "HuXiuProject";
            }
        }

        public string DataTableNameURL
        {
            get
            {
                return "HuXiuProjectURL";
            }
        }

        public string DataTableNameCity
        {
            get
            {
                return "MH_CartoonCity";
            }
        }

        
    }
}

