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
    
    public partial class STK_MKT_ThreeFactorModelDaily
    {
        public System.Guid UniqueID { get; set; }
        public string MarkettypeID { get; set; }
        public string TradingDate { get; set; }
        public Nullable<double> RiskPremium1 { get; set; }
        public Nullable<double> RiskPremium2 { get; set; }
        public Nullable<double> SMB1 { get; set; }
        public Nullable<double> SMB2 { get; set; }
        public Nullable<double> HML1 { get; set; }
        public Nullable<double> HML2 { get; set; }
    }
}
