using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
    public static class CadHelper
    {
      //  public static ReadAutodesk CADReader { get; set; }
        public static List<LinearPath> PLinesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is LinearPath).Cast<LinearPath>().ToList();
        }

        public static List<Line> LinesGetByLayerName(ReadAutodesk cadReader, string layerName)
        {
            return cadReader.Entities.Where(e => e.LayerName == layerName && e is Line).Cast<Line>().ToList();
        }
    }
}
