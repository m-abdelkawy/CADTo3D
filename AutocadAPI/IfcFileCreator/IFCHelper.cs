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
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace IfcFileCreator
{
    public static class IFCHelper
    {
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

        internal static void ProfileInsertionPointSet(this IfcRectangleProfileDef rectProf, IfcStore model, double x, double y)
        {
            IfcCartesianPoint insertionPoint = model.Instances.New<IfcCartesianPoint>();
            insertionPoint.SetXY(x, y);
            rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
            rectProf.Position.Location = insertionPoint;
        }
        internal static void ProfileInsertionPointSet(this IfcCircleProfileDef cirProf, IfcStore model, double x, double y)
        {
            IfcCartesianPoint insertionPoint = model.Instances.New<IfcCartesianPoint>();
            insertionPoint.SetXY(x, y);
            cirProf.Position = model.Instances.New<IfcAxis2Placement2D>();
            cirProf.Position.Location = insertionPoint;
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

        internal static void BodyPlacementSet(this IfcExtrudedAreaSolid areaSolidBody, IfcStore model, double x, double y, double z = 0, Vector3D uv = null)
        {
            areaSolidBody.Position = model.Instances.New<IfcAxis2Placement3D>();
            IfcCartesianPoint location = model.Instances.New<IfcCartesianPoint>();
            location.SetXYZ(x, y, z);
            areaSolidBody.Position.Location = location;

            if (uv != null)
            {
                IfcDirection dir = model.Instances.New<IfcDirection>();
                dir.SetXYZ(uv.X, uv.Y, uv.Z);
                areaSolidBody.Position.RefDirection = dir;
            }
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

        internal static void AttchOpening(this IfcSlab slab, IfcStore model, IfcOpeningElement opening)
        {
            IfcRelVoidsElement relVoids = model.Instances.New<IfcRelVoidsElement>();
            relVoids.RelatedOpeningElement = opening;
            relVoids.RelatingBuildingElement = slab;
        }

        #region For Rebar
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
    }
}
