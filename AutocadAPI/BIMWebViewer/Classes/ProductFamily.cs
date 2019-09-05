using System;
using System.Collections.Generic;
using System.Linq;
using System.Web; 

namespace BIMWebViewer.Classes
{
    public class ProductFamily
    {
        
        public string Name { get; set; }
        public List<ProductType> Types { get; set; } = new List<ProductType>();
    }
}