using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;


namespace BIMWebViewer.ViewModels
{
    public class IFCFileUploader
    {
        [Required]
        public HttpPostedFileBase ifcFile { get; set; }
    }
}