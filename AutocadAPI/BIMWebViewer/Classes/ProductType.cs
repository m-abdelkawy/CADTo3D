using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace BIMWebViewer.Classes
{
    public class ProductType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<IIfcProduct> Products { get; set; } = new List<IIfcProduct>();
    }
    class ProductCategoryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IIfcProduct));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(IfcProduct));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(IfcProduct));
        }

    }
}