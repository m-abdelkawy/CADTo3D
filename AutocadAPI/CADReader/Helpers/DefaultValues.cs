using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
   public static class DefaultValues
    {
        public static double SlabThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.25 : 250;
        public static double RCFootingThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.5: 500;
        public static double PCFootingThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.20 : 200;
        public static double ColumnCover { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;
        public static double BarDiameter { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.018 : 18;
        public static double StirrupsSpacing { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.15 : 150;
        public static double StirrupsDiameter { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.012 : 12;
    }
}
