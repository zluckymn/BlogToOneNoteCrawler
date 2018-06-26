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
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// app 土地公告与土地预告
    /// https://appapi.3g.fang.com/LandApp/LandNotice?count=20&page=1&mode=PushLand&wirelesscode=904e5fed8bef6df7c0fb79bc0e897ac4&r=6cFqC2ZmBQA%3D
    /// https://appapi.3g.fang.com/LandApp/LandNotice?count=20&page=1&mode=PushLand&wirelesscode=904e5fed8bef6df7c0fb79bc0e897ac4&r=6cFqC2ZmBQA%3D
    /// https://appapi.3g.fang.com/LandApp/MarketSearch?scity=%E5%8C%97%E4%BA%AC%E5%B8%82&imei=000000000000000&psize=20&ordertype=2&type=1&mode=json&pindex=2&ordername=landstartdate&messagename=search&wirelesscode=e72f6a278fd134dab78adbcde73c8341&r=kNLFpDufcFo%3D(北京)
    /// <summary>
    /// 用于地块地区获取,经过改程序跑完还是没有县市 表示该房子已经变更，或者不存在了,支持用户更新的地块更新
    /// </summary>
    public class WeiXinUrlDetailCrawler : ISimpleCrawler
    {

        object lock_obj = new object();
        //private   string connStr = "mongodb://MZsa:MZdba@192.168.1.121:37088/WorkPlanManage";
        DataOperation dataop = null;
        private CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        private BloomFilter<string> filter;

        private const string _DataTableName = "WeiXinArticleUrl";//存储的数据库表明

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
            get { return _DataTableName; }

        }
        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableNameCity
        {
            get { return _DataTableName + "CityEXURL"; }

        }
        /// <summary>
        /// 需要新增的
        /// </summary>
        public string DataTableNameNeedAdd
        {
            get { return _DataTableName + "NeedAddUrl"; }

        }


        List<BsonDocument> cityUrlList = new List<BsonDocument>();
        List<BsonDocument> landUrlList = new List<BsonDocument>();//没有县市的Url
        List<BsonDocument> allLandUrlList = new List<BsonDocument>();//没有县市的Url
        /// <summary>
        /// 谁的那个
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public WeiXinUrlDetailCrawler(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
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
            Settings.ThreadCount = 1;
            Console.WriteLine("正在获取已存在的url数据");

            Settings.CurWebProxy =null;
            //布隆url初始化,防止重复读取url
            allLandUrlList = dataop.FindAllByQuery(DataTableNameURL, Query.NE("isUpdate", "2")).ToList();//城市url

            //这里只提取去有县市区域的url 没有县市url的需要手动在执行一次
            foreach (var cityUrl in allLandUrlList.Distinct())//
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(cityUrl.Text("url")) { Depth = 1 });
            }


            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {
                //  Console.WriteLine("zluckymn模拟登陆成功");
            }
            else
            {
                Console.WriteLine("zluckymn模拟登陆失败");
            }

        }
        Dictionary<string, List<BsonDocument>> cityLandObjectList = new Dictionary<string, List<BsonDocument>>();
        public static Object lockRoom = new System.Object();
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        public void DataReceive(DataReceivedEventArgs args)
        {
            var hmtl = args.Html;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(args.Html);//提取文章
            var root = htmlDoc.DocumentNode;
            if (root == null) return;

            var title = htmlDoc.GetElementbyId("activity-name");
            if (title == null) return;
            var content = htmlDoc.GetElementbyId("js_content");
            if (content == null) return;
            var date = htmlDoc.GetElementbyId("post-date");
            if (date == null) return;

            var hitUrl = allLandUrlList.Where(c => c.Text("url") == args.Url).FirstOrDefault();
            if (hitUrl == null) return;

            var removeNode = content.ChildNodes.Where(c => c.Name == "p" && c.InnerText.Contains("可快速关注我们") || c.InnerText.Contains("关注「DotNet」")).ToList();
            for (var i = 0; i < removeNode.Count; i++)
            {
                content.RemoveChild(removeNode[i]);
            }

            var allImgNodes = content.SelectNodes("//img");
            var contentStr = content.InnerHtml.Replace("data-src=\"", "src=\"");
            foreach (var imgNode in allImgNodes)
            {
                var imgSrcAttr = imgNode.Attributes["data-src"];
                if (imgSrcAttr == null) continue;
                var imgPath = imgSrcAttr.Value;
                var imgExtAttr = imgNode.Attributes["data-type"];
                var ext = string.Empty;
                if (imgExtAttr != null)
                {
                    ext = imgExtAttr.Value;
                }
                else
                {
                    ext = "png";
                }
                var imgFileName = string.Format("{0}.{1}", imgPath.GetHashCode().ToString(), ext);//保存的图片文件名
                var imgPhyFilePath = string.Format("D:/WeiXin/{1}/{0}", imgFileName,DateTime.Now.ToString("yyyyMMdd"));
                var imgSitFilePath = string.Format("/UploadFiles/WeiXinNew/{1}/{0}", imgFileName, DateTime.Now.ToString("yyyyMMdd"));
                //对内容中的图片进行下载
                if (!File.Exists(imgPhyFilePath))
                {
                    lock (lockRoom)
                    {
                        ImgSave(imgPath, imgPhyFilePath);
                    }
                }
                //对内容进行替换
                contentStr = contentStr.Replace(imgPath, imgSitFilePath);//图片地址替换
            }
            var curAddBsonDocument = new BsonDocument();
            curAddBsonDocument.Add("name", title.InnerText.Trim());
            curAddBsonDocument.Add("content", contentStr);
            curAddBsonDocument.Add("date", date.InnerText);
            curAddBsonDocument.Add("isUpdate", "1");
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = curAddBsonDocument, Name = DataTableName, Query = Query.EQ("url", args.Url), Type = StorageType.Update });

            Console.WriteLine("{0}{1}", title, date.InnerText);

        }
        private static void ImgSave(string imgUrl, string imgPath)
        {
            WebClient mywebclient = new WebClient();
            var fileInfo = new FileInfo(imgPath);
            if (!Directory.Exists(fileInfo.Directory.FullName))
            {
               
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            mywebclient.DownloadFile(imgUrl, imgPath);
        }

        /// <summary>
        /// 图片下载
        /// </summary>
        /// <param name="imgUrl"></param>
        /// <param name="imgPath"></param>
        private static void ImgSaveGzip(string imgUrl, string imgPath)
        {

            try
            {
                //命名空间System.Net下的HttpWebRequest类
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imgUrl);
                //参照浏览器的请求报文 封装需要的参数 这里参照ie9
                //浏览器可接受的MIME类型
                request.Accept = "image/webp,*/*;q=0.8";
                //包含一个URL，用户从该URL代表的页面出发访问当前请求的页面
                request.Referer = "http://www.fang99.com/";
                //浏览器类型，如果Servlet返回的内容与浏览器类型有关则该值非常有用
                request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; Trident/5.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; .NET4.0C; .NET4.0E)";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                //请求方式
                request.Method = "Get";
                //是否保持常连接
                request.KeepAlive = false;
                request.Headers.Add("Accept-Encoding", "gzip, deflate,sdch");

                //响应
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //判断响应的信息是否为压缩信息 若为压缩信息解压后返回
                if (response.ContentEncoding == "gzip")
                {
                    MemoryStream ms = new MemoryStream();
                    GZipStream zip = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                    byte[] buffer = new byte[1024];
                    int l = zip.Read(buffer, 0, buffer.Length);
                    while (l > 0)
                    {
                        ms.Write(buffer, 0, l);
                        l = zip.Read(buffer, 0, buffer.Length);
                    }


                    //result = Encoding.UTF8.GetString(ms.ToArray());

                    FileStream fs = new FileStream(imgPath, FileMode.OpenOrCreate);
                    BinaryWriter w = new BinaryWriter(fs);
                    w.Write(ms.ToArray());
                    fs.Close();
                    ms.Close();

                    ms.Dispose();
                    zip.Dispose();
                }

            }
            catch (Exception exception)
            {

                //   throw;
            }
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
