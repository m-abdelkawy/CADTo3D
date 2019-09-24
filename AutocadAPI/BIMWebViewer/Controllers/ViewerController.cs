using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using BIMWebViewer.Classes;
using System.Drawing;
using Newtonsoft.Json;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc4.Kernel;
using Xbim.ModelGeometry.Scene.Extensions;
using Newtonsoft.Json.Serialization;
using CADReader.BuildingElements;
using IfcFileCreator;

namespace BIMWebViewer.Controllers
{
    //[Authorize]
    public class ViewerController : Controller
    {

        // GET: Upload
        public ActionResult Index()
        {

            return View();
        }
        [HttpGet]
        public ActionResult UploadFile()
        {
            return View();
        }

        public ActionResult Viewer()
        {
            var filePath = "";
             
                filePath = TempData["wexbimFilePath"].ToString();
                //// file = TempData["wexbimFilePath"].ToString();
                ViewBag.FilePath = filePath;
                ViewBag.IFCFilePath = TempData["IFCFilePath"].ToString();
                ViewBag.VersionName = TempData["VersionName"].ToString();
                if (TempData["ViewPoints"] != null)
                    ViewBag.ViewPoints = TempData["ViewPoints"];
            
            List<ProductCategory> categories = IFCConverter.Categories;
            return View(categories);
        }
 

        public ActionResult ViewerLoad(string FileName)
        {
            return File(FileName, "application/octet-stream", FileName);
        }

        public ActionResult BrowserLoad(string FileName)
        {
            return File(FileName, "application/json", FileName);
        }

        [HttpPost]
        public ActionResult GetProperties(int productId)
        { 
            if (IFCConverter.propertySetsx4 != null)
            {
                if (IFCConverter.propertySetsx4.ContainsKey(productId))
                    return PartialView("ProductProperties", IFCConverter.propertySetsx4[productId]);
            }

            else
            {
                if (IFCConverter.propertySets2x3.ContainsKey(productId))
                    return PartialView("ProductProperties2x3", IFCConverter.propertySets2x3[productId]);
            }


            return new EmptyResult();
        }
        public ActionResult PreviewModel(string versionPath)
        {
            Building building = new Building("Building A");
            building.AddNewFloor(@"D:\03_PROJECT PREPERATION\04_Drawings\CAD Template\03.Ground Roof Slab.dwg", 3);
            building.AddNewFloor(@"D:\03_PROJECT PREPERATION\04_Drawings\CAD Template\02.Basement Roof SLab.dwg", 0);
            building.AddBuildingFoundation(@"D:\03_PROJECT PREPERATION\04_Drawings\CAD Template\01.Foundation.dwg", -4);

            XbimCreateBuilding newBuilding = new XbimCreateBuilding(building,versionPath);
           // devDept.Eyeshot.Translators.ReadAutodesk.OnApplicationExit(null, null);



            List<string> files = Directory.GetFiles(versionPath).ToList();
           // string wexFile = files.Where(a => Path.GetExtension(a) == ".wexBIM").FirstOrDefault();
            string IFCFile = files.Where(a => Path.GetExtension(a) == ".ifc").FirstOrDefault();

            var verName = Path.GetFileName(versionPath);
            var proName = Path.GetFileName(Path.GetDirectoryName(versionPath));
            FileStruc.CurrentVersion = versionPath;
            var newPath = IFCConverter.ToWexBIM(IFCFile);
            TempData["wexbimFilePath"] = newPath;
            TempData["IFCFilePath"] = IFCFile;
            TempData["VersionName"] = proName + "/" + verName;
            RedirectToAction("Viewer");

            return RedirectToAction("Viewer");
        }

        [HttpPost]
        public void UploadPic(string imageData)
        {
            string Pic_Path = HttpContext.Server.MapPath("MyPicture.png");
            using (FileStream fs = new FileStream(Pic_Path, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    byte[] data = Convert.FromBase64String(imageData);
                    bw.Write(data);
                    bw.Close();
                }
            }
        }

        [HttpPost]
        public ActionResult SaveViewList(string viewList)
        {
            var currentVersionPath = FileStruc.CurrentVersion;
            System.IO.File.WriteAllText(currentVersionPath + @"\viewList.json", viewList);
            return RedirectToAction("Viewer");
        }
        public ActionResult GetViewPoints()
        {
            List<string> files = Directory.GetFiles(FileStruc.CurrentVersion).ToList();
            string ViewListPath = files.Where(a => Path.GetExtension(a) == ".json").FirstOrDefault();
            string ViewListstring = "";
            if (ViewListPath != null)
            {
                ViewListstring = System.IO.File.ReadAllText(ViewListPath);
            }
            JsonResult result = new JsonResult();
            var jsonData = new { ViewPointsList = ViewListstring };
            var serializerSettings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            return new JsonResult { Data = JsonConvert.SerializeObject(jsonData, Formatting.Indented, serializerSettings), MaxJsonLength = Int32.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        public ActionResult GanttChart()
        {
            return View("_GanttChartPartial");
        }
        public ActionResult GanttChartTest()
        {
            return View();
        }
        [HttpPost]
        public ActionResult GetProductIds(int TypeId)
        {
            IIfcRelDefinesByType relType = IFCConverter.ModelTypes.Where(t => t.RelatingType.EntityLabel == TypeId).FirstOrDefault();
            List<int> productIds = new List<int>();
            if (relType != null)
                  productIds = relType.RelatedObjects.Select(a => a.EntityLabel).ToList();
            JsonResult result = new JsonResult();
            var jsonData = new { ProductIdList = productIds };
            var serializerSettings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            return new JsonResult { Data = JsonConvert.SerializeObject(jsonData, Formatting.Indented, serializerSettings), MaxJsonLength = Int32.MaxValue, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }
        [HttpPost]
        public string GetProductId(int selectedLabel)
        {

            IIfcProduct Product = IFCConverter.Products.Where(p => p.EntityLabel == selectedLabel).FirstOrDefault();
            if (Product != null)
            {
                string ProductId = Product.Name.Value.Value.ToString().Split(':')[2];
                return ProductId;
            }
            else
            {
                IIfcRelDefinesByType relType = IFCConverter.ModelTypes.Where(t => t.RelatingType.EntityLabel == selectedLabel).FirstOrDefault();
                string typeName = relType.RelatingType.Name.ToString();
                return typeName;
            }
              
        }
    }
}