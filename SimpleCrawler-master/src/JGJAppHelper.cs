using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using System.Globalization;

namespace SimpleCrawler
{
    
    /// <summary>
    /// 吉工家
    /// </summary>
    //imei=133524428521974&mobile=gjhepUya1v4%252Bn6SKDUK7sg%253D%253D&wirelesscode=eefb7c88d8489048649a57413b715b0a&r=zi6zhtmPAL8%3D
    public class JGJAppHelper
    {
        const string KEY = "OaxhSsnvFnRCUql53jVDUVVp26pQkYea";
        public static int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        }
        /// <summary>
        /// use sha1 to encrypt string
        /// </summary>
        private  string SHA1_Encrypt(string Source_String)
        {
            byte[] StrRes = Encoding.Default.GetBytes(Source_String);
            HashAlgorithm iSHA = new SHA1CryptoServiceProvider();
            StrRes = iSHA.ComputeHash(StrRes);
            StringBuilder EnText = new StringBuilder();
            foreach (byte iByte in StrRes)
            {
                EnText.AppendFormat("{0:x2}", iByte);
            }
            return EnText.ToString();
        }
        public string FixJGJUrl(string url)
        {
            var r = ConvertDateTimeInt(DateTime.Now.AddSeconds(320)).ToString();
            var sign = SHA1_Encrypt(KEY + r);


            var _timestamp = GetUrlParam(url, "timestamp");
            var _sign = GetUrlParam(url, "sign");

            if (!string.IsNullOrEmpty(_timestamp) && _timestamp != r)
            {
                url = url.Replace(_timestamp, r);
            }
            if (!string.IsNullOrEmpty(_sign) && _sign != sign)
            {
                url = url.Replace(_sign, sign);
            }

            return url;
        }
        private static string GetUrlParam(string queryStr, string name)
        {

            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }
        public UrlInfo FixJGJUrl(UrlInfo urlInfo)
        {
            var r = ConvertDateTimeInt(DateTime.Now.AddSeconds(320)).ToString();
            var sign = SHA1_Encrypt(KEY + r);

            var url = urlInfo.UrlString;
            var _timestamp = GetUrlParam(url, "timestamp");
            var _sign = GetUrlParam(url, "sign");

            if (!string.IsNullOrEmpty(_timestamp) && _timestamp != r)
            {
                url = url.Replace(_timestamp, r);
            }
            if (!string.IsNullOrEmpty(_sign) && _sign != sign)
            {
                url = url.Replace(_sign, sign);
            }

            return new UrlInfo(url) {  UrlString=url, Depth= urlInfo.Depth};
        }
    }
}  


