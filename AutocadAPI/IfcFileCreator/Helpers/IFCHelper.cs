using CADReader.Reinforced_Elements;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace IfcFileCreator.Helpers
{
    public static class IFCHelper
    {
        #region Geometry
        internal static IfcRectangleProfileDef RectProfileCreate(IfcStore model, double length, double width)
        {
            IfcRectangleProfileDef rectProf = model.Instances.New<IfcRectangleProfileDef>();
            rectProf.ProfileType = IfcProfileTypeEnum.AREA;
            rectProf.XDim = length;
            rectProf.YDim = width;

            return rectProf;
        }
        internal static IfcCircleProfileDef CircleProfileCreate(IfcStore model, double radius)
        {
            IfcCircleProfileDef cirProf = model.Instances.New<IfcCircleProfileDef>();
            cirProf.ProfileType = IfcProfileTypeEnum.AREA;
            cirProf.Radius = radius;

            return cirProf;
        }

        internal static IfcArbitraryClosedProfileDef ArbitraryClosedProfileCreate(IfcStore model, List<Point3D> lstPoints)
        {
            IfcPolyline pLine = model.Instances.New<IfcPolyline>();
            for (int i = 0; i < lstPoints.Count; i++)
            {
                IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                point.SetXYZ(lstPoints[i].X, lstPoints[i].Y, lstPoints[i].Z);
                pLine.Points.Add(point);

            }
            IfcArbitraryClosedProfileDef profile = model.Instances.New<IfcArbitraryClosedProfileDef>();
            profile.ProfileType = IfcProfileTypeEnum.AREA;
            profile.OuterCurve = pLine;

            return profile;
        }

        public static void ProfileInsertionPointSet(this IfcParameterizedProfileDef prof, IfcStore model, IfcCartesianPoint insertionPt)
        {
            IfcCartesianPoint insertionPoint = model.Instances.New<IfcCartesianPoint>();
            insertionPoint.SetXYZ(insertionPt.X, insertionPt.Y, insertionPt.Z);
            prof.Position = model.Instances.New<IfcAxis2Placement2D>();
            prof.Position.Location = insertionPoint;
        }

        internal static IfcExtrudedAreaSolid ProfileSweptSolidCreate(IfcStore model, double extrusionDepth, IfcProfileDef prof, IfcDirection extrusionDirection)
        {
            IfcExtrudedAreaSolid body = model.Instances.New<IfcExtrudedAreaSolid>();
            body.Depth = extrusionDepth;
            body.SweptArea = prof;
            body.ExtrudedDirection = extrusionDirection;

            return body;
        }

        internal static IfcSurfaceCurveSweptAreaSolid ProfileSurfaceSweptSolidCreate(IfcStore model, IfcProfileDef prof, List<Point3D> lstPoints, IfcDirection planeZaxis = null, IfcDirection refDir = null)
        {
            IfcSurfaceCurveSweptAreaSolid body = model.Instances.New<IfcSurfaceCurveSweptAreaSolid>();
            IfcPolyline pLine = model.Instances.New<IfcPolyline>();
            for (int i = 0; i < lstPoints.Count; i++)
            {
                IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                point.SetXYZ(lstPoints[i].X, lstPoints[i].Y, lstPoints[i].Z);
                pLine.Points.Add(point);

            }

            body.Directrix = pLine;
            body.SweptArea = prof;

            var plane = model.Instances.New<IfcPlane>();
            plane.Position = model.Instances.New<IfcAxis2Placement3D>();
            plane.Position.Location = model.Instances.New<IfcCartesianPoint>();
            plane.Position.Location.SetXYZ(lstPoints[0].X, lstPoints[0].Y, lstPoints[0].Z);

            plane.Position.Axis = planeZaxis;
            plane.Position.RefDirection = refDir;

            body.ReferenceSurface = plane;
            //body.FixedReference.SetXYZ(1, 0, 0);
            return body;
        }

        internal static IfcSurfaceCurveSweptAreaSolid ProfileSurfaceSweptSolidCreate(IfcStore model, IfcProfileDef prof, List<Point3D> lstPoints)
        {
            IfcSurfaceCurveSweptAreaSolid body = model.Instances.New<IfcSurfaceCurveSweptAreaSolid>();
            IfcPolyline pLine = model.Instances.New<IfcPolyline>();
            for (int i = 0; i < lstPoints.Count; i++)
            {
                IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                point.SetXYZ(lstPoints[i].X, lstPoints[i].Y, lstPoints[i].Z);
                pLine.Points.Add(point);

            }

            body.Directrix = pLine;
            body.SweptArea = prof;
            var plane = model.Instances.New<IfcPlane>();
            plane.Position = model.Instances.New<IfcAxis2Placement3D>();
            plane.Position.Location = model.Instances.New<IfcCartesianPoint>();
            plane.Position.Location.SetXYZ(lstPoints[0].X, lstPoints[0].Y, lstPoints[0].Z);

            plane.Position.Axis = model.Instances.New<IfcDirection>();
            plane.Position.Axis.SetXYZ(0, 0, 1);
            plane.Position.RefDirection = model.Instances.New<IfcDirection>();
            plane.Position.RefDirection.SetXYZ(1, 0, 0);
            body.ReferenceSurface = plane;
            //body.FixedReference.SetXYZ(1, 0, 0);
            return body;
        }

        internal static IfcSurfaceCurveSweptAreaSolid ProfileSurfaceSweptSolidCreateByCompositeCurve(IfcStore model, IfcProfileDef prof, Entity profPath)
        {
            IfcSurfaceCurveSweptAreaSolid body = model.Instances.New<IfcSurfaceCurveSweptAreaSolid>();
            IfcCompositeCurve compositeCurve = model.Instances.New<IfcCompositeCurve>();
            if (profPath is LinearPath)
            {
                LinearPath linearPath = profPath as LinearPath;
                IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
                IfcPolyline pLine = model.Instances.New<IfcPolyline>();
                for (int i = 0; i < linearPath.Vertices.Length; i++)
                {
                    IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                    point.SetXYZ(linearPath.Vertices[i].X, linearPath.Vertices[i].Y, linearPath.Vertices[i].Z);
                    pLine.Points.Add(point);
                }
                segment.ParentCurve = pLine;
                segment.Transition = IfcTransitionCode.CONTINUOUS;
                compositeCurve.Segments.Add(segment);
            }
            else
            {
                CompositeCurve compCurvePath = profPath as CompositeCurve;
                for (int i = 0; i < compCurvePath.CurveList.Count; i++)
                {
                    if (compCurvePath.CurveList[i] is Line)
                    {
                        Line line = compCurvePath.CurveList[i] as Line;
                        IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
                        IfcPolyline pLine = model.Instances.New<IfcPolyline>();
                        for (int j = 0; j < line.Vertices.Length; j++)
                        {
                            IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                            point.SetXYZ(line.Vertices[j].X, line.Vertices[j].Y, line.Vertices[j].Z);
                            pLine.Points.Add(point);
                        }
                        segment.ParentCurve = pLine;
                        segment.Transition = IfcTransitionCode.CONTINUOUS;
                        compositeCurve.Segments.Add(segment);
                    }
                    else
                    {
                        Arc arc = compCurvePath.CurveList[i] as Arc;
                        IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
                        IfcTrimmedCurve trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
                        IfcCircle cir = model.Instances.New<IfcCircle>(e => e.Radius = arc.Radius);
                        cir.Position = model.Instances.New<IfcAxis2Placement3D>(e => e.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.Center.X, arc.Center.Y, arc.Center.Z)));

                        trimmedCurve.BasisCurve = cir;
                        trimmedCurve.Trim1.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.StartPoint.X, arc.StartPoint.Y, arc.StartPoint.Z)));
                        trimmedCurve.Trim2.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.EndPoint.X, arc.EndPoint.Y, arc.EndPoint.Z)));
                        trimmedCurve.SenseAgreement = arc.Plane.AxisZ == Vector3D.AxisZ ? false : true;
                        trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
                        segment.ParentCurve = trimmedCurve;
                        segment.Transition = IfcTransitionCode.CONTINUOUS;
                        compositeCurve.Segments.Add(segment);

                    }
                }
            }



            body.Directrix = compositeCurve;
            body.SweptArea = prof;
            var plane = model.Instances.New<IfcPlane>();
            plane.Position = model.Instances.New<IfcAxis2Placement3D>();
            plane.Position.Location = model.Instances.New<IfcCartesianPoint>();
            //plane.Position.Location.SetXYZ(profPath.X, lstPoints[0].Y, lstPoints[0].Z);

            plane.Position.Axis = model.Instances.New<IfcDirection>();
            plane.Position.Axis.SetXYZ(0, 0, 1);
            plane.Position.RefDirection = model.Instances.New<IfcDirection>();
            plane.Position.RefDirection.SetXYZ(1, 0, 0);
            body.ReferenceSurface = plane;
            //body.FixedReference.SetXYZ(1, 0, 0);
            return body;
        }
        internal static IfcSweptDiskSolid ProfileSweptDiskSolidByCompositeCurve(IfcStore model, Entity profPath, double raduis)
        {
            IfcSweptDiskSolid body = model.Instances.New<IfcSweptDiskSolid>();
            IfcCompositeCurve compositeCurve = model.Instances.New<IfcCompositeCurve>();
            compositeCurve.SelfIntersect = false;
            if (profPath is LinearPath)
            {
                LinearPath linearPath = profPath as LinearPath;
                IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
                IfcPolyline pLine = model.Instances.New<IfcPolyline>();
                for (int i = 0; i < linearPath.Vertices.Length; i++)
                {
                    IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                    point.SetXYZ(linearPath.Vertices[i].X, linearPath.Vertices[i].Y, linearPath.Vertices[i].Z);
                    pLine.Points.Add(point);
                }
                segment.ParentCurve = pLine;
                segment.Transition = IfcTransitionCode.CONTINUOUS;
                compositeCurve.Segments.Add(segment);
            }
            else
            {
                CompositeCurve compCurvePath = profPath as CompositeCurve;
                for (int i = 0; i < compCurvePath.CurveList.Count; i++)
                {
                    IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
                    //segment.Transition = i == compCurvePath.CurveList.Count - 1 ? IfcTransitionCode.DISCONTINUOUS : IfcTransitionCode.CONTINUOUS;
                    segment.Transition = IfcTransitionCode.DISCONTINUOUS;
                    if (compCurvePath.CurveList[i] is Line)
                    {
                        segment.SameSense = true;
                        Line line = compCurvePath.CurveList[i] as Line;
                        IfcPolyline pLine = model.Instances.New<IfcPolyline>();
                        for (int j = 0; j < line.Vertices.Length; j++)
                        {
                            IfcCartesianPoint point = model.Instances.New<IfcCartesianPoint>();
                            point.SetXYZ(line.Vertices[j].X, line.Vertices[j].Y, line.Vertices[j].Z);
                            pLine.Points.Add(point);
                        }
                        segment.ParentCurve = pLine;
                        compositeCurve.Segments.Add(segment);
                    }
                    else
                    {
                        Arc arc = compCurvePath.CurveList[i] as Arc;
                        IfcTrimmedCurve trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
                        IfcCircle cir = model.Instances.New<IfcCircle>(e => e.Radius = arc.Radius);
                        cir.Position = model.Instances.New<IfcAxis2Placement3D>(e =>
                        e.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.Center.X, arc.Center.Y, arc.Center.Z)));

                        trimmedCurve.BasisCurve = cir;
                        trimmedCurve.Trim1.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.StartPoint.X, arc.StartPoint.Y, arc.StartPoint.Z)));
                        trimmedCurve.Trim2.Add(model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(arc.EndPoint.X, arc.EndPoint.Y, arc.EndPoint.Z)));
                        trimmedCurve.SenseAgreement = arc.Plane.AxisZ == Vector3D.AxisZ ? true : false;
                        //segment.SameSense = arc.Plane.AxisZ == Vector3D.AxisZ ? false : true;
                        trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
                        segment.ParentCurve = trimmedCurve;
                        compositeCurve.Segments.Add(segment);

                    }
                }
            }



            body.Directrix = compositeCurve;
            body.Radius = raduis;
            body.InnerRadius = raduis * .75;
            return body;
        }

        internal static void BodyPlacementSet(this IfcExtrudedAreaSolid areaSolidBody, IfcStore model, double x, double y, double z = 0, Vector3D uv = null)
        {
            areaSolidBody.Position = model.Instances.New<IfcAxis2Placement3D>();
            IfcCartesianPoint location = model.Instances.New<IfcCartesianPoint>();
            location.SetXYZ(x, y, z);
            areaSolidBody.Position.Location = location;


        }

        internal static void BodyPlacementSet(this IfcExtrudedAreaSolid areaSolidBody, IfcStore model, IfcCartesianPoint insertionPt)
        {
            areaSolidBody.Position = model.Instances.New<IfcAxis2Placement3D>();
            IfcCartesianPoint location = model.Instances.New<IfcCartesianPoint>();
            location.SetXYZ(insertionPt.X, insertionPt.Y, insertionPt.Z);
            areaSolidBody.Position.Location = location;
        }

        internal static IfcShapeRepresentation ShapeRepresentationCreate(IfcStore model, string repType, string repId)
        {
            IfcShapeRepresentation shape = model.Instances.New<IfcShapeRepresentation>();
            IfcGeometricRepresentationContext modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            shape.ContextOfItems = modelContext;
            shape.RepresentationType = repType;
            shape.RepresentationIdentifier = repId;

            return shape;
        }

        internal static IfcAxis2Placement3D LocalAxesSystemCreate(IfcStore model, IfcCartesianPoint locationPt, IfcDirection localXDir, IfcDirection localZDir)
        {
            IfcAxis2Placement3D ax3D = model.Instances.New<IfcAxis2Placement3D>();
            ax3D.Location = locationPt;
            ax3D.RefDirection = localXDir;
            ax3D.Axis = localZDir;

            return ax3D;
        }
        internal static IfcLocalPlacement LocalPlacemetCreate(IfcStore model, IfcAxis2Placement3D _relativePlacement, IfcObjectPlacement _placementRelTo = null)
        {
            IfcLocalPlacement localPlacement = model.Instances.New<IfcLocalPlacement>();
            localPlacement.RelativePlacement = _relativePlacement;
            if (_placementRelTo != null)
                localPlacement.PlacementRelTo = _placementRelTo;
            return localPlacement;
        }

        internal static void AttchOpening(this IfcElement element, IfcStore model, IfcOpeningElement opening)
        {
            IfcRelVoidsElement relVoids = model.Instances.New<IfcRelVoidsElement>();
            relVoids.RelatedOpeningElement = opening;
            relVoids.RelatingBuildingElement = element;
        }

        #region For Rebar By SweptDiskSolid
        internal static IfcCompositeCurveSegment CreateCurveSegment(IfcStore model, Point3D p1, Point3D p2)
        {
            // Create PolyLine for rebar
            IfcPolyline pL = model.Instances.New<IfcPolyline>();
            var startPoint = model.Instances.New<IfcCartesianPoint>();
            startPoint.SetXYZ(p1.X, p1.Y, p1.Z);
            var EndPoint = model.Instances.New<IfcCartesianPoint>();
            EndPoint.SetXYZ(p2.X, p2.Y, p2.Z);
            pL.Points.Add(startPoint);
            pL.Points.Add(EndPoint);

            IfcCompositeCurveSegment segment = model.Instances.New<IfcCompositeCurveSegment>();
            segment.Transition = IfcTransitionCode.CONTINUOUS;
            segment.ParentCurve = pL;
            segment.SameSense = true;
            return segment;

        }

        internal static IfcCompositeCurve CreateCompositeProfile(IfcStore model, List<IfcCompositeCurveSegment> segments)
        {
            IfcCompositeCurve compositProf = model.Instances.New<IfcCompositeCurve>();
            compositProf.Segments.AddRange(segments);
            compositProf.SelfIntersect = false;

            return compositProf;
        }

        internal static IfcSweptDiskSolid SweptDiskSolidCreate(IfcStore model, IfcCurve prof, double radius)
        {
            IfcSweptDiskSolid body = model.Instances.New<IfcSweptDiskSolid>();
            body.Directrix = prof;
            body.Radius = radius;
            return body;
        }
        #endregion
        #endregion

        #region to be named
        internal static IfcBuildingStorey CreateStorey(IfcStore model,IfcBuilding building)
        {
            IfcBuildingStorey storey;
            using (var trans = model.BeginTransaction("Add Storey"))
            {
                storey = model.Instances.New<IfcBuildingStorey>();
                IfcRelAggregates rel = model.Instances.New<IfcRelAggregates>();
                rel.RelatingObject = building;
                rel.RelatedObjects.Add(storey);

                trans.Commit();
            }

            return storey;
        }

        
        #endregion
    }
}
