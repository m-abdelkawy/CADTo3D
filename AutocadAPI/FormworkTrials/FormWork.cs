using devDept.Eyeshot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormworkTrials
{
    class FormWork
    {
        public LinearPath OuterPathFormwork { get; set; }


        public FormWork(LinearPath linPath)
        {
            FormWorkOutline(linPath);
        }


        public void FormWorkOutline(LinearPath linPathElement)
        {
            this.OuterPathFormwork = (LinearPath)linPathElement.Offset(0.03);
        }
    }
}
