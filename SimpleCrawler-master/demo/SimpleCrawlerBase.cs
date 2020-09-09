using DotNet.Utilities;
using Helper;
using HtmlAgilityPack;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MZ.RabbitMQ;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Yinhe.ProcessingCenter;
using Yinhe.ProcessingCenter.DataRule;

namespace SimpleCrawler.Demo
{
    /// <summary>
    /// 注销吊销专题，https://www.mingluji.com/zhuxiaodiaoxiao/update-list?page=0 地方人物库，后续每天定期爬取
    /// 北京 天津 河北 山西 内蒙古 辽宁 吉林 黑龙江 上海 江苏 浙江 安徽 福建 江西 山东 河南 湖北 湖南 广东 广西 海南 重庆 四川 贵州 云南 西藏 陕西 甘肃 青海 宁夏 新疆 香港 澳门
    /// </summary>
    public class SimpleCrawlerBase : ISimpleCrawler
    {



        public DataOperation dataop = null;
        public CrawlSettings Settings = null;
        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL：http://www.cnblogs.com/heaad/archive/2011/01/02/1924195.html
        /// </summary>
        public BloomFilter<string> filter;
        public BloomFilter<string> idFilter;

        public int addCount = 0;
        public int updateCount = 0;
        public int globalTotalCount=0;
        internal string uniqueKeyField = "guid";
        internal string updatedField = "isUpdated";
        internal string updatedValue = "1";
        private string _DataTableName = "";//存储的数据库表名
        private string _DataTableCategoryName = "";//存储的数据库表名目录名

        internal string guidDetail = "";

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableName
        {
            get { return _DataTableName; }
            set { _DataTableName = value; }

        }

        /// <summary>
        /// 返回
        /// </summary>
        public string DataTableCategoryName
        {
            get { return _DataTableCategoryName; }
            set { _DataTableCategoryName = value; }

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
            get { return _DataTableName + "City"; }

        }
        /// <summary>
        /// 需要新增的
        /// </summary>
        public string DataTableNameNeedAdd
        {
            get { return _DataTableName + "NeedAddUrl"; }

        }

        /// <summary>
        /// 需要新增的
        /// </summary>
        public int CurThreadId { get; set; }
         
         
        /// <summary>
        /// 数据实体队列，目前用于前期批量重载，每次执行完一次后取出一条新的数据
        /// </summary>
        public DynamicQueue<BsonDocument> DataQueue
        {
            get { return DynamicQueue<BsonDocument>.Instance; }

        }


        /// <summary>
        /// 数据实体队列，目前用于前期批量重载，每次执行完一次后取出一条新的数据
        /// </summary>
        public void DataQueueInit(List<BsonDocument> docList)
        {
            Parallel.ForEach(docList, (doc) =>
            {
                DynamicQueue<BsonDocument>.Instance.EnQueue(doc);
            });

        }

        /// <summary>
        /// 数据实体队列，目前用于前期批量重载，每次执行完一次后取出一条新的数据
        /// </summary>
        public BsonDocument DataDeQueue(Action<BsonDocument> action)
        {
            var doc = DynamicQueue<BsonDocument>.Instance.DeQueue();
            if (action != null)
            {
                action(doc);
            }
            return doc;
        }

        /// <summary>
        /// 数据实体队列，目前用于前期批量重载，每次执行完一次后取出一条新的数据
        /// </summary>
        public void DataEnQueue(BsonDocument doc)
        {
            DynamicQueue<BsonDocument>.Instance.EnQueue(doc);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="_Settings"></param>
        /// <param name="filter"></param>
        public SimpleCrawlerBase(CrawlSettings _Settings, BloomFilter<string> _filter, DataOperation _dataop)
        {
            Settings = _Settings; filter = _filter; dataop = _dataop;
            idFilter = new BloomFilter<string>(1000000);
            
        }
        virtual
        public void SettingInit()//进行Settings.SeedsAddress Settings.HrefKeywords urlFilterKeyWord 基础设定
        {


            Settings.RegularFilterExpressions.Add("XXX");//不添加其他
            if (SimulateLogin())
            {

            }
            else
            {
                Console.WriteLine("zluckymn模拟登陆失败");
            }

        }
        Dictionary<string, List<BsonDocument>> cityLandObjectList = new Dictionary<string, List<BsonDocument>>();

        internal bool CanLoadNewData()
        {
            if (CurThreadId == 0)
            {
                CurThreadId = Thread.CurrentThread.ManagedThreadId;
            }
            if (UrlQueue.Instance.Count <= 100 && Thread.CurrentThread.ManagedThreadId == CurThreadId)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 数据接收处理，失败后抛出NullReferenceException异常，主线程会进行捕获
        /// </summary>
        /// <param name="args">url参数</param>
        virtual
        public void DataReceive(DataReceivedEventArgs args)
        {
           
         
        }
        /// <summary>
        ///  更新目录
        /// </summary>
        /// <param name="guid"></param>
        public void UpdateDataParentCategory(string guid)
        {
            var updateDoc = new BsonDocument();
            updateDoc.Set("guid", guid);
            updateDoc.Set(updatedField, updatedValue);
            UpdateData(updateDoc, dataTable: DataTableCategoryName);
        }
        public bool hasExistObj(string guid, string fieldName = "guid")
        {
            return (this.dataop.FindCount(this.DataTableName, Query.EQ(fieldName, guid)) > 0);
        }
        public BsonDocument GetExistObj(string guid, string fieldName = "guid")
        {
            return this.dataop.FindOneByQuery(this.DataTableName, Query.EQ(fieldName, guid));
        }
        public bool hasExistObj(string tableName, string guid, string fieldName = "guid")
        {
            return (this.dataop.FindCount(tableName, Query.EQ(fieldName, guid)) > 0);
        }
        public JObject GetJsonObject(string html)
        {
            JObject jsonObj = JObject.Parse(html);
            return jsonObj;
        }

        public bool OnGetNothing(int add)
        {
            ///超时后退出
            if (add > 0)
            {
                Settings.curNoCountTimes = Settings.noCountTimes;//重置
            }
            else
            {
                Settings.curNoCountTimes -= 1;

            }
            if (Settings.curNoCountTimes <= 0)
            {
                return true;
            }
            return false;
        }
        public string GetJsonValueString(JToken node, string columnName)
        {

            if (node != null && node.ToString().Contains(string.Format("\"{0}\":", columnName)))
            {
                return node[columnName].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public int GetJsonValueInt(JToken node, string columnName)
        {

            if (node != null && node.ToString().Contains(string.Format("\"{0}\":", columnName)))
            {
                if (int.TryParse(node[columnName].ToString(), out int newVal))
                {
                    return newVal;
                }
                else
                {
                    return 0;
                }

            }
            else
            {
                return 0;
            }
        }

        public BsonDocument GetBsonDocument(JToken node)
        {
            // var  bsonDoc= BsonDocument.Parse(node.ToJson());
            var bsonDoc = GetBsonDocument(node.ToString());
            return bsonDoc;
        }
        public BsonDocument GetBsonDocument(string strJson)
        {
            // var  bsonDoc= BsonDocument.Parse(node.ToJson());
            var bsonDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(strJson);
            return bsonDoc;
        }
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="dataTable"></param>
        public void AddData(BsonDocument doc, string dataTable = "", IMongoQuery query = null, string keyFiled = "guid")
        {
            if (string.IsNullOrEmpty(dataTable))
            {
                dataTable = DataTableName;
            }
            query = Query.EQ(keyFiled, doc.Text(keyFiled));
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = doc, Name = dataTable, Query = query, Type = StorageType.Update });

        }
        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="query"></param>
        /// <param name="dataTable"></param>
        public void UpdateData(BsonDocument doc, string dataTable = "", IMongoQuery query = null, string keyFiled = "guid")
        {
            if (string.IsNullOrEmpty(dataTable))
            {
                dataTable = DataTableName;
            }
            if (query == null)
            {
                query = Query.EQ(keyFiled, doc.Text(keyFiled));
            }
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = doc, Name = dataTable, Query = query, Type = StorageType.Update });
            guidDetail = doc.Text(keyFiled);
        }
        /// <summary>
        /// 更新数据状态
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="query"></param>
        /// <param name="dataTable"></param>
        public void UpdateDataStatusOnly(string dataTable = "", IMongoQuery query = null,string keyValue="",string keyFiled="guid")
        {
            if (string.IsNullOrEmpty(dataTable))
            {
                dataTable = DataTableName;
            }
            if (query == null)
            {
                query = Query.EQ(keyFiled, keyValue);
            }
            var updateDoc = new BsonDocument();
            updateDoc.Set(updatedField, updatedValue);
            DBChangeQueue.Instance.EnQueue(new StorageData() { Document = updateDoc, Name = dataTable, Query = query, Type = StorageType.Update });
            guidDetail = keyValue;
        }
         

        public void PushData(BsonDocument doc, string keyFiled = "guid", string dataTable = "", string arrayFieldName = "",Action<BsonDocument>addAction=null, Action<BsonDocument>updateAction = null)
        {
            var bsonArray = new BsonArray();
            //此处将单字段切换成数组方式存储
            if (!string.IsNullOrEmpty(arrayFieldName))
            {
                bsonArray.Add(doc.Text(arrayFieldName));
                doc.Set(arrayFieldName, bsonArray);
            }

            if (string.IsNullOrEmpty(dataTable))
            {
                dataTable = DataTableName;
            }
            var keyValue = doc.Text(keyFiled);
            if (!idFilter.Contains(keyValue) && !hasExistObj(keyValue, keyFiled))
            {
                AddData(doc,keyFiled: keyFiled, dataTable: dataTable);
                addAction?.Invoke(doc);
                System.Threading.Interlocked.Increment(ref addCount);
                
            }
            else
            {
                ///当字段重复的时候进行数组行添加
                if (!string.IsNullOrEmpty(arrayFieldName))
                {
                    var curExistObj = GetExistObj(keyValue, keyFiled);
                    BsonArray existArray = new BsonArray();
                    if (curExistObj.ContainsColumn(arrayFieldName))
                    {
                        existArray = curExistObj[arrayFieldName] as BsonArray ?? new BsonArray();
                    }
                    if (!existArray.Contains(doc.Text(arrayFieldName)))
                    {
                        existArray.AddRange(bsonArray);
                        existArray=BsonArray.Create(existArray.Distinct().ToList());
                        if (existArray.Count > 4)
                        {

                        }
                        doc.Set(arrayFieldName, existArray);
                    }
                }
                
                UpdateData(doc, keyFiled: keyFiled, dataTable: dataTable);
                updateAction?.Invoke(doc);
                System.Threading.Interlocked.Increment(ref updateCount);
            }
            guidDetail = doc.Text(keyFiled);
        }

        public void ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
        public void ShowStatus(string result="")
        {
            var curProxyAddress = Settings.CurWebProxy != null ? Settings.CurWebProxy.Address.ToString() : String.Empty;
            Console.WriteLine($"截至目前位置总个数_{globalTotalCount} 新增_{addCount} 更新_{updateCount} guid:{guidDetail}");
            Console.WriteLine($"重试队列待请求url个数:{UrlRetryQueue.Instance.Count}_{result}");
            Console.WriteLine($"url队列待请求url个数:{UrlQueue.Instance.Count}");
            Console.WriteLine($"数据库等待更新个数:{DBChangeQueue.Instance.Count} 线程ID：{Thread.CurrentThread.ManagedThreadId} 代理地址：{curProxyAddress}");

        }

        public void ShowMessageInfo(string msg,bool isAppend=false)
        {
            Console.WriteLine(msg);
        }
        public static object lockThis = new object();
        public static List<Task> allTask = new List<Task>();
        #region 文件下载
        internal void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Console.WriteLine("下载成功");
        }
        internal void DownLoadFile(string imgSrc,string fileName, string fileDirName = "SimpleCrawlerDownLoad")
        {
            try
            {
               
                lock (lockThis)
                {
                    var fileDir = string.Empty;
                    if (string.IsNullOrEmpty(fileDir))
                    {
                        fileDir = AppDomain.CurrentDomain.BaseDirectory + $"{fileDirName}";
                    }

                    if (!Directory.Exists(fileDir))
                    {
                        Directory.CreateDirectory(fileDir);
                    }
                    var filePath = string.Format("{0}/{1}", fileDir, fileName);
                    var result = ImgSave(imgSrc, filePath);
                    allTask.Add(result);// 添加到等待日志完成
                    Console.WriteLine("完成下载{0}", imgSrc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        /// <summary>
        /// http://www.fang99.com/
        /// </summary>
        /// <param name="imgPath"></param>
        private async static Task<string> ImgSave(string imgUrl, string imgPath)
        {
            try
            {
                WebClient webClient = new WebClient();
                await webClient.DownloadFileTaskAsync(new Uri(imgUrl), imgPath);
                return imgUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return imgUrl;
            //await Task.Run(() =>
            //{
            //    WebClient webClient = new WebClient();
            //    webClient.DownloadFile(new Uri(imgUrl), imgPath);

            //});

        }
        #endregion

        #region 数据查找


        /// <summary>
        /// 获取待更新数据的数据
        /// </summary>
        /// <returns></returns>
        public List<BsonDocument> FindDataForUpdate(IMongoQuery query = null, string[] fields = null, string dataTableName = "",int limit=0)
        {
            if (string.IsNullOrEmpty(dataTableName))
            {
                dataTableName = DataTableName;
            }
            if (query == null)
            {
                query = Query.Or(Query.Exists(updatedField, false), Query.NE(updatedField, updatedValue));
            }
            if (fields == null)
            {
                fields = new string[] { "guid" };
            }
            var allHitObjList = new List<BsonDocument>();
            if (limit == 0)
            {
                allHitObjList = dataop.FindFieldsByQuery(dataTableName, query, fields)
                   .ToList();
            }
            else {
                allHitObjList = dataop.FindFieldsByQuery(dataTableName, query, fields).SetLimit(limit)
                      .ToList();
            }
           
            return allHitObjList;
        }

        /// <summary>
        /// 获取待更新数据的数据
        /// </summary>
        /// <returns></returns>
        public MongoCursor<BsonDocument> FindDataForUpdateLimit(IMongoQuery query = null, string[] fields = null, string dataTableName = "",int limit=100)
        {
            if (string.IsNullOrEmpty(dataTableName))
            {
                dataTableName = DataTableName;
            }
            if (query == null)
            {
                query = Query.Or(Query.Exists(updatedField, false), Query.NE(updatedField, updatedValue));
            }
            if (fields == null)
            {
                fields = new string[] { "guid" };
            }
            var allHitObjList = dataop.FindFieldsByQuery(dataTableName, query, fields).SetLimit(limit);
            return allHitObjList;
        }


        /// <summary>
        /// 等待更新的用户列表
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fieldName"></param>
        public void InitialForUpdateUrl(string url, string guid)
        {

            var curUrl = string.Format(url, guid);
            AddUrl(guid);
        }

        /// <summary>
        /// 等待更新的用户列表
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fieldName"></param>
        public void AddUrl(string curUrl,string uniqueKey="")
        {

          
            if (!filter.Contains(curUrl))
            {
                UrlQueue.Instance.EnQueue(new UrlInfo(curUrl) { UniqueKey = uniqueKey });
                filter.Add(curUrl);// 防止执行2次
            }
        }
        #endregion

        #region url 操作


        private string ReplaceParamAllowEmpty(string url, string joinStr, string paramName, string oldValue, string newValue)
        {
             
            if (url.Contains(paramName))
            {
                return url.Replace(string.Format(joinStr + "{0}={1}", paramName, oldValue), string.Format(joinStr + "{0}={1}", paramName, newValue));
            }
            return (url + string.Format(joinStr + "{0}={1}", paramName, newValue));
        }

        private string ReplaceParam(string url,string joinStr, string paramName, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(joinStr))
            {
                joinStr = "&";
            }
            if (url.Contains(paramName))
            {
                return url.Replace(string.Format(joinStr+"{0}={1}", paramName, oldValue), string.Format(joinStr+"{0}={1}", paramName, newValue));
            }
            return (url + string.Format(joinStr+"{0}={1}", paramName, newValue));
        }

        public string ReplaceUrlParam(string _curUrl, string parameName, string newValue, string joinStr="&",bool allowEmpty=false)
        {
            var oldValue =HttpUtility.UrlEncode(GetUrlParam(_curUrl, parameName));
            if (allowEmpty == false)
            {
                return this.ReplaceParam(_curUrl, joinStr, parameName, oldValue, newValue);
            }
            else
            {
                return this.ReplaceParamAllowEmpty(_curUrl, joinStr, parameName, oldValue, newValue);
            }
           
        }
 

        public string GetGuidFromUrl(string url,string begin,string end=".")
        {
            int num = url.LastIndexOf(begin);
            int num2 = url.LastIndexOf(end);
            if (num2 == -1&& end== "|END|")
            {
                num2 = url.Length ;
            }
            if ((num != -1) && (num2 != -1))
            {
                if (num > num2)
                {
                    int num3 = num;
                    num = num2;
                    num2 = num3;
                }
                return url.Substring(num + 1, (num2 - num) - 1);
            }
            return string.Empty;
        }

        public string GetInnerText(HtmlNode node)
        {
            if ((node == null) || string.IsNullOrEmpty(node.InnerText))
            {
                throw new NullReferenceException();
            }
            return node.InnerText.Trim();
        }

        public static string GetQueryString(string url)
        {
            int index = url.IndexOf("?");
            if (index != -1)
            {
                return url.Substring(index + 1, (url.Length - index) - 1);
            }
            return url;
        }

        public string[] GetStrSplited(string str)
        {
            string[] separator = new string[] { ":", "：" };
            return str.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }

        public string GetUrlParam(string url, string name)
        {
            NameValueCollection values = HttpUtility.ParseQueryString(GetQueryString(url));
            return ((values[name] != null) ? values[name].ToString() : string.Empty);
        }
        /// <summary>
        /// 获取node属性
        /// </summary>
        /// <param name="aNode"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public string GetNodeAttribute(HtmlNode aNode,string column)
        {
            if (aNode != null && aNode.Attributes[column] != null)
            {
                var site = aNode.Attributes[column].Value;
                return site.Trim();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetCityCode(string url)
        {
            var index = url.LastIndexOf("/");
            var endIndex = url.IndexOf("_");
            var cityCode = string.Empty;
            if (index != -1 && endIndex != -1)
            {
                cityCode = url.Substring(index + 1, endIndex - index - 1);

            }
            return cityCode;
        }
        /// <summary>
        /// http://fdc.fang.com/data/land/310100_310101________1_1.html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string GetRegionCode(string url, string cityCode)
        {
            var fixUrl = url.Replace(cityCode + "_", "");
            return GetCityCode(fixUrl);
        }
        public string ValeFix(string str)
        {
            return str.Replace("\n", "").Replace("\r", "").Trim();
        }
        #endregion
        /// <summary>
        /// IP限定处理，ip被限制 账号被限制跳转处理
        /// </summary>
        /// <param name="args"></param>
        virtual
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
        /// 获取时间戳 
        /// </summary> 
        /// <returns></returns> 
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
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
        internal void StartDBChangeProcess()
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

        #region
        /// <summary>
        /// 消息队列初始化
        /// </summary>
        internal void InitialMQ(string mqName= "mz.core.enterprise_info")
        {
            Console.WriteLine($"正在初始化队列{mqName}");
            MQHelper.Instance().Init(mqName);
            Console.WriteLine($"初始化队列成功");
        }
        /// <summary>
        /// 推送消息到队列
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        internal   bool PushMessageAsync(BsonDocument doc)
        {
            var result =   MQHelper.Instance().Publish<String>(doc.ToJson());
            Console.WriteLine($"推送队列结果{result}");
            return result;
        }
        /// <summary>
        /// 消息队列取消
        /// </summary>
        /// <param name="doc"></param>
        internal void DisposeMQ(BsonDocument doc)
        {
            MQHelper.Instance().Dispose();
        }
        #endregion

        #region GetWebNode

        internal string QuickGetHtmlNodeValue(HtmlNode baseInfNode, string beginTdName, string endTdName)
        {
            
            if (baseInfNode != null)
            {
             var text = baseInfNode.InnerText.ToolsSubStr(beginTdName, endTdName).Trim();
            }
            return string.Empty;
        }
        #endregion

 

    }

}
