namespace SimpleCrawler.Demo
{
    using HtmlAgilityPack;
    using MongoDB.Bson;
    using MongoDB.Driver.Builders;
    using SimpleCrawler;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Web;
    using Toolslib;
    using Yinhe.ProcessingCenter;
    using Yinhe.ProcessingCenter.DataRule;

    public class HuXiuDetailCrawler : ISimpleCrawler
    {
        private DataOperation dataop = null;
        private CrawlSettings Settings = null;
        private BloomFilter<string> filter;
        private BloomFilter<string> schoolIdFilter = new BloomFilter<string>(0x7a1200);
        private const string _DataTableName = "HuXiuProject";
#pragma warning disable CS0414 // 字段“HuXiuDetailCrawler.huxiu_hash_code”已被赋值，但从未使用过它的值
        private string huxiu_hash_code = "6090caa5f7ef6fe849d98aa30b9b8a22";
#pragma warning restore CS0414 // 字段“HuXiuDetailCrawler.huxiu_hash_code”已被赋值，但从未使用过它的值
        private string cookie = "screen=%7B%22w%22%3A1440%2C%22h%22%3A900%2C%22d%22%3A1%7D; huxiu_analyzer_wcy_id=8ehxzchebcamo4hrtht; gr_user_id=195bbdae-dae2-4c5e-aa58-8cfb1fb2f983; b6a739d69e7ea5b6_gr_last_sent_cs1=0; _ga=GA1.2.31956796.1521192557; screen=%7B%22w%22%3A1440%2C%22h%22%3A900%2C%22d%22%3A1%7D; aliyungf_tc=AQAAAEBxQxdFggcAIkg9OyZRDExnOTml; _gid=GA1.2.973452414.1521708084; Hm_lvt_324368ef52596457d064ca5db8c6618e=1521192557,1521195814,1521708085; b6a739d69e7ea5b6_gr_session_id=a92c45d9-1bf3-47df-b216-e40667b82c3f; b6a739d69e7ea5b6_gr_last_sent_sid_with_cs1=a92c45d9-1bf3-47df-b216-e40667b82c3f; show_view_com_id=60984; b6a739d69e7ea5b6_gr_cs1=0; Hm_lpvt_324368ef52596457d064ca5db8c6618e=1521774318; SERVERID=6d35b07e250aadfe8b4fbf72aeadecb9|1521774418|1521771265";
        private List<BsonDocument> records = new List<BsonDocument>();
        public static object lockThis = new object();
        public static List<Task> allTask = new List<Task>();

        public HuXiuDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            this.Settings = _Settings;
            this.filter = _filter;
            this.dataop = _dataop;
        }

        public bool CanAddUrl(AddUrlEventArgs args)
        {
            return true;
        }

        public void DataReceive(SimpleCrawler.DataReceivedEventArgs args)
        {
            try
            {
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(args.Html);
                string authorization = args.urlInfo.Authorization;
                HtmlNode elementbyId = document.GetElementbyId("cy_center");
                if (elementbyId == null)
                {
                    Console.WriteLine("查找不到ID对象");
                }
                else
                {
                    BsonDocument bsonDoc = new BsonDocument();
                    HtmlNode img = elementbyId.SelectSingleNode("./div/div[@class='cy-icon-box']/img");
                    if ((img != null) && (img.Attributes["src"] !=null))
                    {
                        BsonDocument document3 = SaveProductImg(img, "projThumb");
                        bsonDoc.Set("projLocalThumb", document3.Text("localSrc"));
                        bsonDoc.Set("projThumb", document3.Text("src"));
                    }
                    HtmlNode node3 = elementbyId.SelectSingleNode("//h1[@class='cy-cp-name']");
                    if (node3 !=null)
                    {
                        bsonDoc.Add("name", node3.InnerText.Trim());
                    }
                    HtmlNode node4 = elementbyId.SelectSingleNode("//div[@class='cy-xq-time']");
                    if (node4 !=null)
                    {
                        bsonDoc.Add("pushDate", node4.InnerText.Replace("发布时间：", "").Trim());
                    }
                    HtmlNode node5 = elementbyId.SelectSingleNode("//div[@class='cy-tag-list']");
                    if (node5 !=null)
                    {
                        bsonDoc.Add("tag", node5.InnerText.Trim());
                    }
                    HtmlNode node6 = elementbyId.SelectSingleNode("//div[@class='cy-cp-intro']");
                    if (node6 !=null)
                    {
                        bsonDoc.Add("companyIntro", node6.InnerText.Trim());
                    }
                    HtmlNode node7 = elementbyId.SelectSingleNode("//div[@class='cy-cp-intro-info']");
                    if (node7 !=null)
                    {
                        bsonDoc.Add("productIntro", node7.InnerText.Trim());
                    }
                    HtmlNode node8 = document.GetElementbyId("business");
                    if (node8 !=null)
                    {
                        bsonDoc.Add("domain", node8.InnerText.Trim());
                    }
                    if (document.GetElementbyId("shareholders") !=null)
                    {
                        bsonDoc.Add("shareholders", node8.InnerText);
                    }
                    HtmlNodeCollection imgs = elementbyId.SelectNodes("//ul[@class='gallery-img-box']/li/img");
                    if (imgs !=null)
                    {
                        BsonDocument document4 = SaveProductImgs(imgs, bsonDoc.Text("pushDate"));
                        bsonDoc.Add("srcList", document4.Text("srcList"));
                        bsonDoc.Add("localSrcList", document4.Text("localSrcList"));
                    }
                    HtmlNode node10 = document.GetElementbyId("advantage");
                    if (node10 !=null)
                    {
                        bsonDoc.Add("advantage", node10.InnerText.Trim());
                    }
                    HtmlNode node11 = document.GetElementbyId("results");
                    if (node11 !=null)
                    {
                        bsonDoc.Add("achievements", node11.InnerText.Trim());
                    }
                    HtmlNodeCollection nodes2 = elementbyId.SelectNodes("//ul[@class='cy-cp-team']/li");
                    if (nodes2 !=null)
                    {
                        BsonArray array = new BsonArray();
                        foreach (HtmlNode node14 in (IEnumerable<HtmlNode>) nodes2)
                        {
                            HtmlNode node15 = node14.SelectSingleNode("./div[@class='team-personnel-name']");
                            HtmlNode node16 = node14.SelectSingleNode("./div[@class='team-personnel-position']");
                            HtmlNode node17 = node14.SelectSingleNode("./div[@class='team-personnel-intro']");
                            if (((node15 != null) && (node16 != null)) && (node17 !=null))
                            {
                                BsonDocument document5 = new BsonDocument {
                                    { 
                                        "name",
                                        node15.InnerText.Trim()
                                    },
                                    { 
                                        "position",
                                        node16.InnerText.Trim()
                                    },
                                    { 
                                        "intro",
                                        node17.InnerText.Trim()
                                    }
                                };
                                array.Add(document5);
                            }
                        }
                        bsonDoc.Add("teamInfo", array);
                    }
                    HtmlNode node12 = elementbyId.SelectSingleNode("//div[@data-type='agree_chuangye']");
                    if (node12 !=null)
                    {
                        bsonDoc.Add("agree", node12.InnerText.Trim());
                    }
                    HtmlNode node13 = elementbyId.SelectSingleNode("//div[@data-type='disagree_chuangye']");
                    if (node13 !=null)
                    {
                        bsonDoc.Add("disAgree", node13.InnerText.Trim());
                    }
                    HtmlNodeCollection nodes3 = elementbyId.SelectNodes("//div[@class='box-moder cy-box-moder company-info']/ul/li");
                    if (nodes3 !=null)
                    {
                        foreach (HtmlNode node18 in (IEnumerable<HtmlNode>) nodes3)
                        {
                            string[] separator = new string[] { "：", ":" };
                            string[] strArray = node18.InnerText.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            if (strArray.Length >= 2)
                            {
                                bsonDoc.Set(strArray[0].Trim(), strArray[1].Trim());
                            }
                        }
                    }
                    HtmlNodeCollection nodes4 = elementbyId.SelectNodes("//div[@class='box-moder cy-box-moder company-info get-company-box hide']/ul/li");
                    if (nodes4 !=null)
                    {
                        foreach (HtmlNode node19 in (IEnumerable<HtmlNode>) nodes4)
                        {
                            string[] separator = new string[] { "：", ":" };
                            string[] strArray2 = node19.InnerText.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            if (strArray2.Length >= 2)
                            {
                                bsonDoc.Set(strArray2[0].Trim(), strArray2[1].Trim());
                            }
                        }
                    }
                    HtmlNodeCollection nodes5 = elementbyId.SelectNodes("//div[@class='box-moder cy-box-moder company-info get-company-box']/ul/li");
                    if (nodes5 !=null)
                    {
                        foreach (HtmlNode node20 in (IEnumerable<HtmlNode>) nodes5)
                        {
                            string[] separator = new string[] { "：", ":" };
                            string[] strArray3 = node20.InnerText.Trim().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                            if (strArray3.Length >= 2)
                            {
                                if (strArray3[0].Trim() == "办公地点")
                                {
                                    bsonDoc.Set("地址", strArray3[1].Trim());
                                }
                                else
                                {
                                    bsonDoc.Set(strArray3[0].Trim(), strArray3[1].Trim());
                                }
                            }
                        }
                    }
                    if (bsonDoc.ElementCount > 0)
                    {
                        bsonDoc.Set("isUpdate", "2");
                        Console.WriteLine(bsonDoc.Text("name") + "更新成功");
                        StorageData target = new StorageData {
                            Document = bsonDoc,
                            Query = Query.EQ("_id", ObjectId.Parse(authorization)),
                            Name = this.DataTableName,
                            Type = StorageType.Update
                        };
                        DBChangeQueue.Instance.EnQueue(target);
                    }
                    if (UrlQueue.Instance.Count == 0)
                    {
                        Task.WaitAll(allTask.ToArray());
                        Task.WhenAll(allTask).GetAwaiter().OnCompleted(() => Console.WriteLine("所有下载完成"));
                        Console.ReadLine();
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
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

        /// <summary>
        /// http://www.fang99.com/
        /// </summary>
        /// <param name="imgPath"></param>
        private async static Task<string> ImgSave(string imgUrl, string imgPath)
        {
            WebClient webClient = new WebClient();
            await webClient.DownloadFileTaskAsync(new Uri(imgUrl), imgPath);
            return imgUrl;
            //await Task.Run(() =>
            //{
            //    WebClient webClient = new WebClient();
            //    webClient.DownloadFile(new Uri(imgUrl), imgPath);

            //});

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

        public bool IPLimitProcess(SimpleCrawler.DataReceivedEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))
                {
                    return true;
                }
                HtmlDocument document = new HtmlDocument();
                document.LoadHtml(args.Html);
                if (document.GetElementbyId("cy_center") == null)
                {
                    Console.WriteLine("查找不到ID对象");
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        private static BsonDocument SaveProductImg(HtmlNode img, string catName)
        {
            BsonDocument document = new BsonDocument();
            if ((img != null) && (img.Attributes["src"] !=null))
            {
                string subject = img.Attributes["src"].Value.ToString();
                if (subject.Contains("imageView"))
                {
                    subject = "https" + Str.Sub(subject, "https", "?");
                }
                int hashCode = subject.GetHashCode();
                string path = string.Format("/HuXiu/{0}", catName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string str3 = string.Format("{0}/{1}.jpg", path, hashCode);
                if (!System.IO.File.Exists(str3))
                {
                    object lockThis = HuXiuDetailCrawler.lockThis;
                    lock (lockThis)
                    {
                        Task<string> result = ImgSave(subject, str3);
                        allTask.Add(result);
                        result.GetAwaiter().OnCompleted(delegate {
                            allTask.Remove(result);
                            Console.WriteLine("完成下载{0}\n{1}", result.Result, allTask.Count<Task>());
                        });
                    }
                }
                document.Add("src", subject);
                document.Add("localSrc", str3);
            }
            return document;
        }

        private static BsonDocument SaveProductImgs(HtmlNodeCollection imgs, string catName)
        {
            BsonDocument document = new BsonDocument();
            BsonArray array = new BsonArray();
            BsonArray array2 = new BsonArray();
            foreach (HtmlNode node in (IEnumerable<HtmlNode>) imgs)
            {
                BsonDocument bsonDoc = SaveProductImg(node, catName);
                array.Add(bsonDoc.Text("src"));
                array2.Add(bsonDoc.Text("localSrc"));
            }
            document.Add("srcList", array);
            document.Add("localSrcList", array2);
            return document;
        }

        public void SettingInit()
        {
            this.Settings.IPProxyList = new List<IPProxy>();
            this.Settings.IgnoreSucceedUrlToDB = true;
            this.Settings.MaxReTryTimes = 20;
            this.Settings.ThreadCount = 5;
            this.Settings.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            this.Settings.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            this.Settings.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.146 Safari/537.36";
            this.Settings.KeepCookie = true;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            this.Settings.SimulateCookies = this.cookie;
            dictionary.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
            dictionary.Add("Cookie", this.Settings.SimulateCookies);
            dictionary.Add("Accept-Encoding", "gzip, deflate, br");
            dictionary.Add("Upgrade-Insecure-Requests", "1");
            this.Settings.HeadSetDic = dictionary;
            Console.WriteLine("正在获取已存在的url数据");
            Console.WriteLine("正在初始化选择url队列");
            string[] fields = new string[] { "url" };
            this.records = this.dataop.FindAllByQuery("HuXiuProject", Query.Exists("isUpdate", false)).SetFields(fields).ToList<BsonDocument>();
            foreach (BsonDocument document in this.records)
            {
                string str = document.Text("url");
                int startIndex = str.LastIndexOf("/");
                string oldValue = str.Substring(startIndex, str.Length - startIndex);
                str = str.Replace(oldValue, "");
                str = string.Format("https://www.huxiu.com{0}", str);
                UrlInfo target = new UrlInfo(str) {
                    Depth = 1,
                    Authorization = document.Text("_id")
                };
                UrlQueue.Instance.EnQueue(target);
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

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功" + allTask.Count.ToString());
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
                return "FocusCity";
            }
        }
 

        
    }
}

