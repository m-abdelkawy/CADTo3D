using CADReader.Helpers;
using devDept.Eyeshot.Translators;
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
        public List<IFloor> Floors { get; set; }
        public string Name { get; set; }
        public CADConfig CadConfig { get; set; }

        #endregion

        #region Constructor
        public Building(string buildingName)
        {
            CadConfig = new CADConfig();
            Floors = new List<IFloor>();
            Name = buildingName;
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
        public void AddBuildingFoundation(string filePath, double level)
        {
            CadConfig.CadReader = new ReadAutodesk(filePath);
            CADConfig.Units = CadConfig.CadReader.Units;

            Foundation foundation = new Foundation(CadConfig.CadReader, level);
            Floors.Add(foundation);
        }
        #endregion


    }
}
