using CADReader.Base;
using CADReader.Helpers;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Column
    {
        public LinearPath ColPath { get; set; }
        public double Cover { get; set; } = DefaultValues.ColumnCover;
        
        public Column(LinearPath _colLinPath)
        {
            ColPath = _colLinPath;
        }
        
 
    }
}
