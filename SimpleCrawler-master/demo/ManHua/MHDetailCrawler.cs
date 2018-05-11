using DotNet.Utilities;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 用于城市与区域代码初始化
    /// </summary>
    public class MHDetailCrawler : ISimpleCrawler
    {

        //private   string connStr = "mongodb://MZsa:MZdba@59.61.72.34:37088/WorkPlanManage";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：
        ///  www.hhcool.com/cool62061/1.html?s=10&d=0
        /// 结束http://www.hhcool.com/cool62061/253.html?s=10&d=0
        /// </summary>
        private BloomFilter<string> filter;
        private BloomFilter<string> schoolIdFilter = new BloomFilter<string>(8000000);
        private const string _DataTableName = "MH_Cartoon";//存储的数据库表名

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
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public MHDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
        }


        List<BsonDocument> records = new List<BsonDocument>();
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {
            //webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
            //种子地址需要加布隆过滤

            //Settings.Depth = 4;
            //代理ip模式
            Settings.IPProxyList = new List<IPProxy>();
            Settings.IgnoreSucceedUrlToDB = true;//不添加地址到数据库
            Settings.MaxReTryTimes = 20;
            Settings.ThreadCount =5;

            Console.WriteLine("正在获取已存在的url数据");
            //布隆url初始化,防止重复读取url
            Console.WriteLine("正在初始化选择url队列");
            //http://www.hhcool.com/cool62061/{0}.html?s=10&d=0 253
            //http://www.hhcool.com/cool33788/1.html?s=4 91
             records = dataop.FindAllByQuery("MH_Cartoon", Query.NE("isUpdate", 1)).ToList();
            //iCPH
            foreach (var page in records)
            {
                var mhUrl = page.Text("url")+ "&checkPageCount=1";
                if (!filter.Contains(mhUrl))//具体页面
                {
                    UrlQueue.Instance.EnQueue(new UrlInfo(mhUrl) { Depth = 1 });
                }
            }
           // UrlQueue.Instance.EnQueue(new UrlInfo("http://www.hhcool.com/cool286073/1.html?s=11&d=0"+"&checkPageCount=1") { Depth = 1 });
            
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

        private void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功");
        }

        public static object lockThis = new object();
        public static List<Task> allTask = new List<Task>();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// cool62061/1.html?s=10&d=0
        /// 
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var root = htmlDoc.DocumentNode;
            //获取目录名
            // http://www.hhcool.com/cool286073/2.html?s=11&d=0
            var dirName = Toolslib.Str.Sub(args.Url, "http://www.hhcool.com/", "/");//剧集
            if (string.IsNullOrEmpty(dirName))
            {
                dirName = Toolslib.Str.Sub(args.Url, "http://www.hhcool.com//", "/");//剧集
            }
            var fileName = Toolslib.Str.Sub(args.Url, dirName + "/", ".");//页数
            var curUrl = args.Url.Replace("&checkPageCount=1", "");
            var hitDoc = records.Where(c => c.Text("url").Contains(dirName)).FirstOrDefault();
            if (hitDoc == null)
            {
                Console.WriteLine("无对应对象");
            }
            var catName = hitDoc.Text("catName");
            //获取页数
            if (args.Url.Contains("checkPageCount"))
            {
                
                var pageNode = root.SelectSingleNode("//div[@class='cH1']/b");
                if (pageNode != null)
                {
                    var maxPageNum = Toolslib.Str.Sub(pageNode.InnerText, "/", "");
                    var maxNum = 0;
                    if (int.TryParse(maxPageNum.Trim(), out maxNum))
                    {
                       
                        for (var i = 2; i <= maxNum; i++)
                        {
                            var newIndex = string.Format("/{0}.html", i);
                            var mhUrl = curUrl.Replace("/1.html", newIndex);
                            if (!filter.Contains(mhUrl))//具体页面
                            {
                                UrlQueue.Instance.EnQueue(new UrlInfo(mhUrl) { Depth = 1 });
                            }
                        }
                    }
                    DBChangeQueue.Instance.EnQueue(new StorageData() { Document = new BsonDocument().Add("isUpdate",1) , Name = DataTableName,Query=Query.EQ("url", curUrl), Type = StorageType.Update });
                }
           }
            //获取图片地址
            var hdDomain= htmlDoc.GetElementbyId("hdDomain");//http://124.94201314.net/dm11/|http://165.94201314.net/dm11/
            var hdDomainStr = "";
            if (hdDomain == null) {
                Console.WriteLine("无服务器");
                return;
              }
            var hArray=hdDomain.Attributes["value"].Value.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            hdDomainStr = hArray[0];

            var ibodyDiv = htmlDoc.GetElementbyId("iBodyQ");
            if (ibodyDiv != null)
            {
                
                var img = ibodyDiv.SelectSingleNode("./img");
                if (img != null && img.Attributes["name"] != null)
                {
                   
                    if (string.IsNullOrEmpty(fileName)) return;

                    var name = img.Attributes["name"].Value.ToString();
                    if (string.IsNullOrEmpty(name)) { return; }
                    var src = string.Format("{0}{1}", hdDomainStr,unsuan(name));
                   // http://124.94201314.net/dm11//ok-comic11/m/mowuniangdexiangbanrichang/act_051/z_0001_10965.JPG
                    var fileDir = string.Format(@"F:/comic/Other/{0}/{1}", catName, hitDoc.Text("name")+dirName);
                    if (!Directory.Exists(fileDir))
                    {
                        Directory.CreateDirectory(fileDir);
                    }
                    var filePath = string.Format("{0}/{1}.jpg", fileDir, fileName);
                    lock (lockThis)
                    {
                        var result = ImgSave(src, filePath);
                            allTask.Add(result);// 添加到等待日志完成
                            result.GetAwaiter().OnCompleted(() =>
                            {
                                allTask.Remove(result);
                                Console.WriteLine("完成下载{0}", result.Result);
                            });
                     
                    }
                   
                }
            }
            else
            {
                Console.WriteLine("图片不存在");
            }
            if (UrlQueue.Instance.Count == 0)
            {
                Task.WaitAll(allTask.ToArray());
                Task.WhenAll(allTask).GetAwaiter().OnCompleted(()=>{
                    Console.WriteLine("所有下载完成");
                });
                Console.ReadLine();
            }

        }
        /// <summary>
        /// yexoooxopexytxqqxoooxopqxoptxqqxyqxywxyexooqxyexooqxoouxopqxoioxyexoowxoooxopwxqtxywxyqxyextextexttxttxywxqtxywxywxyqxerxoptxrqxoppxyrxoprxooixopupoiuytrewqxpa
        /// http://124.94201314.net/dm10//ok-comic10/w/wqmy/vol_01/99770_001LiEd.jpg
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string unsuan(string s)
        {
            var sw = "44123.com|hhcool.com";
            var su = "www.hhcool.com";
            var b = false;
            for (var i = 0; i < sw.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries).Length; i++)
            {
                if (su.IndexOf(sw.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries)[i]) > -1)
                {
                    b = true;
                    break;
                }
            }
            if (!b) return "";

            var x = s.Substring(s.Length - 1);
            var w = "abcdefghijklmnopqrstuvwxyz";
            var xi = w.IndexOf(x) + 1;
            var beginIndex = s.Length - xi - 12;
            var endIndex = s.Length - xi - 1;
            var sk = s.Substring(beginIndex, endIndex - beginIndex);
            s = s.Substring(0, s.Length - xi - 12);
            var k = sk.Substring(0, sk.Length - 1);
            var f = sk.Substring(sk.Length - 1);
            for (var i = 0; i < k.Length; i++)
            {
                //var str = "s=s.replace(/" + k.Substring(i, i + 1) + "/g,'" + i + "')";
                // eval("s=s.replace(/" + k.Substring(i, i + 1) + "/g,'" + i + "')");
                s = s.Replace(k[i].ToString(), i.ToString());
            }
            var ss = s.Split(new string[] { f }, StringSplitOptions.RemoveEmptyEntries);
            s = "";
            var intList = new List<int>();
            for (var i = 0; i < ss.Length; i++)
            {
                var charNum = (char)int.Parse(ss[i]);
                s += charNum.ToString();
            }

            return s;
        }
       
        /// <summary>
        /// http://www.fang99.com/
        /// </summary>
        /// <param name="imgPath"></param>
        private async static Task<string> ImgSave(string imgUrl, string imgPath)
        {
            WebClient webClient = new WebClient();
            await  webClient.DownloadFileTaskAsync(new Uri(imgUrl), imgPath);
            return imgUrl;
            //await Task.Run(() =>
            //{
            //    WebClient webClient = new WebClient();
            //    webClient.DownloadFile(new Uri(imgUrl), imgPath);

            //});
           
        }
 

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
            if (string.IsNullOrEmpty(args.Html) || args.Html.Contains("503 Service Unavailable"))//需要编写被限定IP的处理
            {
                return true;
            }
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);
            var ibodyDiv = htmlDoc.GetElementbyId("iBody");
            if (ibodyDiv == null)
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
