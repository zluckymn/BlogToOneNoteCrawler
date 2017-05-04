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
    public class WenShuAppHelper
    {
        private const string TOKENKEY= "lawyeecourtwenshuapp";
        private const string CONTENKEY = "lawyeecourtwensh";
        private const string CONTENVI = "lawyeecourtwensh";

        /// <summary>
        /// 获取最新的请求token
        /// </summary>
        /// <returns></returns>
        public static string GetRequestToken()
        {
            var curDate = DateTime.Now.ToString("yyyyMMddHHmm");
            var md5Str = curDate + TOKENKEY;
            var str = getMd5Hash(md5Str);
            return str;
        }

        /// <summary>
        /// 返回解密信息
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string GetWenShuDecode(string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                var key = CONTENKEY;
                var vi = CONTENVI;
                var result = AESDecode(str, key, vi);
                return result;
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 获取Md5Hash
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
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
       


        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="text"></param>
        /// <param name="password"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string AESDecode(string text, string password, string iv)
        {
            RijndaelManaged rijndaelCipher = new RijndaelManaged();

            rijndaelCipher.Mode = CipherMode.CBC;

            rijndaelCipher.Padding = PaddingMode.PKCS7;

            rijndaelCipher.KeySize = 128;

            rijndaelCipher.BlockSize = 128;

            byte[] encryptedData = Convert.FromBase64String(text);

            byte[] pwdBytes = System.Text.Encoding.UTF8.GetBytes(password);

            byte[] keyBytes = new byte[16];

            int len = pwdBytes.Length;

            if (len > keyBytes.Length) len = keyBytes.Length;

            System.Array.Copy(pwdBytes, keyBytes, len);

            rijndaelCipher.Key = keyBytes;

            byte[] ivBytes = System.Text.Encoding.UTF8.GetBytes(iv);
            rijndaelCipher.IV = ivBytes;

            ICryptoTransform transform = rijndaelCipher.CreateDecryptor();

            byte[] plainText = transform.TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            return Encoding.UTF8.GetString(plainText);

        }
    }
}  


