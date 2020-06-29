

using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Linq;
using Helpers;

namespace SimpleCrawler
{
    /// <summary>
    /// 签名生成helper
    /// </summary>
    public class TokenGenerateHelper
    {
        /// <summary>
        /// 根据url与请求参数进行签名生成
        /// https://www.shihuo.cn/app_swoole_zone/getDgComment?platform=ios&timestamp=1575460800000&v=6.6.5&token=97fe6e57bc6fd56193a36ebed21b88ea&id=977&level=2&page=1&page_size=20&tag_id=
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public static string TokenGenerater_ShiHuo(string url, string postData="")
        {
            var filterColumns = new string[] {"token" };
            var curToken = GetUrlParam(url, "token");
            var splitArray = url.Split(new string[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
            var preUrl =   splitArray[0];
            var queryString = string.Empty; ;
            if (splitArray.Length > 1)
            {
                queryString = splitArray[1];
            }

            var urlDic = HttpUtility.ParseQueryString(queryString);
            var postDic=  HttpUtility.ParseQueryString(postData);
            foreach (var postKey in postDic.AllKeys)
            {
                urlDic.Add(postKey, postDic[postKey]);
            }
            var allKeys = urlDic.AllKeys.OrderBy(c=>c).ToList();
            var encodeStr = new StringBuilder();
            foreach (var key in allKeys)
            {
                if (filterColumns.Contains(key)) continue;
                var value = urlDic[key].Trim();
                encodeStr.AppendFormat(value);
             }
            var allStr = encodeStr + "123456";
            var token = MD5EncodeHelper.Encode(allStr);

            return token.ToLower();
        }
 
        private static string GetUrlParam(string queryStr, string name)
        {
            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }
    }
 
}
