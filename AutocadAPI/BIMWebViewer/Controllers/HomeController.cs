using BIMWebViewer.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BIMWebViewer.Controllers
{
    public class HomeController : Controller
    {
         public ActionResult Home()
        {
            return View();
        }
       // [Authorize]
        public ActionResult Index()
        {
            var UsersFileLocation = "UploadedFiles";
            var UserDirectory = $"{Server.MapPath("~")}\\{UsersFileLocation}\\Projects\\";
            FileStruc.projectsDirs = Directory.GetDirectories(UserDirectory).ToList();
            ViewBag.CurrentPath = UserDirectory;
            ViewData["CurrentPath"] = UserDirectory;
            TempData["CurrentPath"] = UserDirectory;
            return View(FileStruc.projectsDirs);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        public ActionResult LoadProject(int projectIndex)
        { 
            FileStruc.versionsDirs = Directory.GetDirectories(FileStruc.projectsDirs[projectIndex]).ToList();
            ViewBag.ProjectName = Path.GetFileName(FileStruc.projectsDirs[projectIndex]);
            return View(FileStruc.versionsDirs);
        }
        
        public ActionResult Preview(int VersionIndex)
        { 
            var verPath = FileStruc.versionsDirs[VersionIndex];
            
            return RedirectToAction("PreviewModel","Viewer",new { versionPath=verPath });
        }
       // [HttpPost]
        public ActionResult loadVersions()
        { 
            return PartialView("Versions", FileStruc.versionsDirs);
        }
        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file, string projectName)
        {
            try
            {
                var projectPath = FileStruc.projectsDirs.Where(d => Path.GetFileName(d) == projectName).First();
                var CurrentDate = DateTime.Now; 
                var UserDirectory = $"{projectPath}\\Version {FileStruc.versionsDirs.Count + 1}";
                var fileExtention = Path.GetExtension(file.FileName);
                if (!(fileExtention == ".ifc" || fileExtention == ".IFC"))
                {
                    ViewBag.Message = "Please Upload IFC File!";
                }
                if (file.ContentLength > 0)
                {
                    string _FileName = Path.GetFileName(file.FileName);
                    string _path = Path.Combine(UserDirectory, _FileName);
                    Directory.CreateDirectory(UserDirectory);
                    file.SaveAs(_path);
                    FileStruc.versionsDirs = Directory.GetDirectories(projectPath).ToList();
                    IFCConverter.Products = null;
                    IFCConverter.Categories = null;
                }
                return View("LoadProject", FileStruc.versionsDirs);
                //return RedirectToAction("loadVersions","Home");
                //    var newPath = IFCConverter.ToWexBIM(_path);
                //    TempData["wexbimFilePath"] = newPath;
                //    TempData["IFCFilePath"] = _path;
                //    RedirectToAction("Viewer");


                //}
                //ViewBag.Message = "File Uploaded Successfully!!";
                //return RedirectToAction("Viewer");
            }
            catch (Exception ex)
            {

                throw new Exception(ex.ToString());
            }
        }


    }
}