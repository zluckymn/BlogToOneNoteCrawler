using SimpleCrawler.Demo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace SimpleCrawler.Demo
{
    /// <SUMMARY></SUMMARY> 
    /// 取得WebBrowser的完整Cookie。 
    /// 因为默认的webBrowser1.Document.Cookie取不到HttpOnly的Cookie 
    /// IE7不兼容，IE8可以，其它未知 
    /// 
    public class FullWebBrowserCookie
    {
        public static Dictionary<string, string> GetCookieList(Uri uri, bool throwIfNoCookie)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string cookie = GetCookieInternal(uri, throwIfNoCookie);
            Console.WriteLine("FullWebBrowserCookie - 所有cookie:" + cookie);
            string[] arrCookie = cookie.Split(';');
            foreach (var item in arrCookie)
            {
                string[] arr = item.Split('=');
                string key = arr[0].Trim();
                string val = "";
                if (arr.Length >= 2)
                {
                    val = arr[1].Trim();
                }

                if (!dict.ContainsKey(key))
                {
                    dict.Add(key, val);
                }
            }
            Console.WriteLine("FullWebBrowserCookie - cookie已载入dict，共" + dict.Count.ToString() + "项");

            return dict;
        }

        public static string GetCookieValue(string key, Uri uri, bool throwIfNoCookie)
        {
            Console.WriteLine("GetCookieValue");
            Dictionary<string, string> dict = GetCookieList(uri, throwIfNoCookie);

            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return "";
        }

        [SecurityCritical]
        public static string GetCookieInternal(Uri uri, bool throwIfNoCookie)
        {
            //Console.WriteLine("GetCookieInternal");

            uint pchCookieData = 0;
            string url = UriToString(uri);
            uint flag = (uint)INativeMethods.InternetFlags.INTERNET_COOKIE_HTTPONLY;

            //Gets the size of the string builder      
            if (INativeMethods.InternetGetCookieEx(url, null, null, ref pchCookieData, flag, IntPtr.Zero))
            {
                pchCookieData++;
                StringBuilder cookieData = new StringBuilder((int)pchCookieData);

                //Read the cookie      
                if (INativeMethods.InternetGetCookieEx(url, null, cookieData, ref pchCookieData, flag, IntPtr.Zero))
                {
                    DemandWebPermission(uri);
                    return cookieData.ToString();
                }
            }

            int lastErrorCode = Marshal.GetLastWin32Error();

            if (throwIfNoCookie || (lastErrorCode != (int)INativeMethods.ErrorFlags.ERROR_NO_MORE_ITEMS))
            {
                throw new Win32Exception(lastErrorCode);
            }

            return null;
        }

        private static void DemandWebPermission(Uri uri)
        {
            string uriString = UriToString(uri);

            if (uri.IsFile)
            {
                string localPath = uri.LocalPath;
                new FileIOPermission(FileIOPermissionAccess.Read, localPath).Demand();
            }
            else
            {
                new WebPermission(NetworkAccess.Connect, uriString).Demand();
            }
        }

        private static string UriToString(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            UriComponents components = (uri.IsAbsoluteUri ? UriComponents.AbsoluteUri : UriComponents.SerializationInfoString);
            return new StringBuilder(uri.GetComponents(components, UriFormat.SafeUnescaped), 2083).ToString();
        }
    }
}