using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc4.Interfaces;

namespace BIMWebViewer.Classes
{
    public class ProductCategory
    {
        public string Name { get; set; }
        public List<ProductFamily> Families { get; set; } = new List<ProductFamily>();
    }

   
}