using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ActorResource;
using Xbim.Ifc4.DateTimeResource;
using Xbim.Ifc4.ExternalReferenceResource;
using Xbim.Ifc4.PresentationOrganizationResource;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.PropertyResource;
using Xbim.Ifc4.QuantityResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.StructuralElementsDomain;
using CADReader.BuildingElements;
using CADReader;
using devDept.Eyeshot.Entities;
using CADReader.Helpers;
using CADReader.Base;
using IfcFileCreator.Helpers;
using CADReader.Reinforced_Elements;
using CADReader.ElementComponents;
using devDept.Geometry;

namespace IfcFileCreator
{
    public class XbimCreateBuilding
    {

        IfcSlab slab = null;
        private Random random = new Random(1000);
        public XbimCreateBuilding(Building cadBuilding, string pathToSave)
        {
            using (var model = CreateandInitModel("Demo1"))
            {

                List<FloorBase> lstSortedFloors = cadBuilding.Floors.OrderBy(f => f.Level).ToList();

                if (model != null)
                {
                    IfcBuilding building = CreateBuilding(model, "Default Building");
                    double lvlDifference = 0;

                    for (int i = 0; i < lstSortedFloors.Count; i++)
                    {
                        if (i + 1 != lstSortedFloors.Count)
                            lvlDifference = Math.Abs(lstSortedFloors[i].Level - lstSortedFloors[i + 1].Level);


                        Floor floor = lstSortedFloors[i] as Floor;
                        if (floor != null)
                        {

                            double wallHeight = lvlDifference - DefaultValues.SlabThinkess;
                            foreach (Wall cadWall in floor.Walls)
                            {
                                IfcWallStandardCase wall = CreateIfcWall(model, cadWall, wallHeight);

                                if (wall != null) AddPropertiesToWall(model, wall);
                                using (var txn = model.BeginTransaction("Add Wall"))
                                {
                                    building.AddElement(wall);
                                    txn.Commit();
                                }
                            }


                            foreach (ReinforcedCadColumn rcCol in floor.RcColumns)
                            {
                                IfcColumn column = CreateIfcRcColumn(model, rcCol, lvlDifference);
                                using (var txn = model.BeginTransaction("Add column"))
                                {
                                    building.AddElement(column);
                                    //rebarStopAndOperateengineer_
                                    foreach (var rebar in rcCol.LstRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                                        building.AddElement(bar);
                                    }
                                    int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (rcCol.Spacing));
                                    for (int j = 0; j < nStirrups - 1; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, rcCol.Spacing);
                                        building.AddElement(stirrup);
                                    }

                                    txn.Commit();
                                }
                            }
                            //foreach (RectColumn cadCol in floor.Columns)
                            //{

                            //    IfcColumn column = CreateIfcColumn(model, cadCol, lvlDifference);

                            //    using (var txn = model.BeginTransaction("Add column"))
                            //    {
                            //        building.AddElement(column);
                            //        txn.Commit();
                            //    }
                            //}

                            foreach (Slab cadSlab in floor.Slabs)
                            {
                                slab = CreateIfcSlab(model, cadSlab);
                                using (var trans = model.BeginTransaction("Add Slab"))
                                {
                                    building.AddElement(slab);
                                    IfcOpeningElement opening = null;
                                    foreach (var cadOpening in cadSlab.Openings)
                                    {
                                        opening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);

                                        building.AddElement(opening);
                                        //attach opening
                                        slab.AttchOpening(model, opening);


                                    }
                                    trans.Commit();
                                }
                            }



                            /*IfcOpeningElement opening = null;
                            foreach (var cadOpening in floor.Openings)
                            {
                                opening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);
                                using (var trans = model.BeginTransaction("Add Opening"))
                                {
                                    building.AddElement(opening);
                                    //attach opening
                                    slab.AttchOpening(model, opening);
                                    trans.Commit();
                                }

                            }*/

                            //Create stairs
                            foreach (Stair cadStair in floor.Stairs)
                            {

                                IfcStair stair = CreateIfcStair(model, cadStair);

                                using (var txn = model.BeginTransaction("Add Stair"))
                                {
                                    building.AddElement(stair);
                                    txn.Commit();
                                }
                            }
                            foreach (LinearPath cadLanding in floor.Landings)
                            {

                                IfcSlab landing = CreateIfcLanding(model, cadLanding, DefaultValues.SlabThinkess);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    building.AddElement(landing);
                                    txn.Commit();
                                }
                            }
                            foreach (ShearWall cadShearWall in floor.ShearWalls)
                            {

                                IfcColumn landing = CreateIfcShearWall(model, cadShearWall,lvlDifference);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    building.AddElement(landing);
                                    txn.Commit();
                                }
                            }

                            foreach (SlopedSlab cadRamp in floor.Ramps)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);

                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    building.AddElement(ramp);
                                    txn.Commit();
                                }
                            }
                        }
                        else
                        {
                            Foundation foundation = lstSortedFloors[i] as Foundation;
                            foreach (PCFooting cadFooting in foundation.PCFooting)
                            {

                                IfcFooting footing = CreateIfcFooting(model, cadFooting);

                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    building.AddElement(footing);
                                    txn.Commit();
                                }
                            }
                            foreach (RCFooting cadFooting in foundation.RCFooting)
                            {

                                IfcFooting footing = CreateIfcFooting(model, cadFooting);

                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    building.AddElement(footing);
                                    txn.Commit();
                                }
                            }
                            foreach (SlopedSlab cadRamp in foundation.Ramps)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);

                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    building.AddElement(ramp);
                                    txn.Commit();
                                }
                            }

                            double wallHeight = lvlDifference - DefaultValues.SlabThinkess;

                            foreach (Wall cadWall in foundation.RetainingWalls)
                            {

                                IfcWall wall = CreateIfcWall(model, cadWall, wallHeight);

                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    building.AddElement(wall);
                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadColumn rcCol in foundation.RcColumns)
                            {
                                IfcColumn column = CreateIfcRcColumn(model, rcCol, lvlDifference);
                                using (var txn = model.BeginTransaction("Add column"))
                                {
                                    building.AddElement(column);
                                    /*
                                    foreach (var rebar in rcCol.LstRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                                        building.AddElement(bar);
                                    }
                                    int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (rcCol.Spacing));
                                    for (int j = 0; j < nStirrups; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, rcCol.Spacing);
                                        building.AddElement(stirrup);
                                    }*/
                                    //IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.stirrup, 0);
                                    //IfcReinforcingBar stirrup2 = CreateIfcStirrup(model, rcCol.stirrup, 200);
                                    //building.AddElement(stirrup);
                                    //building.AddElement(stirrup2);
                                    txn.Commit();
                                }
                            }
                            foreach (ShearWall cadShearWall in foundation.ShearWalls)
                            {

                                IfcColumn landing = CreateIfcShearWall(model, cadShearWall, lvlDifference);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    building.AddElement(landing);
                                    txn.Commit();
                                }
                            }
                        }
                    }

                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        //write the Ifc File
                        model.SaveAs(pathToSave + @"\Demo1.ifc", IfcStorageType.Ifc);
                        Console.WriteLine("WallIfc4.ifc has been successfully written");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save!");
                        Console.WriteLine(e.Message);
                    }

                }

            }
        }

        private IfcBuilding CreateBuilding(IfcStore model, string name)
        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;

                building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                building.ObjectPlacement = localPlacement;
                var placement = model.Instances.New<IfcAxis2Placement3D>();
                localPlacement.RelativePlacement = placement;
                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project?.AddBuilding(building);
                txn.Commit();
                return building;
            }
        }

        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        private IfcStore CreateandInitModel(string projectName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = "xBimTeam",
                ApplicationFullName = "Hello Wall Application",
                ApplicationIdentifier = "HelloWall.exe",
                ApplicationVersion = "1.0",
                EditorsFamilyName = "Team",
                EditorsGivenName = "xBIM",
                EditorsOrganisationName = "xBimTeam"
            };
            //now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            //database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required

            var model = IfcStore.Create(credentials, IfcSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {

                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to SI (mm and metres)
                project.InitProject(CADConfig.Units);
                project.Name = projectName;
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;

        }
        /// <summary>
        /// This creates a wall and it's geometry, many geometric representations are possible and extruded rectangular footprint is chosen as this is commonly used for standard case walls
        /// </summary>
        /// <param name="model"></param>
        /// <param name="length">Length of the rectangular footprint</param>
        /// <param name="width">Width of the rectangular footprint (width of the wall)</param>
        /// <param name="height">Height to extrude the wall, extrusion is vertical</param>
        /// <returns></returns>
        private IfcWallStandardCase CreateIfcWall(IfcStore model, Wall cadWall, double height)
        {
            //cadWall.Thickness *= 1000;
            //cadWall.StPt.X *= 1000;
            //cadWall.StPt.Y*= 1000;
            //cadWall.EndPt.X *= 1000;
            //cadWall.EndPt.Y *= 1000;

            //dimensions of the new IFC Wall we want to create
            double length = MathHelper.CalcDistanceBetweenTwoPoint3D(cadWall.StPt, cadWall.EndPt);
            double width = cadWall.Thickness;

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wallToCreate = model.Instances.New<IfcWallStandardCase>();
                wallToCreate.Name = " Wall - Wall:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile Insertion Point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);

                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);

                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                wallToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall
                var midPt = MathHelper.MidPoint3D(cadWall.StPt, cadWall.EndPt);

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(midPt.X, midPt.Y, midPt.Z);

                var uvWallLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadWall.StPt, cadWall.EndPt);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvWallLongDir.X, uvWallLongDir.Y, uvWallLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);

                wallToCreate.ObjectPlacement = lp;


                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);

                trans.Commit();
                return wallToCreate;
            }

        }

        private IfcColumn CreateIfcColumn(IfcStore model, RectColumn cadCol, double height)
        {
            //cadCol.Length *= 1000;
            //cadCol.Width *= 1000;

            //cadCol.CenterPt.X *= 1000;
            //cadCol.CenterPt.Y *= 1000;

            //cadCol.PtLengthDir.X *= 1000;
            //cadCol.PtLengthDir.Y *= 1000;
            //
            double length = cadCol.Length;
            double width = cadCol.Width;
            //begin a transaction
            using (var trans = model.BeginTransaction("Create column"))
            {
                IfcColumn colToCreate = model.Instances.New<IfcColumn>();
                colToCreate.Name = "UC-Universal Columns-Column:UC305x305x97:" + random.Next(1000, 10000);
                colToCreate.PredefinedType = IfcColumnTypeEnum.COLUMN;
                //represent column as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);


                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                colToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadCol.CenterPt.X, cadCol.CenterPt.Y, cadCol.CenterPt.Z);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadCol.CenterPt, cadCol.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                colToCreate.ObjectPlacement = lp;

                trans.Commit();
                return colToCreate;
            }

        }

        private IfcFooting CreateIfcFooting(IfcStore model, FootingBase cadFooting)
        {
            
            //begin a transaction
            using (var trans = model.BeginTransaction("Create Footing"))
            {
                IfcFooting footingToCreate = model.Instances.New<IfcFooting>();
                footingToCreate.Name = " Foundation - Footing:UC305x305x97: " + random.Next(1000, 10000);

                //represent footing as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadFooting.ProfilePath.Vertices.ToList());



                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadFooting.Thickness, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                footingToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0,0, cadFooting.Type == "PC" ? 0 : 0 + DefaultValues.PCFootingThinkess);

                //var uvFootingLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadFooting.CenterPt, cadFooting.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1,0,0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);


                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                footingToCreate.ObjectPlacement = lp;


                trans.Commit();
                return footingToCreate;
            }

        }


        private IfcColumn CreateIfcShearWall(IfcStore model, ShearWall cadShearWall,double height)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Shear Wall"))
            {
                IfcColumn shearWallToCreate = model.Instances.New<IfcColumn>();
                shearWallToCreate.Name = " ShearWall - ShearWall:UC305x305x97: " + random.Next(1000, 10000);

                //represent footing as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadShearWall.ProfilePath.Vertices.ToList());



                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model,height, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                shearWallToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the wall

                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0,0);

                //var uvFootingLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadFooting.CenterPt, cadFooting.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);


                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                shearWallToCreate.ObjectPlacement = lp;


                trans.Commit();
                return shearWallToCreate;
            }

        }

        private IfcSlab CreateIfcRectSlab(IfcStore model, RectSlab cadSlab)
        {
            //cadSlab.Length *= 1000;
            //cadSlab.Width *= 1000;
            //cadSlab.Thickness *= 1000;
            //cadSlab.CenterPt.X *= 1000;
            //cadSlab.CenterPt.Y *= 1000;

            //cadSlab.PtLengthDir.X *= 1000;
            //cadSlab.PtLengthDir.Y *= 1000;
            //
            double length = cadSlab.Length;
            double width = cadSlab.Width;
            //begin a transaction
            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = " Slab - Slab:UC305x305x97:" + random.Next(1000, 10000);

                //represent Element as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadSlab.Thickness, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadSlab.CenterPt.X, cadSlab.CenterPt.Y, cadSlab.CenterPt.Z);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                slabToCreate.ObjectPlacement = lp;


                trans.Commit();
                return slabToCreate;
            }

        }

        private IfcSlab CreateIfcSlopedSlab(IfcStore model, SlopedSlab cadSlab)
        {
            //for (int i = 0; i < cadSlab.LstFacePt.Count; i++)
            //{
            //    cadSlab.LstFacePt[i] *= 1000;
            //}
            //cadSlab.Thickness *= 1000;

            //begin a transaction
            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = " Slab - Slab:UC305x305x97:" + random.Next(1000, 10000);

                //represent Element as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.LstFacePt);

                //Profile insertion point 


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadSlab.Thickness, profile, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0, 0);

                //var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                slabToCreate.ObjectPlacement = lp;


                trans.Commit();
                return slabToCreate;
            }

        }

        private IfcSlab CreateIfcSlab(IfcStore model, Slab cadSlab)
        {
            //for (int i = 0; i < cadSlab.LstFacePt.Count; i++)
            //{
            //    cadSlab.LstFacePt[i] *= 1000;
            //}
            //cadSlab.Thickness *= 1000;

            //begin a transaction
            using (ITransaction trans = model.BeginTransaction("Create Slab"))
            {
                IfcSlab slabToCreate = model.Instances.New<IfcSlab>();
                slabToCreate.Name = " Slab - Slab:UC305x305x97:" + random.Next(1000, 10000);

                //represent Element as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.LstFacePt);

                //Profile insertion point 


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, cadSlab.Thickness, profile, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);



                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                slabToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0, 0);

                //var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                slabToCreate.ObjectPlacement = lp;


                trans.Commit();
                return slabToCreate;
            }

        }


        private IfcOpeningElement CreateIfcOpening(IfcStore model, Opening cadOpening, double thickness)
        {
            //cadOpening.CenterPt.X *= 1000;
            //cadOpening.CenterPt.Y *= 1000;

            //cadOpening.PtLengthDir.X *= 1000;
            //cadOpening.PtLengthDir.Y *= 1000;

            //cadOpening.Length *= 1000;
            //cadOpening.Width *= 1000;
            //
            double length = cadOpening.Length;
            double width = cadOpening.Width;
            //begin a transaction
            //using (var trans = model.BeginTransaction("Create Opening"))
            //{
                IfcOpeningElement openingToCreate = model.Instances.New<IfcOpeningElement>();
                openingToCreate.Name = " Openings - Openings:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);

                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                var insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXYZ(0, 0, cadOpening.CenterPt.Z); //insert at arbitrary position
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);


                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the opening
                IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
                prDefRep.Representations.Add(shape);
                openingToCreate.Representation = prDefRep;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadOpening.CenterPt.X, cadOpening.CenterPt.Y, cadOpening.CenterPt.Z);

                var uvColLongDir = MathHelper.UnitVectorFromPt1ToPt2(cadOpening.CenterPt, cadOpening.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                openingToCreate.ObjectPlacement = lp;


                //commit transaction
               // trans.Commit();

                return openingToCreate;
            //}

        }

        private IfcStair CreateIfcStair(IfcStore model, Stair stair)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Stair"))
            {
                IfcStair stairToCreate = model.Instances.New<IfcStair>();
                stairToCreate.Name = " stair - Wall:UC305x305x97:" + 1500;


                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                //Create a Definition shape to hold the geometry of the Stair 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");

                for (int i = 0; i < stair.LstStep.Count; i++)
                {
                    IfcArbitraryClosedProfileDef stepProfile = IFCHelper.ArbitraryClosedProfileCreate(model, (stair.LstStep[i].Vertices/*.Select(v => v * 1000)*/).ToList());

                    IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, stair.Thickness, stepProfile, extrusionDir);

                    body.BodyPlacementSet(model, 0, 0, stair.IsUp ? stair.Thickness * i : stair.Thickness * -i);

                    shape.Items.Add(body);
                }







                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);

                IfcStairFlight flight = model.Instances.New<IfcStairFlight>();
                flight.Representation = prDefShape;

                IfcRelAggregates relAggregate = model.Instances.New<IfcRelAggregates>();
                relAggregate.RelatingObject = stairToCreate;
                relAggregate.RelatedObjects.Add(flight);

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0, 0);

                //var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                flight.ObjectPlacement = lp;

                trans.Commit();
                return stairToCreate;
            }

        }

        private IfcSlab CreateIfcLanding(IfcStore model, LinearPath landingPline, double landingThickness)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {

                IfcSlab landing = model.Instances.New<IfcSlab>();


                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, -1);

                //Create a Definition shape to hold the geometry of the Stair 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");


                IfcArbitraryClosedProfileDef stepProfile = IFCHelper.ArbitraryClosedProfileCreate(model, (landingPline.Vertices/*.Select(v => v * 1000)*/).ToList());

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, landingThickness, stepProfile, extrusionDir);


                body.BodyPlacementSet(model, 0, 0, 0);

                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);

                landing.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(0, 0, 0);

                //var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the slab into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                landing.ObjectPlacement = lp;
                trans.Commit();
                return landing;
            }

        }

        private IfcColumn CreateIfcRcColumn(IfcStore model, ReinforcedCadColumn cadCol, double height)
        {
            //cadCol.RectColumn.Length *= 1000;
            //cadCol.RectColumn.Width *= 1000;

            //cadCol.RectColumn.CenterPt.X *= 1000;
            //cadCol.RectColumn.CenterPt.Y *= 1000;

            //cadCol.RectColumn.PtLengthDir.X *= 1000;
            //cadCol.RectColumn.PtLengthDir.Y *= 1000;
            //
            double length = cadCol.RectColumn.Length;
            double width = cadCol.RectColumn.Width;
            //begin a transaction
            using (var trans = model.BeginTransaction("Create column"))
            {
                IfcColumn colToCreate = model.Instances.New<IfcColumn>();
                colToCreate.Name = "UC-Universal Columns-Column:UC305x305x97:" + 1600;

                //represent column as a rectangular profile
                IfcRectangleProfileDef rectProf = IFCHelper.RectProfileCreate(model, length, width);


                //Profile insertion point
                rectProf.ProfileInsertionPointSet(model, 0, 0);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                colToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(cadCol.RectColumn.CenterPt.X, cadCol.RectColumn.CenterPt.Y, cadCol.RectColumn.CenterPt.Z);

                var uvColLongDir = MathHelper.UnitVector3DFromPt1ToPt2(cadCol.RectColumn.CenterPt, cadCol.RectColumn.PtLengthDir);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(uvColLongDir.X, uvColLongDir.Y, uvColLongDir.Z);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                colToCreate.ObjectPlacement = lp;

                trans.Commit();
                return colToCreate;
            }

        }

        private IfcReinforcingBar CreateIfcRebar(IfcStore model, Rebar rebar, double height)
        {
            //rebar.LocationPt.X *= 1000;
            //rebar.LocationPt.Y *= 1000;
            //rebar.LocationPt.Z *= 1000;
            //rebar.Diameter *= 1000;
            rebar.LocationPt.Z += (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000) + height;

            Point3D endPoint = new Point3D(rebar.LocationPt.X, rebar.LocationPt.Y, rebar.LocationPt.Z - (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000) - height);

            //begin a transaction
            //   using (var trans = model.BeginTransaction("Create column"))
            //   {
            IfcReinforcingBar rebarToCreate = model.Instances.New<IfcReinforcingBar>();
            rebarToCreate.Name = "UC-Universal Rebar" + 1700;

            //represent column as a rectangular profile
            IfcCompositeCurveSegment segment = IFCHelper.CreateCurveSegment(model, rebar.LocationPt, endPoint);
            List<IfcCompositeCurveSegment> segments = new List<IfcCompositeCurveSegment>
                {
                    segment
                };
            IfcCompositeCurve compProf = IFCHelper.CreateCompositeProfile(model, segments);


            //model as a swept disk solid 

            IfcSweptDiskSolid body = IFCHelper.SweptDiskSolidCreate(model, compProf, rebar.Diameter / 2);


            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            rebarToCreate.Representation = prDefShape;



            //    trans.Commit();
            return rebarToCreate;
            // }

        }

        private IfcReinforcingBar CreateIfcStirrup(IfcStore model, Stirrup stirrup, double zPosition)
        {
            List<Line> lstBranchesNew = new List<Line>();
            List<IfcCompositeCurveSegment> lstSegment = new List<IfcCompositeCurveSegment>();
            IfcCompositeCurveSegment segment = null;
            for (int i = 0; i < stirrup.LstBranch.Count; i++)
            {


                stirrup.LstBranch[i].StartPoint.Z += zPosition;
                stirrup.LstBranch[i].EndPoint.Z += zPosition;


                segment = IFCHelper.CreateCurveSegment(model, stirrup.LstBranch[i].StartPoint, stirrup.LstBranch[i].EndPoint);
                lstSegment.Add(segment);
            }
            //stirrup.Diameter *= 1000;





            //Point3D endPoint = new Point3D(stirrup.LocationPt.X, stirrup.LocationPt.Y, stirrup.LocationPt.Z - 1000 - height);

            //begin a transaction
            //   using (var trans = model.BeginTransaction("Create column"))
            //   {
            IfcReinforcingBar stirrupToCreate = model.Instances.New<IfcReinforcingBar>();
            stirrupToCreate.Name = "UC-Universal Rebar" + 1700;

            //represent column as a rectangular profile

            IfcCompositeCurve compProf = IFCHelper.CreateCompositeProfile(model, lstSegment);


            //model as a swept disk solid 

            IfcSweptDiskSolid body = IFCHelper.SweptDiskSolidCreate(model, compProf, stirrup.Diameter / 2);


            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            stirrupToCreate.Representation = prDefShape;



            //    trans.Commit();
            return stirrupToCreate;
            // }

        }


        /// <summary>
        /// Add some properties to the wall,
        /// </summary>
        /// <param name="model">XbimModel</param>
        /// <param name="wall"></param>
        private void AddPropertiesToWall(IfcStore model, IfcWallStandardCase wall)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                CreateSimpleProperty(model, wall);
                txn.Commit();
            }
        }

        private void CreateSimpleProperty(IfcStore model, IfcWallStandardCase wall)
        {
            var ifcPropertySingleValue = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "IfcPropertySingleValue:Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                });
            });


            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Test:IfcPropertySet";
                ps.Description = "Property Set";
                ps.HasProperties.Add(ifcPropertySingleValue);
            });

            //need to create the relationship
            model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.Name = "Property Association";
                rdbp.Description = "IfcPropertySet associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcPropertySet;
            });
        }




    }
}
