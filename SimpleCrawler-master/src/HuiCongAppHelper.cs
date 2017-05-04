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
    /// 模拟大小写排序
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyStringComparer<T> : IComparer<T>
    {
        private CompareInfo myComp = CultureInfo.InvariantCulture.CompareInfo;
        private CompareOptions myOptions = CompareOptions.Ordinal;
        public MyStringComparer()
        {

        }

        public int Compare(T xT, T yT)
        {

            if (xT == null) return -1;
            if (yT == null) return 1;
            var x = xT.ToString();
            var y = yT.ToString();
            if (x == y) return 0;
            String sa = x as String;
            String sb = y as String;

            if (sa != null && sb != null)
                return myComp.Compare(sa, sb, myOptions);
            throw new ArgumentException("x and y should be strings.");
        }
    }

    //imei=133524428521974&mobile=gjhepUya1v4%252Bn6SKDUK7sg%253D%253D&wirelesscode=eefb7c88d8489048649a57413b715b0a&r=zi6zhtmPAL8%3D
    public class HuiCongAppHelper
    {
       
        private const string DESKEY= "lifgnfdfg2896934133gwnkdstvjxeh";

        /// <summary>
        /// 获取慧聪网验证码
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public   string GetHuiCongAuthorizationCode(string url)
        {
            url = url.Replace("http://z.hc360.com", "").Replace("http://openapi.m.hc360.com", "");
            var urlArr = url.Split(new string[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
            var queryStr = string.Empty;
            var pathStr = string.Empty;
            if (urlArr.Length >= 1)
            {
                pathStr = urlArr[0];
            }
            if (urlArr.Length >= 2)
            {

                queryStr = GetTreerString(urlArr[1]);
            }
             var result = UrlEncode(pathStr, Encoding.UTF8) + UrlEncode(queryStr, Encoding.UTF8);
            return getMd5Hash(result + DESKEY);
        }
        public   string GetTreerString(string paramString)
        {

            StringBuilder localStringBuilder = new StringBuilder();
            Dictionary<string, string> paramMap = getParamsMap(paramString);
            var keyList = paramMap.Select(c => c.Key).ToList();
            IComparer<string> compare = new MyStringComparer<string>();
            keyList.Sort(compare);//模拟treeMap排序
            foreach (var key in keyList)
            {
                string str1 = key;
                string str2 = paramMap[key];
                localStringBuilder.Append(str1 + "=" + str2);
            }
            return localStringBuilder.ToString();
        }




        private   Dictionary<string, string> getParamsMap(String paramString)
        {
            Dictionary<string, string> localTreeMap = new Dictionary<string, string>();
            String[] paramStringArr = paramString.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            while (i < paramStringArr.Length)
            {
                String[] arrayOfString = paramStringArr[i].Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                localTreeMap.Add(arrayOfString[0], arrayOfString[1]);
                i += 1;
            }
            return localTreeMap;
        }



        private  string GetUrlParam(string url, string name)
        {
            var queryStr = GetQueryString(url);
            var dic = HttpUtility.ParseQueryString(queryStr);
            var industryCode = dic[name] != null ? dic[name].ToString() : string.Empty;//行业代码
            return industryCode;
        }


        /// <summary>
        /// 获取url对应查询参数
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private  string GetQueryString(string url)
        {
            var queryStrIndex = url.IndexOf("?");
            if (queryStrIndex != -1)
            {
                var queryStr = url.Substring(queryStrIndex + 1, url.Length - queryStrIndex - 1);
                return queryStr;
            }
            return string.Empty;
        }


        /// <summary>
        /// 2次urldecode 大写
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static string DoubleUrlEncode(string temp, Encoding encoding)
        {
            return UrlEncode(UrlEncode(temp, Encoding.UTF8), Encoding.UTF8);
        }
        /// <summary>
        /// 转化为大写的urldecode
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private static string UrlEncode(string temp, Encoding encoding)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < temp.Length; i++)
            {
                string t = temp[i].ToString();
                string k = HttpUtility.UrlEncode(t, encoding);
                if (t == k)
                {
                    stringBuilder.Append(t);
                }
                else
                {
                    stringBuilder.Append(k.ToUpper());
                }
            }
            return stringBuilder.ToString();
        }
        static string getMd5Hash(string input)
        {
            // Create a new instance of the MD5CryptoServiceProvider object.
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("X2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        public static string Encode(string source, string _DESKey)
        {

            StringBuilder sb = new StringBuilder();
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] key = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                //byte[] iv = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                byte[] iv = new byte[8];
                byte[] dataByteArray = Encoding.UTF8.GetBytes(source);
                des.Mode = System.Security.Cryptography.CipherMode.CBC;
                des.Key = key;
                des.IV = iv;
                string encrypt = "";
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(dataByteArray, 0, dataByteArray.Length);
                    cs.FlushFinalBlock();
                    // encrypt =Base64.encode(ms.ToArray());
                    encrypt = Convert.ToBase64String(ms.ToArray());
                }
                return encrypt;
            }

        }
        /// <summary>
        /// des解密
        /// </summary>
        /// <param name="str"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Decode(string source, string sKey)
        {
            byte[] inputByteArray = System.Convert.FromBase64String(source);//Encoding.UTF8.GetBytes(source);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                //des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = new byte[8];
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }

        }
    }
}  


