using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.Helpers
{
    public class CADConfig
    {
        private ReadAutodesk cadReader;

        public ReadAutodesk CadReader
        {
            get { return cadReader; }
            set { cadReader = value; cadReader.DoWork(); }
        }


        static public linearUnitsType Units { get; set; }

    }
}
