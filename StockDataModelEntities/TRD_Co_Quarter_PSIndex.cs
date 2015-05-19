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
    
    public partial class TRD_Co_Quarter_PSIndex
    {
        public string StockID { get; set; }
        public string DateAsOfReporting { get; set; }
        public string ReportType { get; set; }
        public string IndustryID { get; set; }
        public Nullable<double> EPS1 { get; set; }
        public Nullable<double> EPSTTM1 { get; set; }
        public Nullable<double> EPS2 { get; set; }
        public Nullable<double> EPSTTM2 { get; set; }
        public Nullable<double> EPS3 { get; set; }
        public Nullable<double> EPSTTM3 { get; set; }
        public Nullable<double> EPS4 { get; set; }
        public Nullable<double> EPSTTM4 { get; set; }
        public Nullable<double> ConsolidatedEPS1 { get; set; }
        public Nullable<double> ConsolidatedEPSTTM1 { get; set; }
        public Nullable<double> ConsolidatedEPS2 { get; set; }
        public Nullable<double> ConsolidatedEPSTTM2 { get; set; }
        public Nullable<double> EPSAttrToCompany { get; set; }
        public Nullable<double> EPSTTMAttrToCompany { get; set; }
        public Nullable<double> ConsolidatedEPSAttrToCompany { get; set; }
        public Nullable<double> ConsolidatedEPSTTMAttrToCompany { get; set; }
        public Nullable<double> OpTotalRevenuePerShare { get; set; }
        public Nullable<double> OpTotalRevenueTTMPerShare { get; set; }
        public Nullable<double> OpRevenuePerShare { get; set; }
        public Nullable<double> OpRevenueTTMPerShare { get; set; }
        public Nullable<double> EPSBIT { get; set; }
        public Nullable<double> EPSTTMBIT { get; set; }
        public Nullable<double> EPSBITDA { get; set; }
        public Nullable<double> EPSTTMBITDA { get; set; }
        public Nullable<double> OpProfitPerShare { get; set; }
        public Nullable<double> OpProfitTTMPerShare { get; set; }
        public Nullable<double> BookValuePS { get; set; }
        public Nullable<double> NetTangibleAssetPS { get; set; }
        public Nullable<double> DebtPS { get; set; }
        public Nullable<double> AccumulatedCapitalPS { get; set; }
        public Nullable<double> AccumulatedEPS { get; set; }
        public Nullable<double> RetainedProfitPS { get; set; }
        public Nullable<double> RetainedEPS { get; set; }
        public Nullable<double> BVPSAttrToCompany { get; set; }
        public Nullable<double> NetOACashFlowPS { get; set; }
        public Nullable<double> NetOACashFlowTTMPS { get; set; }
        public Nullable<double> NetIACashFlowPS { get; set; }
        public Nullable<double> NetIACashFlowTTMPS { get; set; }
        public Nullable<double> NetFACashFlowPS { get; set; }
        public Nullable<double> NetFACashFlowTTMPS { get; set; }
        public Nullable<double> CorpFreeCashFlowPS { get; set; }
        public Nullable<double> CorpFreeCashFlowTTMPS { get; set; }
        public Nullable<double> StakeHolderFreeCashFlowPS { get; set; }
        public Nullable<double> StakeHolderFreeCashFlowTTMPS { get; set; }
        public Nullable<double> DAPS { get; set; }
        public Nullable<double> DATTMPS { get; set; }
        public Nullable<double> OrigCorpFreeCashFlowPS { get; set; }
        public Nullable<double> OrigEquityFreeCashFlowPS { get; set; }
        public Nullable<double> NetCashFlowPS1 { get; set; }
        public Nullable<double> NetCashFlowTTMPS1 { get; set; }
        public Nullable<double> NetCashFlowPS2 { get; set; }
        public Nullable<double> NetCashFlowTTMPS2 { get; set; }
        public System.Guid UniqueID { get; set; }
    
        public virtual TRD_Co TRD_Co { get; set; }
    }
}
