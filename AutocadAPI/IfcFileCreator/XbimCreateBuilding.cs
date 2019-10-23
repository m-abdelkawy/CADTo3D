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
using Xbim.Ifc4.SharedComponentElements;
using Xbim.Ifc4.ElectricalDomain;
using CADReader.ElectricalElements;

namespace IfcFileCreator
{
    public class XbimCreateBuilding
    {
        public Submissions BuildingSubmissions { get; set; }

        IfcSlab slab = null;
        private Random random = new Random(1000);
        public XbimCreateBuilding(/*List<Building> lstBuilding, */Building cadBuilding, string pathToSave)
        {
            BuildingSubmissions = new Submissions();

            using (var model = CreateandInitModel("Demo1"))
            {

                //for (int m = 0; m < lstBuilding.Count; m++)

                List<FloorBase> lstSortedFloors = cadBuilding.Floors.OrderBy(f => f.Level).ToList();

                if (model != null)
                {
                    IfcBuilding building = CreateBuilding(model, "Default Building", cadBuilding.Location);
                    //rel.
                    double lvlDifference = 0;

                    for (int i = 0; i < lstSortedFloors.Count; i++)
                    {
                        IfcBuildingStorey storey;
                        using (var txn = model.BeginTransaction("Add Storey"))
                        {

                            storey = model.Instances.New<IfcBuildingStorey>();
                            IfcRelAggregates rel = model.Instances.New<IfcRelAggregates>();
                            rel.RelatingObject = building;
                            rel.RelatedObjects.Add(storey);

                            txn.Commit();
                        }


                        if (i + 1 != lstSortedFloors.Count)
                            lvlDifference = Math.Abs(lstSortedFloors[i].Level - lstSortedFloors[i + 1].Level);


                        Floor floor = lstSortedFloors[i] as Floor;
                        if (floor != null)
                        {
                            List<IfcProduct> lstCols = new List<IfcProduct>();
                            List<IfcProduct> lstColFormWork = new List<IfcProduct>();

                            List<IfcProduct> lstShearWall = new List<IfcProduct>();
                            List<IfcProduct> lstShearWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlabFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlab = new List<IfcProduct>();
                            List<IfcProduct> lstWall = new List<IfcProduct>();
                            List<IfcProduct> lstWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstStair = new List<IfcProduct>();
                            List<IfcProduct> lstRebar = new List<IfcProduct>();
                            List<IfcProduct> lstSlabRebar = new List<IfcProduct>();


                            double wallHeight = lvlDifference - DefaultValues.SlabThinkess;
                            foreach (ReinforcedCadWall rcWall in floor.ReinforcedCadWalls)
                            {
                                IfcWallStandardCase wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);

                                if (wall != null) AddPropertiesToWall(model, wall);
                                using (var txn = model.BeginTransaction("Add RetainingWall"))
                                {
                                    storey.AddElement(wall);
                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add wall to Submission
                                    lstWall.Add(wall);
                                    lstWallFormWork.Add(formWork);
                                    lstWallFormWork.Add(opening);

                                    ////Wall Rft
                                    for (int j = 0; j < rcWall.LstRebar.Count; j++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rcWall.LstRebar[j], wallHeight);
                                        storey.AddElement(bar);
                                    }
                                    int nStirrups = Convert.ToInt32(lvlDifference / (DefaultValues.StirrupsSpacing));
                                    for (int j = 0; j < nStirrups; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcWall.Stirrup, DefaultValues.StirrupsSpacing);
                                        storey.AddElement(stirrup);
                                        //lstColRebar.Add(stirrup);
                                    }

                                    txn.Commit();
                                }
                            }


                            foreach (ReinforcedCadColumn rcCol in floor.RcColumns)
                            {
                                IfcColumn column = CreateIfcRcColumn(model, rcCol, lvlDifference);
                                using (var txn = model.BeginTransaction("Add RcColumn"))
                                {
                                    storey.AddElement(column);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.RectColumn.ColPath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add columns to Submission
                                    lstCols.Add(column);

                                    lstColFormWork.Add(opening);
                                    lstColFormWork.Add(formWork);


                                    //rebarStopAndOperate
                                    //if (i == lstSortedFloors.Count - 1)
                                    //{
                                    foreach (var rebar in rcCol.LstRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                                        storey.AddElement(bar);
                                        lstRebar.Add(bar);
                                    }
                                    int nstirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (rcCol.Spacing));
                                    for (int j = 0; j < nstirrups - 1; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, rcCol.Spacing);
                                        storey.AddElement(stirrup);
                                        lstRebar.Add(stirrup);

                                    }
                                    //}
                                    txn.Commit();
                                }
                            }


                            foreach (ReinforcedCadSlab cadRCSlab in floor.RcSlab)
                            {
                                slab = CreateIfcSlab(model, cadRCSlab.Slab);
                                using (var trans = model.BeginTransaction("Add Slab"))
                                {
                                    storey.AddElement(slab);

                                    IfcOpeningElement openingFormWork;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRCSlab.Slab.linPathSlab, DefaultValues.FormWorkThickness, cadRCSlab.Slab.Thickness, out openingFormWork, true);
                                    storey.AddElement(openingFormWork);
                                    storey.AddElement(formWork);

                                    //add slab to Submission
                                    lstSlab.Add(slab);

                                    lstSlabFormWork.Add(openingFormWork);
                                    lstSlabFormWork.Add(formWork);
                                    List<IfcOpeningElement> lstOpening = new List<IfcOpeningElement>();
                                    IfcOpeningElement opening = null;
                                    for (int n = 0; n < cadRCSlab.Slab.Openings.Count; n++)
                                    {
                                        var cadOpening = cadRCSlab.Slab.Openings[n];
                                        opening = CreateIfcOpening(model, cadOpening, DefaultValues.SlabThinkess);

                                        lstOpening.Add(opening);

                                        storey.AddElement(opening);

                                        lstSlab.Add(opening);

                                        //attach opening
                                        slab.AttchOpening(model, opening);
                                        formWork.AttchOpening(model, opening);

                                    }


                                    for (int k = 0; k < cadRCSlab.OpeningsRFT.Count; k++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.OpeningsRFT[k], 0);
                                        for (int j = 0; j < lstOpening.Count; j++)
                                        {
                                            bar.AttchOpening(model, lstOpening[j]);

                                            lstSlabRebar.Add(bar);
                                        }
                                        storey.AddElement(bar);
                                    }

                                    for (int j = 0; j < cadRCSlab.RFT.Count; j++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, cadRCSlab.RFT[j], 0);
                                        storey.AddElement(bar);
                                        lstSlabRebar.Add(bar);

                                    }
                                    trans.Commit();
                                }
                            }



                            //Create stairs
                            foreach (Stair cadStair in floor.Stairs)
                            {
                                IfcStairFlight flight;
                                IfcStair stair = CreateIfcStair(model, cadStair, out flight);

                                using (var txn = model.BeginTransaction("Add Stair"))
                                {
                                    storey.AddElement(stair);

                                    //add stair to Submission
                                    lstStair.Add(flight);

                                    txn.Commit();
                                }
                            }
                            foreach (LinearPath cadLanding in floor.Landings)
                            {

                                IfcSlab landing = CreateIfcLanding(model, cadLanding, DefaultValues.SlabThinkess);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    storey.AddElement(landing);

                                    lstStair.Add(landing);

                                    txn.Commit();
                                }
                            }
                            foreach (ShearWall cadShearWall in floor.ShearWalls)
                            {

                                IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall, lvlDifference);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    storey.AddElement(shearWall);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ProfilePath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add shear wall to Submission
                                    lstShearWall.Add(shearWall);

                                    lstShearWallFormWork.Add(opening);
                                    lstShearWallFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (SlopedSlab cadRamp in floor.Ramps)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);



                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    storey.AddElement(ramp);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness, cadRamp.Thickness, out opening, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add ramp to Submission
                                    lstSlab.Add(ramp);

                                    lstSlabFormWork.Add(opening);
                                    lstSlabFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ElectricalConduit cadConduit in floor.ElecConduits)
                            {

                                using (var txn = model.BeginTransaction("Add conduit"))
                                {
                                IfcCableCarrierSegment conduit = CreateIfcConduit(model, cadConduit);
                                    storey.AddElement(conduit);
                                    txn.Commit();
                                }
                            }


                            BuildingSubmissions.SubmittedElems.Add(lstStair);
                            BuildingSubmissions.SubmittedElems.Add(lstSlabFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstSlabRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstSlab);
                            BuildingSubmissions.SubmittedElems.Add(lstRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstColFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstCols);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWall);
                            BuildingSubmissions.SubmittedElems.Add(lstWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstWall);
                        }
                        else
                        {

                            List<IfcProduct> lstPCFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstPCFooting = new List<IfcProduct>();
                            List<IfcProduct> lstRCFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstRCFooting = new List<IfcProduct>();
                            List<IfcProduct> lstColFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstCol = new List<IfcProduct>();
                            List<IfcProduct> lstShearWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstShearWall = new List<IfcProduct>();
                            List<IfcProduct> lstSemelle = new List<IfcProduct>();
                            List<IfcProduct> lstWallFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstWall = new List<IfcProduct>();
                            List<IfcProduct> lstSlabFormWork = new List<IfcProduct>();
                            List<IfcProduct> lstSlab = new List<IfcProduct>();
                            List<IfcProduct> lstRebar = new List<IfcProduct>();
                            List<IfcProduct> lstColRebar = new List<IfcProduct>();


                            Foundation foundation = lstSortedFloors[i] as Foundation;
                            foreach (PCFooting cadFooting in foundation.PCFooting)
                            {

                                IfcFooting footing = CreateIfcFooting(model, cadFooting);



                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    storey.AddElement(footing);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.ProfilePath, DefaultValues.FormWorkThickness, cadFooting.Thickness, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add pcfooting to Submission
                                    lstPCFooting.Add(footing);

                                    lstPCFormWork.Add(opening);
                                    lstPCFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadSemelle cadSemelle in foundation.ReinforcedSemelles)
                            {
                                IfcBeam semelle = CreateIfcBeam(model, cadSemelle.Semelle);

                                using (var txn = model.BeginTransaction("Add Semelle"))
                                {
                                    storey.AddElement(semelle);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadSemelle.Semelle.HzLinPath,
                                        DefaultValues.FormWorkThickness, cadSemelle.Semelle.Thickness, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add pcfooting to Submission
                                    //lstPCFooting.Add(footing);

                                    //Steel
                                    for (int l = 0; l < cadSemelle.Rebars.Count(); l++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, cadSemelle.Rebars[l], 0);
                                        storey.AddElement(bar);
                                    }

                                    lstPCFormWork.Add(opening);
                                    lstPCFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadFooting cadFooting in foundation.ReinforcedCadFootings)
                            {

                                IfcFooting footing = CreateIfcFooting(model, cadFooting.RcFooting);



                                using (var txn = model.BeginTransaction("Add Footing"))
                                {
                                    storey.AddElement(footing);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadFooting.RcFooting.ProfilePath, DefaultValues.FormWorkThickness, cadFooting.RcFooting.Thickness, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);


                                    //add rcfooting to Submission
                                    lstRCFooting.Add(footing);

                                    lstRCFormWork.Add(opening);
                                    lstRCFormWork.Add(formWork);

                                    foreach (var longBar in cadFooting.LongRft)
                                    {
                                        IfcReinforcingBar barLong = CreateIfcRebar(model, longBar, 0);
                                        storey.AddElement(barLong);
                                        lstRebar.Add(barLong);
                                    }

                                    foreach (var transverseBar in cadFooting.TransverseRft)
                                    {
                                        IfcReinforcingBar barLong = CreateIfcRebar(model, transverseBar, 0);
                                        storey.AddElement(barLong);
                                        lstRebar.Add(barLong);

                                    }

                                    txn.Commit();
                                }
                            }

                            foreach (SlopedSlab cadRamp in foundation.Ramps)
                            {

                                IfcSlab ramp = CreateIfcSlopedSlab(model, cadRamp);

                                using (var txn = model.BeginTransaction("Add Ramp"))
                                {
                                    storey.AddElement(ramp);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadRamp.LinPathSlopedSlab, DefaultValues.FormWorkThickness, cadRamp.Thickness, out opening, true);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstSlab.Add(ramp);

                                    lstSlabFormWork.Add(opening);
                                    lstSlabFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            double wallHeight = lvlDifference - DefaultValues.SlabThinkess;

                            foreach (ReinforcedCadWall rcWall in foundation.ReinforcedCadWalls)
                            {

                                IfcWall wall = CreateIfcWall(model, rcWall.CadWall, wallHeight, building);

                                using (var txn = model.BeginTransaction("Add RetainingWall"))
                                {
                                    storey.AddElement(wall);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcWall.CadWall.LinPathWall, DefaultValues.FormWorkThickness, wallHeight, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstWall.Add(wall);

                                    lstWallFormWork.Add(opening);
                                    lstWallFormWork.Add(formWork);

                                    //Wall Rft
                                    for (int j = 0; j < rcWall.LstRebar.Count; j++)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rcWall.LstRebar[j], wallHeight);
                                        storey.AddElement(bar);
                                    }
                                    int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (DefaultValues.StirrupsSpacing));
                                    for (int j = 0; j < nStirrups; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcWall.Stirrup, DefaultValues.StirrupsSpacing);
                                        storey.AddElement(stirrup);
                                        //lstColRebar.Add(stirrup);

                                    }

                                    txn.Commit();
                                }
                            }

                            foreach (ReinforcedCadColumn rcCol in foundation.RcColumns)
                            {
                                IfcColumn column = CreateIfcRcColumn(model, rcCol, lvlDifference);
                                using (var txn = model.BeginTransaction("Add column"))
                                {
                                    storey.AddElement(column);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, rcCol.RectColumn.ColPath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstCol.Add(column);

                                    lstColFormWork.Add(opening);
                                    lstColFormWork.Add(formWork);



                                    foreach (var rebar in rcCol.LstRebar)
                                    {
                                        IfcReinforcingBar bar = CreateIfcRebar(model, rebar, lvlDifference);
                                        storey.AddElement(bar);
                                        lstColRebar.Add(bar);
                                    }
                                    int nStirrups = Convert.ToInt32((lvlDifference + (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000)) / (rcCol.Spacing));
                                    for (int j = 0; j < nStirrups; j++)
                                    {
                                        IfcReinforcingBar stirrup = CreateIfcStirrup(model, rcCol.Stirrup, rcCol.Spacing);
                                        storey.AddElement(stirrup);
                                        lstColRebar.Add(stirrup);

                                    }

                                    txn.Commit();
                                }
                            }
                            foreach (ShearWall cadShearWall in foundation.ShearWalls)
                            {

                                IfcColumn shearWall = CreateIfcShearWall(model, cadShearWall, lvlDifference);

                                using (var txn = model.BeginTransaction("Add Landing"))
                                {
                                    storey.AddElement(shearWall);

                                    IfcOpeningElement opening;
                                    IfcBuildingElementPart formWork = CreateFormWork(model, cadShearWall.ProfilePath, DefaultValues.FormWorkThickness, lvlDifference, out opening);
                                    storey.AddElement(opening);
                                    storey.AddElement(formWork);

                                    //add rcfooting to Submission
                                    lstShearWall.Add(shearWall);

                                    lstShearWallFormWork.Add(opening);
                                    lstShearWallFormWork.Add(formWork);

                                    txn.Commit();
                                }
                            }

                            BuildingSubmissions.SubmittedElems.Add(lstPCFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstPCFooting);
                            BuildingSubmissions.SubmittedElems.Add(lstRCFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstRCFooting);
                            BuildingSubmissions.SubmittedElems.Add(lstColRebar);
                            BuildingSubmissions.SubmittedElems.Add(lstColFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstCol);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstShearWall);
                            //  BuildingSubmissions.SubmittedElems.Add(lstSlab);
                            //BuildingSubmissions.SubmittedElems.Add(lstStair);
                            BuildingSubmissions.SubmittedElems.Add(lstWallFormWork);
                            BuildingSubmissions.SubmittedElems.Add(lstWall);

                        }
                    }



                }

                try
                {

                    model.SaveAs(pathToSave + @"\Demo1.ifc", IfcStorageType.Ifc);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private IfcBuilding CreateBuilding(IfcStore model, string name, Point3D location)

        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>();
                building.Name = name;

                building.CompositionType = IfcElementCompositionEnum.ELEMENT;
                var localPlacement = model.Instances.New<IfcLocalPlacement>();

                var placement = model.Instances.New<IfcAxis2Placement3D>();
                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(location.X, location.Y, location.Z));

                localPlacement.RelativePlacement = placement;
                building.ObjectPlacement = localPlacement;
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project?.InitProject(CADConfig.Units);
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
        private IfcWallStandardCase CreateIfcWall(IfcStore model, Wall cadWall, double height, IfcBuilding building)
        {


            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wallToCreate = model.Instances.New<IfcWallStandardCase>();
                wallToCreate.Name = " Wall - Wall:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadWall.LinPathWall.Vertices.ToList());


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


                //Placement To building
                var localPlacement = model.Instances.New<IfcLocalPlacement>();

                localPlacement.PlacementRelTo = building.ObjectPlacement;
                wallToCreate.ObjectPlacement = localPlacement;

                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                wallToCreate.Representation = prDefShape;


                trans.Commit();
                return wallToCreate;
            }

        }

        private IfcBeam CreateIfcBeam(IfcStore model, Semelle semelle)
        {


            //begin a transaction
            using (var trans = model.BeginTransaction("Create Wall"))
            {
                IfcBeam beamToCreate = model.Instances.New<IfcBeam>();
                beamToCreate.Name = " Wall - Wall:UC305x305x97:" + random.Next(1000, 10000);

                //represent wall as a rectangular profile
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, semelle.HzLinPath.Vertices.ToList());


                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);


                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, semelle.Thickness, profile, extrusionDir);



                //Create a Definition shape to hold the geometry of the wall 3D body
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                beamToCreate.Representation = prDefShape;


                trans.Commit();
                return beamToCreate;
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





                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                footingToCreate.Representation = prDefShape;



                trans.Commit();
                return footingToCreate;
            }

        }

        private IfcColumn CreateIfcShearWall(IfcStore model, ShearWall cadShearWall, double height)
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

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);


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
                location3D.SetXYZ(0, 0, 0);

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
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.LinPathSlopedSlab.Vertices.ToList());

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
                IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadSlab.linPathSlab.Vertices.ToList());

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

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness + DefaultValues.FormWorkThickness, rectProf, extrusionDir);


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
        private IfcOpeningElement CreateIfcOpening(IfcStore model, LinearPath cadOpeninlinPath, double thickness, bool isSlab = false)
        {


            IfcOpeningElement openingToCreate = model.Instances.New<IfcOpeningElement>();
            openingToCreate.Name = " Openings - Openings:UC305x305x97:";

            //represent wall as a rectangular profile
            IfcArbitraryClosedProfileDef profile = IFCHelper.ArbitraryClosedProfileCreate(model, cadOpeninlinPath.Vertices.ToList());




            //model as a swept area solid
            IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
            extrusionDir.SetXYZ(0, 0, isSlab ? -1 : 1);

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, thickness, profile, extrusionDir);


            //parameters to insert the geometry in the model
            body.BodyPlacementSet(model, 0, 0, 0/*zBottomFace*/);


            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the opening
            IfcProductDefinitionShape prDefRep = model.Instances.New<IfcProductDefinitionShape>();
            prDefRep.Representations.Add(shape);
            openingToCreate.Representation = prDefRep;

            return openingToCreate;

        }

        private IfcStair CreateIfcStair(IfcStore model, Stair stair, out IfcStairFlight flight)
        {

            //begin a transaction
            using (var trans = model.BeginTransaction("Create Stair"))
            {
                IfcStair stairToCreate = model.Instances.New<IfcStair>();
                stairToCreate.Name = " Stair :UC305x305x97:" + random.Next(10000);


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

                flight = model.Instances.New<IfcStairFlight>();
                flight.Name = " Stair :Flight:" + random.Next(10000);
                flight.Representation = prDefShape;

                IfcRelAggregates relAggregate = model.Instances.New<IfcRelAggregates>();
                relAggregate.RelatingObject = stairToCreate;
                relAggregate.RelatedObjects.Add(flight);

                ////Create Local axes system and assign it to the column
                //IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                //location3D.SetXYZ(0, 0, 0);

                ////var uvColLongDir = MathHelper.UnitVectorPtFromPt1ToPt2(cadSlab.CenterPt, cadSlab.PtLengthDir);

                //IfcDirection localXDir = model.Instances.New<IfcDirection>();
                //localXDir.SetXYZ(1, 0, 0);

                //IfcDirection localZDir = model.Instances.New<IfcDirection>();
                //localZDir.SetXYZ(0, 0, 1);

                //IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                ////now place the slab into the model
                //IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                //flight.ObjectPlacement = lp;

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
                landing.Name = " Landing :UC305x305x97:" + random.Next(10000);

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

            //begin a transaction
            using (var trans = model.BeginTransaction("Create column"))
            {
                IfcColumn colToCreate = model.Instances.New<IfcColumn>();
                colToCreate.Name = "UC-Universal Columns-Column:UC305x305x97:" + random.Next(10000);

                //represent column as a rectangular profile
                IfcArbitraryClosedProfileDef rectProf = IFCHelper.ArbitraryClosedProfileCreate(model, cadCol.RectColumn.ColPath.Vertices.ToList());




                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, rectProf, extrusionDir);



                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                colToCreate.Representation = prDefShape;



                trans.Commit();
                return colToCreate;
            }

        }

        //private IfcReinforcingBar CreateIfcRebar(IfcStore model, Rebar rebar, double height)
        //{
        //    //rebar.LocationPt.X *= 1000;
        //    //rebar.LocationPt.Y *= 1000;
        //    //rebar.LocationPt.Z *= 1000;
        //    //rebar.Diameter *= 1000;
        //    rebar.LocationPt.Z += (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000) + height;

        //    Point3D endPoint = new Point3D(rebar.LocationPt.X, rebar.LocationPt.Y, rebar.LocationPt.Z - (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000) - height);

        //    //begin a transaction
        //    //   using (var trans = model.BeginTransaction("Create column"))
        //    //   {
        //    IfcReinforcingBar rebarToCreate = model.Instances.New<IfcReinforcingBar>();
        //    rebarToCreate.Name = "UC-Universal Rebar" + 1700;

        //    //represent column as a rectangular profile
        //    IfcCompositeCurveSegment segment = IFCHelper.CreateCurveSegment(model, rebar.LocationPt, endPoint);
        //    List<IfcCompositeCurveSegment> segments = new List<IfcCompositeCurveSegment>
        //        {
        //            segment
        //        };
        //    IfcCompositeCurve compProf = IFCHelper.CreateCompositeProfile(model, segments);


        //    //model as a swept disk solid 

        //    IfcSweptDiskSolid body = IFCHelper.SweptDiskSolidCreate(model, compProf, rebar.Diameter / 2);


        //    //Create a Definition shape to hold the geometry
        //    IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
        //    shape.Items.Add(body);

        //    //Create a Product Definition and add the model geometry to the wall
        //    IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
        //    prDefShape.Representations.Add(shape);
        //    rebarToCreate.Representation = prDefShape;



        //    //    trans.Commit();
        //    return rebarToCreate;
        //    // }

        //}

        private IfcReinforcingBar CreateIfcRebar(IfcStore model, Rebar rebar, double height)
        {
            IfcReinforcingBar rebarToCreate = model.Instances.New<IfcReinforcingBar>();
            rebarToCreate.Name = "Rebar:UC305x305x97:" + random.Next(100000);

            //represent column as a rectangular profile
            IfcCircleProfileDef cirProf = IFCHelper.CircleProfileCreate(model, rebar.Diameter / 2);


            //Profile insertion point
            cirProf.ProfileInsertionPointSet(model, 0, 0);

            if (rebar.Type.ToLower() == "Vertical".ToLower())
            {

                height += (CADConfig.Units == linearUnitsType.Meters ? 1 : 1000);

                //model as a swept area solid
                IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
                extrusionDir.SetXYZ(0, 0, 1);

                IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, height, cirProf, extrusionDir);


                //parameters to insert the geometry in the model
                body.BodyPlacementSet(model, 0, 0, 0);

                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                rebarToCreate.Representation = prDefShape;

                //Create Local axes system and assign it to the column
                IfcCartesianPoint location3D = model.Instances.New<IfcCartesianPoint>();
                location3D.SetXYZ(rebar.LocationPt.X, rebar.LocationPt.Y, rebar.LocationPt.Z);

                IfcDirection localXDir = model.Instances.New<IfcDirection>();
                localXDir.SetXYZ(1, 0, 0);

                IfcDirection localZDir = model.Instances.New<IfcDirection>();
                localZDir.SetXYZ(0, 0, 1);

                IfcAxis2Placement3D ax3D = IFCHelper.LocalAxesSystemCreate(model, location3D, localXDir, localZDir);

                //now place the wall into the model
                IfcLocalPlacement lp = IFCHelper.LocalPlacemetCreate(model, ax3D);
                rebarToCreate.ObjectPlacement = lp;
            }

            else if (rebar.Type.ToLower() == "Horizontal".ToLower())
            {

                List<Point3D> lstVertices = new List<Point3D>();
                for (int i = 0; i < rebar.LinearPath.Vertices.Count(); i++)
                {
                    lstVertices.Add(rebar.LinearPath.Vertices[i]);
                }
                IfcSurfaceCurveSweptAreaSolid body;

                if (lstVertices.Count > 2)
                {
                    Vector3D uvPerpRebar = MathHelper.UVPerpendicularToLine2DFromPt(new Line(lstVertices[1], lstVertices[2]), lstVertices[1]);

                    IfcDirection planeZAxis = model.Instances.New<IfcDirection>();
                    planeZAxis.SetXYZ(uvPerpRebar.X, uvPerpRebar.Y, uvPerpRebar.Z);

                    Vector3D refDirUV = MathHelper.UnitVector3DFromPt1ToPt2(lstVertices[1], lstVertices[2]);

                    IfcDirection refDir = model.Instances.New<IfcDirection>();
                    refDir.SetXYZ(refDirUV.X, refDirUV.Y, refDirUV.Z);
                    body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, lstVertices, planeZAxis, refDir);
                }
                else
                    body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, lstVertices);


                //parameters to insert the geometry in the model

                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
                prDefShape.Representations.Add(shape);
                rebarToCreate.Representation = prDefShape;
            }



            return rebarToCreate;
            // }

        }

        //private IfcReinforcingBar CreateIfcStirrup(IfcStore model, Stirrup stirrup, double zPosition)
        //{
        //    List<Line> lstBranchesNew = new List<Line>();
        //    List<IfcCompositeCurveSegment> lstSegment = new List<IfcCompositeCurveSegment>();
        //    IfcCompositeCurveSegment segment = null;
        //    for (int i = 0; i < stirrup.LstBranch.Count; i++)
        //    {


        //        stirrup.LstBranch[i].StartPoint.Z += zPosition;
        //        stirrup.LstBranch[i].EndPoint.Z += zPosition;


        //        segment = IFCHelper.CreateCurveSegment(model, stirrup.LstBranch[i].StartPoint, stirrup.LstBranch[i].EndPoint);
        //        lstSegment.Add(segment);
        //    }
        //    //stirrup.Diameter *= 1000;





        //    //Point3D endPoint = new Point3D(stirrup.LocationPt.X, stirrup.LocationPt.Y, stirrup.LocationPt.Z - 1000 - height);

        //    //begin a transaction
        //    //   using (var trans = model.BeginTransaction("Create column"))
        //    //   {
        //    IfcReinforcingBar stirrupToCreate = model.Instances.New<IfcReinforcingBar>();
        //    stirrupToCreate.Name = "UC-Universal Rebar" + 1700;

        //    //represent column as a rectangular profile

        //    IfcCompositeCurve compProf = IFCHelper.CreateCompositeProfile(model, lstSegment);


        //    //model as a swept disk solid 

        //    IfcSweptDiskSolid body = IFCHelper.SweptDiskSolidCreate(model, compProf, stirrup.Diameter / 2);


        //    //Create a Definition shape to hold the geometry
        //    IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "AdvancedSweptSolid", "Body");
        //    shape.Items.Add(body);

        //    //Create a Product Definition and add the model geometry to the wall
        //    IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
        //    prDefShape.Representations.Add(shape);
        //    stirrupToCreate.Representation = prDefShape;



        //    //    trans.Commit();
        //    return stirrupToCreate;
        //    // }

        //}

        private IfcReinforcingBar CreateIfcStirrup(IfcStore model, Stirrup stirrup, double zPosition)
        {
            List<Point3D> lstPt = new List<Point3D>();

            for (int i = 0; i < stirrup.LstBranch.Count; i++)
            {


                stirrup.LstBranch[i].StartPoint.Z += zPosition;
                stirrup.LstBranch[i].EndPoint.Z += zPosition;

                lstPt.Add(stirrup.LstBranch[i].StartPoint);
                lstPt.Add(stirrup.LstBranch[i].EndPoint);
            }

            lstPt = lstPt.Distinct().ToList();
            lstPt.Add(lstPt[0]);
            IfcReinforcingBar stirrupToCreate = model.Instances.New<IfcReinforcingBar>();
            stirrupToCreate.Name = "Rebar:UC305x305x97:" + random.Next(100000);

            IfcCircleProfileDef cirProf = IFCHelper.CircleProfileCreate(model, stirrup.Diameter / 2);
            //Profile insertion point
            cirProf.ProfileInsertionPointSet(model, 0, 0);

            IfcSurfaceCurveSweptAreaSolid body = IFCHelper.ProfileSurfaceSweptSolidCreate(model, cirProf, lstPt);
            //parameters to insert the geometry in the model

            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            stirrupToCreate.Representation = prDefShape;


            return stirrupToCreate;

        }
        private IfcCableCarrierSegment CreateIfcConduit(IfcStore model, ElectricalConduit conduit)
        {
            IfcCableCarrierSegment conduitToCreate = model.Instances.New<IfcCableCarrierSegment>();
            conduitToCreate.Name = "Conduit:UC305x305x97:" + random.Next(100000);

            //represent column as a rectangular profile
            IfcCircleProfileDef cirProf = IFCHelper.CircleProfileCreate(model, conduit.Diameter / 2);
            //Profile insertion point
            cirProf.ProfileInsertionPointSet(model, 0, 0);


            IfcSurfaceCurveSweptAreaSolid body = IFCHelper.ProfileSurfaceSweptSolidCreateByCompositeCurve(model, cirProf,conduit.CurvePath);

            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            conduitToCreate.Representation = prDefShape;

            return conduitToCreate;
            // }

        }

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
            var Time = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                });
            });
            var Sound = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Sound";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.POWERUNIT;
                    siu.Name = IfcSIUnitName.COULOMB;
                });
            });
            var Material = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "Material";
                psv.Description = "";
                psv.NominalValue = new IfcLabel("Concrete");
            });

            //lets create the IfcElementQuantity
            var ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.Name = "Element Properties";
                ps.Description = "Property Set";
                ps.HasProperties.Add(Time);
                ps.HasProperties.Add(Sound);
                ps.HasProperties.Add(Material);
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
        #region formwork
        private IfcBuildingElementPart CreateFormWork(IfcStore model, LinearPath linPathElem, double formWorkThickness, double extrusionHeight
            , out IfcOpeningElement open, bool isSlabOrBeam = false)
        {
            LinearPath outerLinPath = (LinearPath)linPathElem.Offset(formWorkThickness);


            IfcBuildingElementPart formWork = model.Instances.New<IfcBuildingElementPart>();
            formWork.Name = " Foundation - Footing:UC305x305x97: ";

            //represent footing as a rectangular profile
            IfcArbitraryClosedProfileDef outerRectProfile = IFCHelper.ArbitraryClosedProfileCreate(model, outerLinPath.Vertices.ToList());



            //model as a swept area solid
            IfcDirection extrusionDir = model.Instances.New<IfcDirection>();
            extrusionDir.SetXYZ(0, 0, isSlabOrBeam ? -1 : 1);

            IfcExtrudedAreaSolid body = IFCHelper.ProfileSweptSolidCreate(model, isSlabOrBeam ? extrusionHeight + formWorkThickness : extrusionHeight, outerRectProfile, extrusionDir);



            //Create a Definition shape to hold the geometry
            IfcShapeRepresentation shape = IFCHelper.ShapeRepresentationCreate(model, "SweptSolid", "Body");
            shape.Items.Add(body);

            //Create a Product Definition and add the model geometry to the wall
            IfcProductDefinitionShape prDefShape = model.Instances.New<IfcProductDefinitionShape>();
            prDefShape.Representations.Add(shape);
            formWork.Representation = prDefShape;





            open = CreateIfcOpening(model, linPathElem, extrusionHeight, isSlabOrBeam);
            IfcRelVoidsElement relVoids = model.Instances.New<IfcRelVoidsElement>();

            relVoids.RelatedOpeningElement = open;
            relVoids.RelatingBuildingElement = formWork;

            return formWork;
        }

        #endregion
    }
}
