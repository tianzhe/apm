//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace StockDataModelEntities
{
    using System;
    using System.Collections.Generic;
    
    public partial class TRD_Co_Quarter_RiskIndex
    {
        public string StockID { get; set; }
        public string DateAsOfReporting { get; set; }
        public string TypeOfReport { get; set; }
        public string IndustryID { get; set; }
        public Nullable<double> DegreeFinanceLeverage { get; set; }
        public Nullable<double> DegreeOperatingLeverage { get; set; }
        public Nullable<double> DegreeTotalLeverage { get; set; }
        public System.Guid UniqueID { get; set; }
    }
}
