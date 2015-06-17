﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apm
{
    class Portfolio
    {
        // Key - Stock ID
        // Value - %
        public List<PfStock> collection;

        public DateTime Hold { get; set; }

        public int Time { get; set; }
    }

    class PfStock
    {
        public string IndustryTopLevel { get; set; }
        public string Industry2ndLevel { get; set; }
        public string Industry3rdLevel { get; set; }
        public string FirmFullName { get; set; }
        public string FirmShortName { get; set; }
        public string StockID { get; set; }
        public double Determination { get; set; }
        public double PriceEarningRate { get; set; }
        public double Turnover { get; set; }
        public double CirculatedMarketValue { get; set; }
        public double Liquidity { get; set; }
        public double LatestLongTermROE { get; set; }
        public double LatestClosePrice { get; set; }
        public double AveragePriceInPastWeek { get; set; }
        public double AveragePriceInPastMonth { get; set; }
        public double AveragePriceInPastThreeMonth { get; set; }
        public double AveragePriceInPastSixMonth { get; set; }
        public double AveragePriceInPastYear { get; set; }
    }

    class StockIndex
    {
        public string StockId { get; set; }

        // Arithmetic averaged = sum(daily) / count
        public double ExcessiveReturn1 { get; set; }

        // Weighted averaged = ((1+x) (1+y) .... - 1) / count
        public double ExcessiveReturn2 { get; set; }

        // ResidentialReturn = ExcessiveReturn - Beta * MarketExcessiveReturn
        public double ResidentialReturn1 { get; set; }

        public double ResidentialReturn2 { get; set; }

        // Risk = standard deviation
        public double ExcessiveReturnRisk { get; set; }

        public double ResidentialReturnRisk { get; set; }

        // Sharpe Ratio = Return / Risk;
        public double ExcessiveSharpeRatio { get; set; }

        public double ResidentialSharpeRatio { get; set; }

        public double PriceEarningRate { get; set; }

        public double Turnover { get; set; }

        public double CirculatedMarketValue { get; set; }

        public double Liquidity { get; set; }

        public double LatestClosePrice { get; set; }

        public double AveragePricePastWeek { get; set; }

        public double AveragePricePastMonth { get; set; }

        public double AveragePricePastThreeMonth { get; set; }

        public double AveragePricePastSixMonth { get; set; }

        public double AveragePricePastYear { get; set; }
    }
}
