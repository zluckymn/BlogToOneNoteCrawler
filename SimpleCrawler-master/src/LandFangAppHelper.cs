namespace SimpleCrawler
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web;

    public class LandFangAppHelper
    {
        private const string DESKEY = "soufunss";
        private const string MESSAGENAME = "CheckMobile";
        private const string IMEI = "133524428521975";
        private const string REGMODE = "reg";
        private const string ISENCRYPT = "20150303";
        private string GetLandDetailFormatUrl = "https://appapi.3g.fang.com/LandApp/MarketDetail?messagename=LandInfoList&sParcelId={0}&isvip={2}&iUserID={1}&imei=133524428521975";

        public string Decode(string source, string sKey)
        {
            byte[] inputByteArray = Convert.FromBase64String(source);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = Encoding.ASCII.GetBytes(sKey);
                des.IV = new byte[8];
                MemoryStream ms = new MemoryStream();
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

        public string DoubleUrlEncode(string temp, Encoding encoding)
        {
            return this.UrlEncode(this.UrlEncode(temp, encoding), encoding);
        }

        public string Encode(string source, string _DESKey)
        {
            StringBuilder sb = new StringBuilder();
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] key = Encoding.ASCII.GetBytes(_DESKey);
                byte[] iv = new byte[8];
                byte[] dataByteArray = Encoding.UTF8.GetBytes(source);
                des.Mode = CipherMode.CBC;
                des.Key = key;
                des.IV = iv;
                string encrypt = "";
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(dataByteArray, 0, dataByteArray.Length);
                        cs.FlushFinalBlock();
                        encrypt = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return encrypt;
            }
        }

        public string FixIUserIdUrl(string url, string value)
        {
            return this.FixUrl(url, "iUserID", value);
        }

        public string FixUrl(string url)
        {
            string preUrl = this.GetPreUrl(url);
            string randomStrParam = this.GetUrlParam(url, "r");
            string decodeRandomStr = string.Empty;
            if (string.IsNullOrEmpty(randomStrParam))
            {
                decodeRandomStr = this.GetRandom();
            }
            else
            {
                decodeRandomStr = this.GetRandomDecode(randomStrParam);
            }
            string wirelessCode = this.InitWirelessCode(preUrl, decodeRandomStr);
            if (url.Contains("&wirelesscode"))
            {
                string oldWirelessCodeParam = this.GetUrlParam(url, "wirelesscode");
                url = url.Replace(oldWirelessCodeParam, wirelessCode);
            }
            else
            {
                url = url + "&wirelesscode=" + wirelessCode;
            }
            string randomEncodeStr = this.GetRandomEncode(decodeRandomStr);
            if (!string.IsNullOrEmpty(randomStrParam))
            {
                url = url.Replace("&r=" + randomStrParam, "&r=" + randomEncodeStr);
                return url;
            }
            url = url + "&r=" + randomEncodeStr;
            return url;
        }

        public string FixUrl(string url, string paramName, string value)
        {
            string oldParamValue = this.GetUrlParam(url, paramName);
            if (!string.IsNullOrEmpty(paramName))
            {
                url = url.Replace(string.Format("&{0}={1}", paramName, oldParamValue), string.Format("&{0}={1}", paramName, value));
            }
            return this.FixUrl(url);
        }

        public string getMd5Hash(string input)
        {
            int num;
            byte[] data = new MD5CryptoServiceProvider().ComputeHash(Encoding.Default.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i = num + 1)
            {
                sBuilder.Append(data[i].ToString("X2"));
                num = i;
            }
            return sBuilder.ToString().ToLower();
        }

        public string GetPhoneDecode(string mobile)
        {
            return this.Decode(this.UrlDecode(this.UrlDecode(mobile, Encoding.UTF8), Encoding.UTF8), "soufunss");
        }

        public string GetPhoneEncode(string mobile)
        {
            string encodeStr = this.Encode(mobile, "soufunss");
            return this.DoubleUrlEncode(encodeStr, Encoding.UTF8);
        }

        private string GetPreUrl(string url)
        {
            int endIndex = url.IndexOf("&wirelesscode");
            if (endIndex != -1)
            {
                return url.Substring(0, endIndex);
            }
            return url;
        }

        private string GetQueryString(string url)
        {
            int queryStrIndex = url.IndexOf("?");
            if (queryStrIndex != -1)
            {
                return url.Substring(queryStrIndex + 1, (url.Length - queryStrIndex) - 1);
            }
            return string.Empty;
        }

        public string GetRandom()
        {
            int num;
            StringBuilder sb = new StringBuilder();
            string[] str = new string[] {
                "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p",
                "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "0", "1", "2", "3", "4", "5",
                "6", "7", "8", "9"
            };
            Random random = new Random(0);
            for (int i = 0; i < 5; i = num + 1)
            {
                int hitR = random.Next(0, 0x23);
                string hitS = str[hitR];
                sb.Append(hitS);
                num = i;
            }
            return sb.ToString();
        }

        public string GetRandomDecode(string randomStr)
        {
            return this.Decode(this.UrlDecode(randomStr, Encoding.UTF8), "soufunss");
        }

        public string GetRandomEncode(string randomStr)
        {
            string encodeStr = this.Encode(randomStr, "soufunss");
            return this.UrlEncode(encodeStr, Encoding.UTF8);
        }

        private string GetUrlParam(string url, string name)
        {
            NameValueCollection dic = HttpUtility.ParseQueryString(this.GetQueryString(url));
            return ((dic[name] != null) ? dic[name].ToString() : string.Empty);
        }

        public string InitCheckCodeUrl(string code, string mobile)
        {
            string curUrl = string.Format(this.GetCheckCodeFormatUrl, code, mobile);
            return this.FixUrl(curUrl);
        }

        public string InitCityFormatUrl(string cityName, string count, string page)
        {
            string curUrl = string.Format(this.GetCitySearchFormatUrl, cityName, count, page);
            return this.FixUrl(curUrl);
        }

        public string InitLandDetailUrl(string sParcelId, string iUserId, string isVip = "false")
        {
            string curUrl = string.Format(this.GetLandDetailFormatUrl, sParcelId, iUserId, isVip);
            return this.FixUrl(curUrl);
        }

        public string InitLandNoticeUrl(string count, string page)
        {
            string curUrl = string.Format(this.GetLandNoticeFormatUrl, count, page);
            return this.FixUrl(curUrl);
        }

        public string InitProvinceFormatUrl(string provinceName, string count, string page)
        {
            string curUrl = string.Format(this.GetProvinceSearchFormatUrl, provinceName, count, page);
            return this.FixUrl(curUrl);
        }

        public string InitPushLandUrl(string count, string page)
        {
            string curUrl = string.Format(this.GetLandPushFormatUrl, count, page);
            return this.FixUrl(curUrl);
        }

        public string InitRegionFormatUrl(string provinceName, string cityName, string regionName, string count, string page)
        {
            string curUrl = string.Format(this.GetRegionSearchFormatUrl, new object[] { provinceName, cityName, regionName, count, page });
            return this.FixUrl(curUrl);
        }

        public string InitRegSendSmsUrl(string mobile)
        {
            string curUrl = string.Format(this.GetRegSEndSMSFormatUrl, mobile);
            return this.FixUrl(curUrl);
        }

        private string InitWirelessCode(string preUrl, string randomStr)
        {
            if (string.IsNullOrEmpty(randomStr))
            {
                return string.Empty;
            }
            string fixUrl = preUrl + randomStr;
            return this.getMd5Hash(fixUrl);
        }

        public string UrlDecode(string temp, Encoding encoding)
        {
            return HttpUtility.UrlDecode(temp, encoding);
        }

        public string UrlEncode(string temp, Encoding encoding)
        {
            int num;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < temp.Length; i = num + 1)
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
                num = i;
            }
            return stringBuilder.ToString();
        }

        private string GetRegSEndSMSFormatUrl
        {
            get
            {
                return (string.Format("https://appapi.3g.fang.com/LandApp/SendSMS?isencrypt={0}&messagename={1}&mode={2}&imei={3}&newpassword", new object[] { "20150303", "CheckMobile", "reg", "133524428521975" }) + "&mobile={0}");
            }
        }

        private string GetCheckCodeFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/checkcode?isencrypt=20150303&code={0}&mobile={1}&mode=reg&imei=133524428521975";
            }
        }

        private string GetLandPushFormatUrl
        {
            get
            {
                return "https://appapi.3g.fang.com/LandApp/LandNotice?count={0}&page={1}&mode=PushLand";
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

