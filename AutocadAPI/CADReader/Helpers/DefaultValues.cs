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
        public static double SlabThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? .25 : 250;
        public static double RCFootingThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? .5: 500;
        public static double PCFootingThinkess { get; set; } = CADConfig.Units == linearUnitsType.Meters ? .20 : 200;
    }
}
