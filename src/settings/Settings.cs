using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PR
{
    class Settings
    {
        public static readonly string OrdresUrl = @"https://evil-legacy-service.herokuapp.com/api/v101/orders/";
        public static readonly string CategoriesUrl = @"https://evil-legacy-service.herokuapp.com/api/v101/categories/";
        public static readonly string CacheFolder =  Directory.GetCurrentDirectory() + @"\Cache\";
        public static readonly string CacheFile =  CacheFolder + @"\cache.json";
    }
}
