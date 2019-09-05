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
        #endregion
        #region Constructor
        public Building(string buildingName)
        {
            Floors = new List<IFloor>();
            Name = buildingName;
        }
        #endregion

        #region Public Functions
        public void AddNewFloor(string filePath)
        {
            Floor floor = new Floor(filePath);
            Floors.Add(floor);  
        }
        public void AddBuildingFoundation(string filePath)
        {
            Foundation foundation = new Foundation(filePath);
            Floors.Add(foundation);
        } 
        #endregion


    }
}
