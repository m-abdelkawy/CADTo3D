using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc4.Kernel;

namespace IfcFileCreator
{
    public class Submissions
    {
        public List<List<IfcProduct>> SubmittedElems { get; set; }


        public Submissions()
        {
            SubmittedElems = new List<List<IfcProduct>>();
        }
    }
}
