using CADReader.Helpers;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CADReader.BuildingElements
{
    public class Building
    {
        #region Properties
        public List<FloorBase> Floors { get; set; }
        public string Name { get; set; }
        public CADConfig CadConfig { get; set; }
        public Point3D Location { get; set; }
        public double ZeroLevel { get; set; }
        #endregion

        #region Constructor
        public Building(string buildingName, Point3D _location)
        {
            CadConfig = new CADConfig();
            Floors = new List<FloorBase>();
            Name = buildingName;
            Location = _location;
        }

        public Building(string buildingName, Point3D _location, double _zeroLvl)
        {
            CadConfig = new CADConfig();
            Floors = new List<FloorBase>();
            Name = buildingName;
            Location = _location;
            ZeroLevel = _zeroLvl;
        }
        #endregion

        #region Public Functions
        public void AddNewFloor(string filePath, double level)
        {
            CadConfig.CadReader = new ReadAutodesk(filePath);
            CADConfig.Units = CadConfig.CadReader.Units;

            Floor floor = new Floor(CadConfig.CadReader, level);
            Floors.Add(floor);
        }

        public void AddNewFloor(string filePath, double level, double height)
        {
            CadConfig.CadReader = new ReadAutodesk(filePath);
            CADConfig.Units = CadConfig.CadReader.Units;

            FloorBase floor;

            if (filePath.ToLower().Contains("basement"))
                floor = new BasementFloor(CadConfig.CadReader, level, height);
            else
                floor = new Floor(CadConfig.CadReader, level, height);

            Floors.Add(floor);
        }

        public void AddBuildingFoundation(string filePath, double level)
        {
            CadConfig.CadReader = new ReadAutodesk(filePath);
            CADConfig.Units = CadConfig.CadReader.Units;

            Foundation foundation = new Foundation(CadConfig.CadReader, level);
            Floors.Add(foundation);
        }

        public void AddBuildingFoundation(string filePath, double level, double height)
        {
            CadConfig.CadReader = new ReadAutodesk(filePath);
            CADConfig.Units = CadConfig.CadReader.Units;

            Foundation foundation = new Foundation(CadConfig.CadReader, height, level);
            Floors.Add(foundation);
        }
        #endregion


    }
}
