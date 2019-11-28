using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BIMWebViewer
{
    public enum SubmissionStages
    {
        FormWork,
        Concrete,
        Reinforcement
    }

    public enum ElementType
    {
        PCF,
        RCF,
        SEM,
        SHW,
        RTW,
        COL,
        SLB
    }
}