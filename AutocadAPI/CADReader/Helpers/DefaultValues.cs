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
        public static double WallCover { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;
        public static double FootingCover { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;
        public static double SemelleCover { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;
        public static double ShearWallCover { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;
        public static double BarDiameter { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.018 : 18;
        public static double StirrupsSpacing { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.25 : 250;
        public static double StirrupsDiameter { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.012 : 12;
        public static double LongBarSpacing { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.70 : 700;
        public static double FormWorkThickness { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.025 : 25;
        public static double SmellesWithFootingThickness { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.5 : 500;
        public static double SmellesWithColumnThickness { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.7 : 700;
        public static double ConduitDiameter { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 0.03 : 30;

        public static double ColDowelLength { get; set; } = CADConfig.Units == linearUnitsType.Meters ? 1.5 : 1500;
    }
}
