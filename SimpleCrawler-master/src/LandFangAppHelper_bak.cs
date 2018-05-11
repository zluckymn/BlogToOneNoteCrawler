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

namespace SimpleCrawler
{ 
    
    //imei=133524428521974&mobile=gjhepUya1v4%252Bn6SKDUK7sg%253D%253D&wirelesscode=eefb7c88d8489048649a57413b715b0a&r=zi6zhtmPAL8%3D
    //文件替换2018.5.10文件被删除
    public class LandFangAppHelper_bak
    {
        private const string DESKEY="soufunss";
        private const string MESSAGENAME = "CheckMobile";
        private const string IMEI = "133524428521975";
        private const string REGMODE = "reg";
        private const string ISENCRYPT = "20150303";

        #region lanFang 地址获取初始化
        /// <summary>
        /// 返回发送短信的url格式化地址,
        /// </summary>
        private  string GetRegSEndSMSFormatUrl {
          get {
              //var url = string.Format("https://appapi.3g.fang.com/LandApp/SendSMS?isencrypt={0}&messagename={1}&mode={2}&imei={3}", ISENCRYPT, MESSAGENAME, "reg", IMEI);
              var url = string.Format("https://appapi.3g.fang.com/LandApp/SendSMS?isencrypt={0}&messagename={1}&mode={2}&imei={3}&newpassword", ISENCRYPT, MESSAGENAME, "reg", IMEI);
                url+="&mobile={0}";
                return url;
            }
        }

        /// <summary>
        /// 返回登陆地址,
        /// </summary>
        private  string GetCheckCodeFormatUrl
        {
            get {
                var url = "https://appapi.3g.fang.com/LandApp/checkcode?isencrypt="+ISENCRYPT+"&code={0}&mobile={1}&mode=reg&imei="+IMEI;
                return url;
            }
        }

          /// <summary>
        /// 返回获取最新推出的土地{0}为count为一页多少数量 {1} page为当前页数
        /// </summary>
        private  string GetLandPushFormatUrl {
          get {
              var url = "https://appapi.3g.fang.com/LandApp/LandNotice?count={0}&page={1}&mode=PushLand";
              return url;
            }
        }

      
         
        /// <summary>
        /// 传入电话号码返回对应的SMS发送地址
        /// </summary>
        /// <returns></returns>
        public  string InitRegSendSmsUrl(string mobile)
        {
            var curUrl = string.Format(GetRegSEndSMSFormatUrl, mobile);
            curUrl = FixUrl(curUrl);
            return curUrl;
        }

        /// <summary>
        /// 获取发送登陆
        /// </summary>
        /// <returns></returns>
        public  string InitCheckCodeUrl(string code,string mobile)
        {
            var curUrl = string.Format(GetCheckCodeFormatUrl,code, mobile);
            curUrl = FixUrl(curUrl);
            return curUrl;
        }
         /// <summary>
        ///  返回获取最新推出的土地
         /// </summary>
        /// <param name="count">一页多少数量</param>
        /// <param name="page">当前页数</param>
         /// <returns></returns>
        public  string InitPushLandUrl(string count, string page)
        {
            var curUrl = string.Format(GetLandPushFormatUrl, count, page);
            curUrl = FixUrl(curUrl);
            return curUrl;
        }

        private  string GetLandDetailFormatUrl = "https://appapi.3g.fang.com/LandApp/MarketDetail?messagename=LandInfoList&sParcelId={0}&isvip={2}&iUserID={1}&imei=" + IMEI;
         /// <summary>
        /// 返回地块详细信息
        /// </summary>
        /// <param name="sParcelId">地块ID</param>
        /// <param name="iUserId">iUserId：143636</param>
        /// <param name="isVip"></param>
        /// <returns></returns>
        public  string InitLandDetailUrl(string sParcelId, string iUserId,string isVip="false")
        {
            var curUrl = string.Format(GetLandDetailFormatUrl, sParcelId, iUserId, isVip);
            curUrl = FixUrl(curUrl);
            return curUrl;
        }
        #endregion

        /// <summary>
        /// 获取5个字符串的随机r字符串
        /// </summary>
        /// <returns></returns>
        public  string GetRandom()
        {
            var sb = new StringBuilder();
            string[] str = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            var random = new Random(0);
            for (var i = 0; i < 5; i++)
            {
                var hitR = random.Next(0, 35);
                var hitS = str[hitR];
                sb.Append(hitR);
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取编码后的电话号码,编码使用des后进行url大写decode
        /// </summary>
        /// <returns></returns>
        public  string GetPhoneEncode(string mobile)
        { 
            var encodeStr=Encode(mobile,DESKEY);
            return DoubleUrlEncode(encodeStr, Encoding.UTF8);
        }
        /// <summary>
        /// 获取编码后的电话号码,编码使用des后进行url大写decode
        /// </summary>
        /// <returns></returns>
        public  string GetPhoneDecode(string mobile)
        {
            var encodeStr = Decode(UrlDecode(UrlDecode(mobile, Encoding.UTF8), Encoding.UTF8), DESKEY);
            return encodeStr;
        }
     
        /// <summary>
        /// 获取编码后的电话号码,编码使用des后进行url大写decode
        /// </summary>
        /// <returns></returns>
        public  string GetRandomEncode(string randomStr)
        {
            var encodeStr = Encode(randomStr, DESKEY);
            return UrlEncode(encodeStr, Encoding.UTF8);
        }

        /// <summary>
        /// 获取编码后的电话号码,编码使用des后进行url大写decode
        /// </summary>
        /// <returns></returns>
        public  string GetRandomDecode(string randomStr)
        {
            var encodeStr = Decode(UrlDecode(randomStr,Encoding.UTF8), DESKEY);
            return encodeStr;
        }

        
        /// <summary>
        /// 将url中的wirelessCode进行重算
        /// </summary>
        /// <param name="preUrl"></param>
        /// <returns></returns>
        public string FixIUserIdUrl(string url, string value)
        {

            return FixUrl(url, "iUserID", value);
        }
        /// <summary>
        /// 将url中的wirelessCode进行重算
        /// </summary>
        /// <param name="preUrl"></param>
        /// <returns></returns>
        public string FixUrl(string url,string paramName,string value)
        {
            var oldParamValue = GetUrlParam(url, paramName);
            if (!string.IsNullOrEmpty(paramName))
            {
                url=url.Replace(string.Format("&{0}={1}", paramName, oldParamValue), string.Format("&{0}={1}", paramName, value));
            }
           
            return FixUrl(url);
        }

        /// <summary>
        /// 将url中的wirelessCode进行重算
        /// </summary>
        /// <param name="preUrl"></param>
        /// <returns></returns>
        public  string FixUrl(string url)
        {
            var preUrl = GetPreUrl(url);//获取前缀
            var randomStrParam = GetUrlParam(url, "r");
            var decodeRandomStr =string.Empty;
            if (string.IsNullOrEmpty(randomStrParam))
            {
                decodeRandomStr = GetRandom();
            }
            else
            {
                decodeRandomStr = GetRandomDecode(randomStrParam);
            }
            var wirelessCode = InitWirelessCode(preUrl, decodeRandomStr);
            if (url.Contains("&wirelesscode"))
            {
                var oldWirelessCodeParam = GetUrlParam(url, "wirelesscode");
                url = url.Replace(oldWirelessCodeParam, wirelessCode);
            }
            else
            {
                url = url + "&wirelesscode=" + wirelessCode;
            }

            var randomEncodeStr = GetRandomEncode(decodeRandomStr);//重新加密
            if (!string.IsNullOrEmpty(randomStrParam))
            {
                url = url.Replace("&r=" + randomStrParam, "&r="+randomEncodeStr);
            }
            else
            {
                url = url + "&r=" + randomEncodeStr;
            }
            return url;
        }

        /// <summary>
        /// 根据url生成WireLessCode串
        /// </summary>
        /// <returns></returns>
        private  string InitWirelessCode(string preUrl,string randomStr)
        {
            if (string.IsNullOrEmpty(randomStr))
            {
                return string.Empty;
            }
            var fixUrl=preUrl+randomStr;
            return getMd5Hash(fixUrl);
        }
        /// <summary>
        /// 提取WirelessCode之前的字符串
        /// </summary>
        /// <returns></returns>
        private  string GetPreUrl(string url)
        {
            var endIndex = url.IndexOf("&wirelesscode");
            if (endIndex != -1)
            {
                return url.Substring(0, endIndex );
            }
            else {
                return url;
            }
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
        public  string DoubleUrlEncode(string temp, Encoding encoding)
        {
            return UrlEncode(UrlEncode(temp, encoding), encoding);
        }
        /// <summary>
        /// 转化为大写的urldecode
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public  string UrlEncode(string temp, Encoding encoding)
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

        /// <summary>
        /// 转化为大写的urldecode
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public  string UrlDecode(string temp, Encoding encoding)
        {

            return HttpUtility.UrlDecode(temp, encoding);
        }

        public  string getMd5Hash(string input)
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
            return sBuilder.ToString().ToLower();
        }
        public  string Encode(string source, string _DESKey)
        {

            StringBuilder sb = new StringBuilder();
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] key = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                //byte[] iv = ASCIIEncoding.ASCII.GetBytes(_DESKey);
                byte[] iv =new byte[8];
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
        public  string Decode(string source, string sKey)
        {
            byte[] inputByteArray = System.Convert.FromBase64String(source);//Encoding.UTF8.GetBytes(source);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                //des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV =new byte[8];
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

        

        private string GetLandNoticeFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/LandNotice?count={0}&page={1}&mode=LandNotice";
            }
        }

        private string GetCitySearchFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/MarketSearch?scity={0}&imei=133524428521975&psize={1}&ordertype=2&type=1&mode=json&pindex={2}&ordername=landstartdate&messagename=search";
            }
        }

        private string GetRegionSearchFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/MarketSearch?scity={1}&type=1&usage=&status=&mode=json&messagename=search&district={2}&provice={0}&imei=133524428521975&psize={3}&ordertype=2&pindex={4}&ordername=landstartdate";
            }
        }

        private string GetProvinceSearchFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/MarketSearch?provice={0}&imei=133524428521975&psize={1}&ordertype=2&type=1&mode=json&pindex={2}&ordername=landstartdate&messagename=search";
            }
        }

        private string GetLandByXYFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/MarketSearch?imei=133524428521975&psize={0}&ordertype=2&pindex=1&ordername=landstartdate&messagename=search&usage=1&radius=92234.86118824306&y={2}&x={1}";
            }
        }

        private string GetBuildingByXYFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/MapSearch?radius=3000&category={2}&status={3}&y={1}&x={0}";
            }
        }
    }
}  


