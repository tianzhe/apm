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
    
    public partial class STK_MKT_SectorDaily
    {
        public string SectorTypeId { get; set; }
        public Nullable<double> CirculatedMarketValueWeightedReturnRateReinvest { get; set; }
        public Nullable<double> CirculatedMarketValueWeightedReturnRateNonReinvest { get; set; }
        public Nullable<System.DateTime> TradeDate { get; set; }
        public System.Guid UniqueId { get; set; }
    }
}
