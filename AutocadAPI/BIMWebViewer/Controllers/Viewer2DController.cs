using BIMWebViewer.Classes;
using CADReader;
using CADReader.BuildingElements;
using CADReader.Helpers;
using CADReader.Reinforced_Elements;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using IfcFileCreator;
using IfcFileCreator.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.ModelGeometry.Scene;

namespace BIMWebViewer.Controllers
{
    public class Viewer2DController : Controller
    {
        // GET: Viewer2D
        static List<int> lstProductId;
        public static string ElementTypeSubmitted { get; set; }

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Viewer2D()
        {
            var filePath = "";

            filePath = TempData["wexbimFilePath"].ToString();
            //// file = TempData["wexbimFilePath"].ToString();
            ViewBag.FilePath = filePath;
            ViewBag.VersionName = TempData["VersionName"].ToString();

            return View();
        }
        public ActionResult Viewer2DLoad(string FileName)
        {
            return File(FileName, "application/octet-stream", FileName);
        }
        public ActionResult PreviewModel(string versionPath)
        {
            List<string> files = Directory.GetFiles(versionPath).ToList();
            // string wexFile = files.Where(a => Path.GetExtension(a) == ".wexBIM").FirstOrDefault();
            List<string> lstIfcFile = files.Where(a => Path.GetExtension(a) == ".ifc").ToList();
            //string wexFile = files.Where(a => Path.GetExtension(a) == ".wexBIM").FirstOrDefault();
            string wexFile = files.Where(a => Path.GetExtension(a) == ".wexBIM").FirstOrDefault();
            var verName = Path.GetFileName(versionPath);
            var proName = Path.GetFileName(Path.GetDirectoryName(versionPath));
            FileStruc.CurrentVersion = versionPath;

            TempData["VersionName"] = proName + "/" + verName;

            if (wexFile != null)
            {
                TempData["wexbimFilePath"] = wexFile;
            }
            else
            {
                string newPath = null;
                for (int i = 0; i < lstIfcFile.Count; i++)
                {
                    string ifcFile = files.Where(a => Path.GetExtension(a) == ".ifc").ToList()[i];
                    newPath = IFCConverter.ToWexBIM(ifcFile);
                }

                TempData["wexbimFilePath"] = newPath;
                TempData["IFCFilePath"] = lstIfcFile[0];

            }

            //RedirectToAction("Viewer");

            return RedirectToAction("Viewer2D");
        }
        public ActionResult GetElementsByType(string type)
        {
            ElementTypeSubmitted = type;
            List<string> files = Directory.GetFiles(FileStruc.CurrentVersion).ToList();

            string ifcFile = files.Where(a => Path.GetExtension(a) == ".ifc").FirstOrDefault();

            using (var model = IfcStore.Open(ifcFile))
            {
                lstProductId = model.Instances.OfType<IIfcElement>().Where(t => t.Tag == type).Select(e => e.EntityLabel).ToList();
            }

            JsonResult jsonRes = new JsonResult();

            var jsonData = new { ProductList = lstProductId, floorHeight = 4.95, floorLevel = 378.5 };

            var serializerSettings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };

            jsonRes.Data = JsonConvert.SerializeObject(jsonData, Formatting.Indented, serializerSettings);
            jsonRes.MaxJsonLength = Int32.MaxValue;
            jsonRes.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            return jsonRes;
        }
        [HttpPost]
        public ActionResult SubmitBlahBlah(List<int> axesIds, SubmissionStages subItem)
        {
            List<string> files = Directory.GetFiles(FileStruc.CurrentVersion).ToList();
            List<IIfcProduct> Axes;

            List<IIfcProduct> lstProductSubmission;
            List<Line> lstAxesLines = new List<Line>();
            string ifcFile = files.Where(a => Path.GetExtension(a) == ".ifc").FirstOrDefault();

            using (var model = IfcStore.Open(ifcFile))
            {
                Axes = model.Instances.OfType<IIfcProduct>().Where(b => axesIds.Contains(b.EntityLabel)).ToList();
                List<IIfcProduct> lstProduct = model.Instances.OfType<IIfcProduct>().Where(p => lstProductId.Contains(p.EntityLabel)).ToList();

                List<Line> lstLines = IFCHelper.AxesLinesGet(Axes);


                //Axes Boundaries
                LinearPath linPathSubmittal = MathHelper.LinPathAxesIntersection(lstLines);

                Dictionary<int, LinearPath> dicElement = IFCHelper.DicLinPathOfProductsGet(lstProduct);

                //get products within the axes boundary
                Dictionary<int, LinearPath> elementsWithinAxesBoundary = CadHelper.SubmittedElementsGet(linPathSubmittal, dicElement);

                //reinforcement IFC file
                using (IfcStore subModelRFT = IFCHelper.CreateandInitModel("Reinforcement File", model.Instances.OfType<IfcProject>().FirstOrDefault().UnitsInContext))
                {
                    IfcBuilding bldng = IFCHelper.CreateBuilding(subModelRFT, "bldngRFT", new Point3D(0, 0, 0));
                    using (var txn = subModelRFT.BeginTransaction("I"))
                    {
                        IfcBuildingStorey storey = subModelRFT.Instances.New<IfcBuildingStorey>();
                        bldng.AddToSpatialDecomposition(storey);
                        switch (subItem)
                        {
                            case SubmissionStages.FormWork:
                                for (int i = 0; i < elementsWithinAxesBoundary.Values.ToList().Count; i++)
                                {
                                    IIfcProduct product = lstProduct.FirstOrDefault(p => p.EntityLabel == elementsWithinAxesBoundary.Keys.ToList()[i]);
                                    IIfcRepresentationItem repItem = product.Representation.Representations.First.Items.First;
                                    double height = (repItem as IIfcExtrudedAreaSolid).Depth;

                                    IfcOpeningElement open;
                                    XbimCreateBuilding.CreateFormWork(subModelRFT, elementsWithinAxesBoundary.Values.ToList()[i], DefaultValues.FormWorkThickness,
                                        height, out open, "", false, false, false);

                                }
                                //switch (elemTypeFormwork)
                                //{
                                //    case ElementType.PCF:
                                //        break;
                                //    case ElementType.RCF:
                                //        break;
                                //    case ElementType.SEM:
                                //        break;
                                //    case ElementType.SHW:
                                //        break;
                                //    case ElementType.RTW:
                                //        break;
                                //    case ElementType.COL:
                                //        for (int i = 0; i < elementsWithinAxesBoundary.Values.ToList().Count; i++)
                                //        {
                                //            Column col = new Column(elementsWithinAxesBoundary.Values.ToList()[i]);
                                //            ReinforcedCadColumn rftCol = new ReinforcedCadColumn(col, 0);

                                //            IIfcProduct product = lstProduct.FirstOrDefault(p => p.EntityLabel == elementsWithinAxesBoundary.Keys.ToList()[i]);
                                //            IIfcRepresentationItem repItem = product.Representation.Representations.First.Items.First;
                                //            double height = (repItem as IIfcExtrudedAreaSolid).Depth;

                                //            IfcOpeningElement open;
                                //            XbimCreateBuilding.CreateFormWork(subModelRFT, rftCol.CadColumn.ColPath, DefaultValues.FormWorkThickness,
                                //                height, out open, false, false, false);

                                //        }
                                //        break;
                                //    case ElementType.SLB:
                                //        break;
                                //    default:
                                //        break;
                                //}

                                break;
                            case SubmissionStages.Concrete:
                                lstProductSubmission = lstProduct.Where(p => elementsWithinAxesBoundary.ContainsKey(p.EntityLabel)).ToList();
                                var map = new XbimInstanceHandleMap(model, subModelRFT);
                                for (int i = 0; i < lstProductSubmission.Count; i++)
                                {
                                    IIfcProduct product = subModelRFT.InsertCopy(lstProductSubmission[i], map, null, false, false);
                                    storey.AddElement(product as IfcProduct);
                                }
                                break;
                            case SubmissionStages.Reinforcement:
                                Enum.TryParse(ElementTypeSubmitted, out ElementType elemType);

                                switch (elemType)
                                {
                                    case ElementType.PCF:
                                        break;
                                    case ElementType.RCF:
                                        break;
                                    case ElementType.SEM:
                                        break;
                                    case ElementType.SHW:
                                        break;
                                    case ElementType.RTW:
                                        break;
                                    case ElementType.COL:
                                        for (int i = 0; i < elementsWithinAxesBoundary.Values.ToList().Count; i++)
                                        {
                                            Column col = new Column(elementsWithinAxesBoundary.Values.ToList()[i]);
                                            ReinforcedCadColumn rftCol = new ReinforcedCadColumn(col, 0);

                                            IIfcProduct product = lstProduct.FirstOrDefault(p => p.EntityLabel == elementsWithinAxesBoundary.Keys.ToList()[i]);
                                            IIfcRepresentationItem repItem = product.Representation.Representations.First.Items.First;
                                            double height = (repItem as IIfcExtrudedAreaSolid).Depth;

                                            XbimCreateBuilding.CreateColumnRft(rftCol, storey, subModelRFT, height, "");

                                        }
                                        break;
                                    case ElementType.SLB:
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }

                        txn.Commit();
                        subModelRFT.SaveAs(@"E:\01. Work\demo.ifc");
                        var context = new Xbim3DModelContext(subModelRFT);
                        context.CreateContext();

                        //var wexBimFilename = Path.ChangeExtension(, "wexBIM");
                        using (var wexBiMfile = System.IO.File.Create((@"E:\01. Work\demo.wexBIM")))
                        {
                            using (var wexBimBinaryWriter = new BinaryWriter(wexBiMfile))
                            {
                                subModelRFT.SaveAsWexBim(wexBimBinaryWriter);
                                wexBimBinaryWriter.Close();
                            }
                            wexBiMfile.Close();
                        }
                    }
                }

            }

            return new EmptyResult();
        }

    }
}