using System;

namespace ReadsFilesTransform.Models
{
    public class Properties
    {
        public int Id { get; set; }
        public string PropertyID { get; set; }
        public string ConsumerID { get; set; }
        public string DeviceName { get; set; }
        public string NationalNo { get; set; }
        public int DeviceXCoordinate { get; set; }
        public int DeviceYCoordinate { get; set; }
        public string ConsumptionNo { get; set; }
        public int? HydrologicNo { get; set; }
        public string hydrologicCell { get; set; }
        public int AdminZoneID { get; set; }
        public int WaterGroupID { get; set; }
        public int ConsumptionZoneID { get; set; }
        public int SpecialZoneID { get; set; }
        public int SupervisorFolderID { get; set; }
        public int SupervisorID { get; set; }
        public string SupervisionAlias { get; set; }
        public int PropertyCount { get; set; }
        public DateTime RowVer { get; set; }
        public int UsageTypeID { get; set; }
       
    }
}
