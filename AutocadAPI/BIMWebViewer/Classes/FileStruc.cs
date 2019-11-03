using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIMWebViewer.Classes
{
    public static class FileStruc
    {
        public static string CurrentVersion { get; set; } 
        public static string CurrentProject { get; set; }
        public static List<string> projectsDirs { get; set; } = new List<string>();
        public static  List<string> versionsDirs { get; set; } = new List<string>();
   
    }
}