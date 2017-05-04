// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlInfo.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace SimpleCrawler
{
    /// <summary>
    /// The url info.
    /// </summary>
    public class IPProxyHelper
    {
        private const string appKey = "05ff1ceb72950f70e0d4d9b722d16bf3";
        /// <summary>
        /// 返回Ip列表
        /// </summary>
        /// <returns></returns>
        public static List<IPProxy> GetIpProxyList(string qryType)
        {
           
            List<IPProxy> resultList = new List<IPProxy>();
            var url = string.Format("http://api.shikexin.com/ws/api/getFreeAgentIP?qryType={0}&appKey={1}",qryType, appKey);
            HttpHelper helper = new HttpHelper();
            HttpItem hi = new HttpItem() { URL = url, Method = "get", UserAgent= "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " ,Accept= "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" };
            var result=helper.GetHtml(hi);

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JObject jsonObj = JObject.Parse(result.Html);
                var status = jsonObj["reason"];
                if (status.ToString() == "successed")
                {
                    var dataList = jsonObj["data"]["dataList"];
                    foreach (var data in dataList)
                    {
                        var ipDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(data.ToString());
                        resultList.Add(new IPProxy(ipDoc.Text("ip"), ipDoc.Text("port")));
                    }
                }
                
             }
            return resultList;
        }
        

    }
}