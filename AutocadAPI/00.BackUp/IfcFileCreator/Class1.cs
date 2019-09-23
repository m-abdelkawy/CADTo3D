using IfcFileCreator.BuildingElements;
using IfcFileCreator.Constants;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcFileCreator
{
    public class Class1
    {
        private List<List<Point3d>> lstMidPoints = new List<List<Point3d>>();
        List<double> lstThickness = new List<double>();
        List<Wall> lstWalls = new List<Wall>();
        List<RectColumn> lstColumns = new List<RectColumn>();
        List<RectFooting> lstFooting = new List<RectFooting>();
        List<Slab> lstSlab = new List<Slab>();
        List<Opening> lstOpening = new List<Opening>();

        [CommandMethod("AttachXref")]
        public void AttachXref()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            string fileName = @"D:\Coding Trials\GIT\AutocadAPI\AutocadAPI\AutocadAPI\Files\hamada.dwg";
            string strBlkName = System.IO.Path.GetFileNameWithoutExtension(fileName);

            ObjectId objId = acCurDb.AttachXref(fileName, strBlkName);
        }

        [CommandMethod("ListEntities")]
        public void ListEntities()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction trans = acCurDb.TransactionManager.StartTransaction())
            {
                BlockTable blkTbl = trans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                string msg = "\nModel Space Obects: ";
                int count = 0;
                foreach (ObjectId objId in blkTblRec)
                {
                    msg += "\n" + objId.ObjectClass.DxfName;
                    count += 1;
                }

                if (count == 0)
                {
                    msg = "no objects in the model space: ";
                }

                acDoc.Editor.WriteMessage(msg);
            }
        }

        [CommandMethod("AddLayer")]
        public void AddLayer()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!(lyrTbl.Has("hamadaLayer")))
                {
                    trans.GetObject(db.LayerTableId, OpenMode.ForWrite);

                    using (LayerTableRecord lyrTblRec = new LayerTableRecord())
                    {
                        lyrTblRec.Name = "HamadaLayer";
                        lyrTblRec.Color = Color.FromColor(System.Drawing.Color.Cyan);

                        lyrTbl.Add(lyrTblRec);

                        trans.AddNewlyCreatedDBObject(lyrTblRec, true);

                        LayerTableRecord lyrZeroRec = trans.GetObject(lyrTbl["0"], OpenMode.ForWrite) as LayerTableRecord;
                        lyrZeroRec.Color = Color.FromColor(System.Drawing.Color.Red);
                    }


                    trans.Commit();
                }
            }
        }

        [CommandMethod("trialwallPts")]
        public void TrialWallPts()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            List<Line> lstWallLines = new List<Line>();

            ObjectId lyrWallId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has("Wall"))
                {
                    return;
                }
                lyrWallId = lyrTbl["Wall"];
                //LayerTableRecord lyrTblRecWall = trans.GetObject(lyrWallId, OpenMode.ForRead) as LayerTableRecord;



                Editor ed = doc.Editor;
                PromptSelectionResult prSelRes = ed.GetSelection();
                if (prSelRes.Status == PromptStatus.OK)
                {
                    SelectionSet selSet = prSelRes.Value;
                    IEnumerator itr = selSet.GetEnumerator();

                    while (itr.MoveNext())
                    {
                        SelectedObject lineObj = itr.Current as SelectedObject;

                        if (lineObj != null)
                        {
                            Entity lineEnt = trans.GetObject(lineObj.ObjectId, OpenMode.ForRead) as Entity;
                            Line line = lineEnt as Line;

                        }
                    }
                }


            }
        }





        [CommandMethod("DisplayPolyArea")]
        public void DisplayPolyArea()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;


            PromptIntegerResult prIntRes;
            PromptIntegerOptions prIntOpts = new PromptIntegerOptions("");
            prIntOpts.Message = "Enter number of Points";
            prIntRes = doc.Editor.GetInteger(prIntOpts);

            PromptPointResult prPtRes;
            PromptPointOptions prPtOpts = new PromptPointOptions("");
            prPtOpts.Message = "\nEnter number of points";

            Point2dCollection colPt = new Point2dCollection();

            for (int i = 0; i < prIntRes.Value; i++)
            {
                prPtOpts.Message = $"\nEnter Point. [{i + 1}]: ";
                prPtRes = doc.Editor.GetPoint(prPtOpts);

                colPt.Add(new Point2d(prPtRes.Value.X, prPtRes.Value.Y));

                if (prPtRes.Status == PromptStatus.Cancel) return;
            }

            using (Polyline acPoly = new Polyline())
            {
                for (int i = 0; i < colPt.Count; i++)
                {
                    acPoly.AddVertexAt(i, colPt[i], 0, 0, 0);
                }

                acPoly.Closed = true;

                Application.ShowAlertDialog($"Area: {acPoly.Area.ToString()}");
            }
        }


        [CommandMethod("GetBlocks")]
        public void GetBlocks()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                //BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                BlockTableRecord blkTblRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblRec)
                {
                    Entity ent = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    BlockReference br = ent as BlockReference;
                    if(br!= null)
                    {
                        //br.IntersectWith()
                        //string attrs = "";
                        //foreach (DynamicBlockReferenceProperty attr in br.BlockTable)
                        //{
                        //    attrs += "\n" + attr.PropertyName + ": " + attr.Value;
                        //}
                        //Application.ShowAlertDialog(attrs);
                    }
                }

                //foreach (ObjectId objId in blkTbl)
                //{
                //    BlockTableRecord blkTblRec = trans.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;

                    
                //    if (blkTblRec != null && blkTblRec.Name == BlockName.Door)
                //    {
                //        ObjectIdCollection col = blkTblRec.GetBlockReferenceIds(true, true);
                //        Application.ShowAlertDialog("Name: " + blkTblRec.Name + "- Type: " + blkTblRec.GetType());
                //    }
                //    //BlockReference doorBr = trans.GetObject(objId, OpenMode.ForRead) as BlockReference;
                //    //if(doorBr!= null)
                //    //{
                //    //    Application.ShowAlertDialog(doorBr.Name + ": " + doorBr.BlockName);
                //    //}

                //    //BlockTableRecord blkTblRec = trans.GetObject(objId, OpenMode.ForRead) as BlockTableRecord;
                //    //if (blkTblRec != null && blkTblRec.Name.Contains(BlockName.Door))
                //    //{
                //    //    BlockReference doorBr = trans.GetObject(blkTblRec.ObjectId, OpenMode.ForRead) as BlockReference;
                //    //}
                //}
            }
        }


        [CommandMethod("WallGetEndPoints")]
        public void WallGetEndPoints()
        {
            this.lstWalls = new List<Wall>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            List<List<Point3d>> lstMidPoints = new List<List<Point3d>>();
            List<double> lstThickness = new List<double>();


            List<Line> lstWallLines = new List<Line>();

            ObjectId lyrWallId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has("Wall"))
                {
                    return;
                }
                lyrWallId = lyrTbl["Wall"];


                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblModelSpaceRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblModelSpaceRec)
                {
                    Entity lineEnt = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    Line line = lineEnt as Line;
                    if (line == null)
                    {
                        continue;
                    }
                    if (line.LayerId == lyrWallId)
                    {
                        lstWallLines.Add(line);
                    }
                }

                List<List<Line>> lstWalls = new List<List<Line>>();


                for (int i = 0; i < lstWallLines.Count; i++)
                {
                    Line parallel = lstWallLines[i].LineGetNearestParallel(lstWallLines.ToArray());
                    if (parallel != null && (lstWallLines[i].Length > parallel.Length))
                    {
                        lstWalls.Add(new List<Line> { lstWallLines[i], parallel });
                        lstThickness.Add(MathHelper.DistanceBetweenTwoParallels(lstWallLines[i], parallel));
                    }
                }


                foreach (List<Line> lstParallels in lstWalls)
                {
                    Point3d stPt1 = lstParallels[0].StartPoint;
                    Point3d stPt2 = lstParallels[1].StartPoint;

                    Point3d endPt1 = lstParallels[0].EndPoint;
                    Point3d endPt2 = lstParallels[1].EndPoint;

                    if (stPt1.DistanceTo(stPt2) < stPt1.DistanceTo(endPt2))
                    {
                        lstMidPoints.Add(new List<Point3d> { MathHelper.MidPoint(stPt1, stPt2), MathHelper.MidPoint(endPt1, endPt2) });
                    }
                    else
                    {
                        lstMidPoints.Add(new List<Point3d> { MathHelper.MidPoint(stPt1, endPt2), MathHelper.MidPoint(stPt2, endPt1) });
                    }
                }
                trans.Commit();
            }
            this.lstMidPoints = lstMidPoints;
            this.lstThickness = lstThickness;

            for (int i = 0; i < lstMidPoints.Count; i++)
            {
                this.lstWalls.Add(new Wall(lstThickness[i], lstMidPoints[i][0], lstMidPoints[i][1]));
            }
        }


        [CommandMethod("GetColumns")]
        public void GetColumns()
        {
            WallGetEndPoints();
            this.lstColumns = new List<RectColumn>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;


            ObjectId lyrColId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                List<Polyline> lstColPline = new List<Polyline>();
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has("Column"))
                {
                    return;
                }
                lyrColId = lyrTbl["Column"];



                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblModelSpaceRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblModelSpaceRec)
                {
                    Entity plEnt = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    Polyline pline = plEnt as Polyline;
                    if (pline == null)
                        continue;
                    if (pline.LayerId == lyrColId && pline.Closed == true)
                    {
                        double width = double.MaxValue;
                        double length = 0;
                        List<Point2d> lstVertices = new List<Point2d>();
                        lstColPline.Add(pline);
                        Point3d widthMidPt = Point3d.Origin;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            if (i + 1 == pline.NumberOfVertices)
                                break;
                            double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            width = Math.Min(width, dist);
                            if (width == dist)
                            {
                                widthMidPt = MathHelper.MidPoint(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            }
                            length = Math.Max(length, dist);
                        }
                        Extents3d extents = pline.GeometricExtents;
                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;

                        RectColumn col = new RectColumn(width, length, center, widthMidPt);
                        lstColumns.Add(col);
                    }
                }


                trans.Commit();
            }



            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls,this.lstColumns);
            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls);
        }


        [CommandMethod("GetFoooting")]
        public void GetFooting()
        {
            GetColumns();

            this.lstFooting = new List<RectFooting>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;


            ObjectId lyrFootingId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has("Footing"))
                {
                    return;
                }
                lyrFootingId = lyrTbl["Footing"];



                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblModelSpaceRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblModelSpaceRec)
                {
                    Entity plEnt = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    Polyline pline = plEnt as Polyline;
                    if (pline == null)
                        continue;
                    if (pline.LayerId == lyrFootingId && pline.Closed == true)
                    {
                        double width = double.MaxValue;
                        double length = 0;
                        List<Point2d> lstVertices = new List<Point2d>();

                        Point3d widthMidPt = Point3d.Origin;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            if (i + 1 == pline.NumberOfVertices)
                                break;
                            double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            width = Math.Min(width, dist);
                            if (width == dist)
                            {
                                widthMidPt = MathHelper.MidPoint(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            }
                            length = Math.Max(length, dist);
                        }
                        Extents3d extents = pline.GeometricExtents;
                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;

                        RectFooting footing = new RectFooting(width, length, center, widthMidPt);
                        lstFooting.Add(footing);
                    }
                }


                trans.Commit();
            }


        }

        [CommandMethod("GetSlab")]
        public void GetSlab()
        {
            GetFooting();

            this.lstSlab = new List<Slab>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;


            ObjectId lyrSlabId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has("Slab"))
                {
                    return;
                }
                lyrSlabId = lyrTbl["Slab"];



                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblModelSpaceRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblModelSpaceRec)
                {
                    Entity plEnt = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    Polyline pline = plEnt as Polyline;
                    if (pline == null)
                        continue;
                    if (pline.LayerId == lyrSlabId && pline.Closed == true)
                    {
                        double width = double.MaxValue;
                        double length = 0;
                        List<Point2d> lstVertices = new List<Point2d>();

                        Point3d widthMidPt = Point3d.Origin;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            if (i + 1 == pline.NumberOfVertices)
                                break;
                            double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            width = Math.Min(width, dist);
                            if (width == dist)
                            {
                                widthMidPt = MathHelper.MidPoint(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            }
                            length = Math.Max(length, dist);
                        }
                        Extents3d extents = pline.GeometricExtents;
                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;

                        Slab slab = new Slab(width, length, center, widthMidPt, pline.GetPoint3dAt(0).Z);
                        lstSlab.Add(slab);
                    }
                }


                trans.Commit();
            }



            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls, this.lstColumns, lstFooting);
            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls);
        }

        [CommandMethod("GetBuilding")]
        public void GetBuilding()
        {
            GetSlab();

            this.lstOpening = new List<Opening>();
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;


            ObjectId lyrOpningId = ObjectId.Null;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                //wall layer
                LayerTable lyrTbl = trans.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (!lyrTbl.Has(LayerName.Opening))
                {
                    return;
                }
                lyrOpningId = lyrTbl[LayerName.Opening];



                BlockTable blkTbl = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord blkTblModelSpaceRec = trans.GetObject(blkTbl[BlockTableRecord.ModelSpace], OpenMode.ForRead) as BlockTableRecord;

                foreach (ObjectId objId in blkTblModelSpaceRec)
                {
                    Entity plEnt = trans.GetObject(objId, OpenMode.ForRead) as Entity;
                    Polyline pline = plEnt as Polyline;
                    if (pline == null)
                        continue;
                    if (pline.LayerId == lyrOpningId && pline.Closed == true)
                    {
                        double width = double.MaxValue;
                        double length = 0;
                        List<Point2d> lstVertices = new List<Point2d>();

                        Point3d widthMidPt = Point3d.Origin;
                        for (int i = 0; i < pline.NumberOfVertices; i++)
                        {
                            if (i + 1 == pline.NumberOfVertices)
                                break;
                            double dist = MathHelper.CalcDistanceBetweenTwoPoint3D(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            width = Math.Min(width, dist);
                            if (width == dist)
                            {
                                widthMidPt = MathHelper.MidPoint(pline.GetPoint3dAt(i), pline.GetPoint3dAt(i + 1));
                            }
                            length = Math.Max(length, dist);
                        }
                        Extents3d extents = pline.GeometricExtents;
                        Point3d center = extents.MinPoint + (extents.MaxPoint - extents.MinPoint) / 2.0;

                        Opening opening = new Opening(width, length, center, widthMidPt, pline.GetPoint3dAt(0).Z);
                        lstOpening.Add(opening);
                    }
                }


                trans.Commit();
            }



            XbimCreateBuilding xbimWall = new XbimCreateBuilding(this.lstWalls, this.lstColumns, lstFooting, lstSlab, lstOpening);
            //XbimCreateWall xbimWall = new XbimCreateWall(this.lstWalls);
        }
    }
}
