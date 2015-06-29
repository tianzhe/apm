using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StockDataModelEntities;
using System.Configuration;
using System.IO;
using System.Collections;
using System.Threading;
using log4net;

namespace apm
{
    class CAPM : Model
    {
        private Dictionary<string, List<StockIndex>> pivot = new Dictionary<string, List<StockIndex>>();
        private Dictionary<DateTime, double?> _mktReturn = new Dictionary<DateTime,double?>();
        private Dictionary<Tuple<DateTime, string>, double?> _mktIndex = new Dictionary<Tuple<DateTime, string>, double?>();
        private Dictionary<Tuple<DateTime, string>, double?> _mktSector = new Dictionary<Tuple<DateTime, string>, double?>();
        private astockEntities _astock = new astockEntities();
        private int _mktType = 0;
        private DateTime _startDate = DateTime.Now;
        private DateTime _endDate = DateTime.Now;
        private string _industry = string.Empty;
        private string _avAlgo = string.Empty;
        private string _pfAlgo = string.Empty;
        private string _boardType = string.Empty;
        private string _source = string.Empty;
        private int _porfolioNumOfStocks = 0;
        private int _numOfStocksKeptInPivot = 0;
        private bool _isFilterByBoard = false;
        private bool _isFilterBySymbol = false;
        private List<string> _interestedSymbols;
        private string _riskType = string.Empty;
        private static ILog _logger = LogManager.GetLogger("CAPM");

        private static Dictionary<int, List<int> > MarketTypeMap = new Dictionary<int, List<int> >()
            {
                // Market Type
                // 1 - Shanghai A stock market
                // 2 - Shanghai B stock market
                // 4 - Shenzhen A stock market
                // 8 - Shenzhen B stock market
                // 16 - Start-up stock market
                {5, new List<int>{1, 4} },
                {10, new List<int>{2, 8} },
                {15, new List<int>{1, 2, 4, 8}},
                {21, new List<int>{1, 4, 16}},
                {31, new List<int>{1, 2, 4, 8, 16} }
            };

        private static Dictionary<string, List<string>> BoardTypeToStockIdPatternMap = new Dictionary<string, List<string>>()
        {
            {"SZMBA", new List<string>{"000"} },
            {"SZMBB", new List<string>{"200"}},
            {"SHMBA", new List<string>{"6"} },
            {"SHMBB", new List<string>{"900"} },
            {"MSB", new List<string>{"002"} },
            {"GEMB", new List<string>{"300"} }

        };

        private static Dictionary<double, string> MarketTypeToIndexMap = new Dictionary<double, string>()
            {
                {1, "000002"}, // Shanghai A Stock Index
                {2, "000003"}, // Shanghai B Stock Index
                {4, "399107"}, // Consolidated Shenzhen A Stock Index
                {8, "399108"}, // Consolidated Shenzhen B Stock Index
                {16,"399006"} // GEM Board Stock Index
            };

        private static Dictionary<string, string> BoardTypeToIndexMap = new Dictionary<string, string>()
            {
                {"SZMBA", "399107"}, // Consolidated Shenzhen A Stock Index
                {"SZMBB", "399108"}, // Consolidated Shenzhen B Stock Index
                {"SHMBA", "000002"}, // Shanghai A Stock Index
                {"SHMBB", "000003"}, // Shanghai B Stock Index
                {"MSB", "399005"}, // Consolidated MSB Stock Index
                {"GEMB", "399006"},// Consolidated GEMB Stock Index
            };

        private static Dictionary<int, List<string>> MarketIndexMap = new Dictionary<int, List<string>>()
            {
                // Index Type
                // 000002 - Shanghai A stock index
                // 000003 - Shanghai B stock index
                // 399107 - Shenzhen A stock index
                // 399108 - Shenzhen B stock index
                // 399006 - GEM board stock index
                {5, new List<string>{"000002", "399107"} },
                {10, new List<string>{"000003", "399108"} },
                {15, new List<string>{"000002", "399107", "000003", "399108"} },
                {21, new List<string>{"000002", "399107", "399006"} },
                {31, new List<string>{"000002", "399107", "000003", "399108", "399006"} }
            };

        public CAPM()
        {
            Initialise();
        }

        override public Portfolio Generate()
        {
            SortStockOfSameIndustry(_industry, _mktType);

            // Generate Portfolio
            Portfolio p = new Portfolio();
            p.collection = new List<PfStock>();

            foreach (var n in pivot)
            {
                foreach (var v in n.Value)
                {
                    double determination = 0;

                    switch(_pfAlgo)
                    {
                        case "ExcessiveSharpeOptimal":
                            determination = v.ExcessiveSharpeRatio;
                            break;
                        case "ResidentialSharpeOptimal":
                            determination = v.ResidentialSharpeRatio;
                            break;
                        case "ExcessiveReturnOptimal":
                            determination = v.ExcessiveReturn2;
                            break;
                        case "ResidentialReturnOptimal":
                            determination = v.ResidentialReturn2;
                            break;
                        default:
                            throw new ApplicationException(string.Format("Unrecognized optimization algothrim {1}", _pfAlgo));
                    }

                    var company = _astock.TRD_Co
                        .Where(firm => firm.StockId.Equals(v.StockId, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                    var sector = _astock.TRD_Sector
                        .Where(a => a.SectorTypeId.Equals(company.SectorTypeId, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                    var profit = _astock.TRD_Co_Quarter_ProfitIndex
                        .Where(tmp => tmp.StockID.Equals(company.StockId, StringComparison.InvariantCultureIgnoreCase))
                        .OrderByDescending(tmp => tmp.DateAsOfReporting2).ToList();
 
                    var item = new PfStock
                    {
                        IndustryTopLevel = company.IndustryNameA,
                        Industry2ndLevel = company.IndustryNameB,
                        Industry3rdLevel = company.IndustryNameC,
                        IndustrySector = sector.SectorTypeName,
                        FirmFullName = company.CompanyNameCN,
                        FirmShortName = company.StockName,
                        StockID = company.StockId,
                        Determination = determination,
                        CirculatedMarketValue = v.CirculatedMarketValue,
                        Turnover = v.Turnover,
                        Liquidity = v.Liquidity,
                        PriceEarningRate = v.PriceEarningRate,
                        LatestLongTermROE = profit.Count == 0 ? 0 : (double)profit.ElementAt(0).LongTermROE,
                        LatestClosePrice = v.LatestClosePrice,
                        Volatility = v.Volatility,
                        NonSystemRisk = v.CirculatedMarketValueWeightedNonSysRisk,
                        AveragePriceInPastWeek = v.AveragePricePastWeek,
                        AveragePriceInPastMonth = v.AveragePricePastMonth,
                        AveragePriceInPastTwoMonth = v.AveragePricePastTwoMonth,
                        AveragePriceInPastThreeMonth = v.AveragePricePastThreeMonth,
                        AveragePriceInPastSixMonth = v.AveragePricePastSixMonth,
                        AveragePriceInPastYear = v.AveragePricePastYear
                    };
                    p.collection.Add(item);

                    STK_MKT_PortfolioBase pfBase = new STK_MKT_PortfolioBase
                    {
                        StockId = v.StockId,
                        CreationTime = DateTime.Now,
                        ExcessiveReturn1 = v.ExcessiveReturn1,
                        ExcessiveReturn2 = v.ExcessiveReturn2,
                        ResidentialReturn1 = v.ResidentialReturn1,
                        ResidentialReturn2 = v.ResidentialReturn2,
                        ExcessiveReturnRisk = v.ExcessiveReturnRisk,
                        ResidentialReturnRisk = v.ResidentialReturnRisk,
                        ExcessiveReturnSharpeRatio = v.ExcessiveSharpeRatio,
                        ResidentialReturnSharpeRatio = v.ResidentialSharpeRatio,
                        Determination = determination,
                        DeterminationAlgorithm = _pfAlgo,
                        AverageAlgorithm = _avAlgo,
                        UniqueId = Guid.NewGuid(),
                        AverageNonSysRiskToDate = v.CirculatedMarketValueWeightedNonSysRisk,
                        AverageVolatilityToDate = v.Volatility,
                        AveragePriceInPastWeek = v.AveragePricePastWeek,
                        AveragePriceInPastMonth = v.AveragePricePastMonth,
                        AveragePriceInPastTwoMonth = v.AveragePricePastTwoMonth,
                        AveragePriceInPastThreeMonth = v.AveragePricePastThreeMonth,
                        AveragePriceInPastSixMonth = v.AveragePricePastSixMonth,
                        AveragePriceInPastYear = v.AveragePricePastYear,
                        TRD_Co = company
                    };

                    _astock.STK_MKT_PortfolioBase.Add(pfBase);
                }
            }

            try
            {
                _astock.SaveChanges();
            }
            catch (Exception e)
            {
                _logger.ErrorFormat(
                    string.Format("Unexpected error: failed to write to DB table STK_MKT_PortfolioBase.\nMessage = {0}\nStack Trace : \n{1}",
                    e.Message,
                    e.StackTrace));

                if (e.InnerException != null)
                {
                    _logger.ErrorFormat(
                        string.Format("Inner exception : \nMessage = {0}\nStack Trace : \n{1}",
                        e.InnerException.Message,
                        e.InnerException.StackTrace));
                }

                //throw e;

                //TODO
                Thread.Sleep(20000);
                _astock.SaveChanges();
            }
           

            p.Hold = DateTime.Now;
            p.Time = 7;

            //List<string> stockCandidates = new List<string>();
            //foreach(var portfolio in p.collection)
            //{
            //    stockCandidates.Add(portfolio.StockID);
            //}

            //List<List<string>> porfolioCandidates = Util.Combination(
            //    stockCandidates.ToArray(), 
            //    stockCandidates.Count, _porfolioNumOfStocks);

            //List<string> finalDecision = null;

            //double anchor = 0;
            //foreach(var portfolio in porfolioCandidates)
            //{
            //    double capWeightedDetermination = 0;
            //    double totalCaptial = 0;
            //    foreach(var stock in portfolio)
            //    {
            //        double circulatedCapital = (double)_astock.STK_MKT_TradeDaily
            //            .Where(co => co.StockID.Equals(stock, StringComparison.InvariantCultureIgnoreCase)
            //                && DateTime.Parse(co.TradeDate) == _endDate).FirstOrDefault().SumCirculatedMarketValue;

            //        double determination = p.collection
            //            .Where(po => po.StockID.Equals(stock, StringComparison.InvariantCultureIgnoreCase))
            //            .FirstOrDefault().Determination;

            //        capWeightedDetermination += circulatedCapital * determination;
            //        totalCaptial += circulatedCapital;
            //    }

            //    capWeightedDetermination = capWeightedDetermination / totalCaptial;
            //    if(anchor < capWeightedDetermination)
            //    {
            //        anchor = capWeightedDetermination;
            //        finalDecision = portfolio;
            //    }
            //}

            // Print out
            FileStream handle;
            StreamWriter writer;
            string outputFilePath = ConfigurationManager.AppSettings["OutputFileFolderPath"];
            if (string.IsNullOrEmpty(outputFilePath))
            {
                new ApplicationException(string.Format("Configuration item 'OutputFileFolderPath' must be present"));
            }
            outputFilePath += "\\output-" + DateTime.Now.Year.ToString() 
                + '-' + DateTime.Now.Month.ToString() + '-' + DateTime.Now.Day + ".txt";

            using (handle = File.OpenWrite(outputFilePath))
            {
                using (writer = new StreamWriter(handle))
                {
                    foreach (var dic in p.collection.OrderByDescending(c => c.Determination).OrderBy(c => c.IndustryTopLevel))
                    {
                        writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6:F}%,{7:F},{8}%,{9:F},{10:F},{11:F}%,{12},{13:F}%,{14:F}%,{15:F},{16:F},{17:F},{18:F},{19:F},{20:F}",
                            dic.IndustryTopLevel, dic.Industry3rdLevel, dic.IndustrySector,
                            dic.StockID, dic.FirmShortName, dic.FirmFullName, 
                            dic.Determination, dic.CirculatedMarketValue / 1000000, dic.Turnover, 
                            dic.Liquidity * 100, dic.PriceEarningRate, dic.LatestLongTermROE * 100, 
                            dic.LatestClosePrice, dic.Volatility, dic.NonSystemRisk,
                            dic.AveragePriceInPastWeek, dic.AveragePriceInPastMonth, dic.AveragePriceInPastTwoMonth, 
                            dic.AveragePriceInPastThreeMonth, dic.AveragePriceInPastSixMonth, dic.AveragePriceInPastYear));
                    }

                    //foreach (var dic in finalDecision)
                    //{
                    //    var stock = p.collection.Where(po => po.StockID.Equals(dic, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    //    writer.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6:F}%,{7:F}%",
                    //        stock.IndustryTopLevel, stock.Industry2ndLevel,
                    //        stock.Industry3rdLevel, stock.StockID, stock.FirmShortName,
                    //        stock.FirmFullName, stock.Determination, stock.WeightInPercent));
                    //}
                }
            }

            return p;
        }

        private void SortStockOfSameIndustry(string industryCode, int mktType)
        {
            if (!MarketTypeMap.ContainsKey(mktType))
            {
                return;
            }

            List<string> startWith = new List<string>();
            if (_isFilterByBoard)
            {
               if(BoardTypeToStockIdPatternMap.ContainsKey(_boardType))
               {
                   foreach(var pattern in BoardTypeToStockIdPatternMap[_boardType])
                   {
                       startWith.Add(pattern);
                   }
               }
            }
            else if(_isFilterBySymbol)
            {
                foreach(var stock in _interestedSymbols)
                {
                    startWith.Add(stock);
                }
            }
            else 
            {
                startWith.Add(string.Empty);
            }

            switch(industryCode)
            {
                default:
                    throw new ApplicationException(string.Format("Unrecognized industry code {0}", industryCode));

                case "IndustryTopLevel":

                    foreach (var co in _astock.TRD_Co.Where(c => !string.IsNullOrEmpty(c.IndustryCodeA)).ToList())
                    {
                        if (MarketTypeMap[mktType].Any(c => c == co.MarketType) 
                            && startWith.Any(c => co.StockId.StartsWith(c,StringComparison.InvariantCultureIgnoreCase)))
                        {
                            StockIndex result = CalculatePremiumReturn(co.StockId, (double)co.MarketType);
                            if (pivot.ContainsKey(co.IndustryCodeA))
                            {
                                pivot[co.IndustryCodeA].Add(result);
                            }
                            else
                            {
                                pivot.Add(co.IndustryCodeA, new List<StockIndex>() { result });
                            }
                        }
                    }
                    break;

                case "Industry2ndLevel2001":

                    foreach (var co in _astock.TRD_Co.Where(c => !string.IsNullOrEmpty(c.IndustryCodeB)).ToList())
                    {
                        if (MarketTypeMap[mktType].Any(c => c == co.MarketType)
                            && startWith.Any(c => co.StockId.StartsWith(c, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            StockIndex result = CalculatePremiumReturn(co.StockId, (double)co.MarketType);
                            if (pivot.ContainsKey(co.IndustryCodeB))
                            {
                                pivot[co.IndustryCodeB].Add(result);
                            }
                            else
                            {
                                pivot.Add(co.IndustryCodeB, new List<StockIndex>() { result });
                            }
                        }
                    }
                    break;

                case "Industry2ndLevel2012":

                    foreach (var co in _astock.TRD_Co.Where(c => !string.IsNullOrEmpty(c.IndustryCodeC)).ToList())
                    {
                        if (MarketTypeMap[mktType].Any(c => c == co.MarketType)
                            && startWith.Any(c => co.StockId.StartsWith(c, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            StockIndex result = CalculatePremiumReturn(co.StockId, (double)co.MarketType);
                            if (pivot.ContainsKey(co.IndustryCodeC))
                            {
                                pivot[co.IndustryCodeC].Add(result);
                            }
                            else
                            {
                                pivot.Add(co.IndustryCodeC, new List<StockIndex>() { result });
                            }
                        }
                    }
                    break;

                case "IndustrySector":

                    foreach (var co in _astock.TRD_Co.Where(c => !string.IsNullOrEmpty(c.SectorTypeId)).ToList())
                    {
                        if (MarketTypeMap[mktType].Any(c => c == co.MarketType)
                            && startWith.Any(c => co.StockId.StartsWith(c, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            StockIndex result = CalculatePremiumReturn(co.StockId, (double)co.MarketType);
                            if (pivot.ContainsKey(co.SectorTypeId))
                            {
                                pivot[co.SectorTypeId].Add(result);
                            }
                            else
                            {
                                pivot.Add(co.SectorTypeId, new List<StockIndex>() { result });
                            }
                        }
                    }
                    break;
            }

            List<StockIndex> adjusted;
            int count = 0;
            foreach(var p in pivot)
            {
                switch(_pfAlgo)
                {
                    case "ResidentialSharpeOptimal":

                        p.Value.RemoveAll(c => c.ResidentialSharpeRatio <= 0);
                        adjusted = p.Value.OrderByDescending(c => c.ResidentialSharpeRatio).ToList();
                        p.Value.RemoveAll(c => c != null);
                        count = _numOfStocksKeptInPivot >= adjusted.Count ? adjusted.Count : _numOfStocksKeptInPivot;
                        for (int i = 0; i < count; ++i)
                        {
                            p.Value.Add(adjusted.ElementAt(i));
                        }
                       
                        break;

                    case "ExcessiveSharpeOptimal":
                        
                        p.Value.RemoveAll(c => c.ExcessiveSharpeRatio <= 0);
                        adjusted = p.Value.OrderByDescending(c => c.ExcessiveSharpeRatio).ToList();
                        p.Value.RemoveAll(c => c != null);
                        count = _numOfStocksKeptInPivot >= adjusted.Count ? adjusted.Count : _numOfStocksKeptInPivot;
                        for (int i = 0; i < count; ++i)
                        {
                            p.Value.Add(adjusted.ElementAt(i));
                        }

                        break;

                    case "ExcessiveReturnOptimal":
                        
                        p.Value.RemoveAll(c => c.ExcessiveReturn2 <= 0);
                        adjusted = p.Value.OrderByDescending(c => c.ExcessiveReturn2).ToList();
                        p.Value.RemoveAll(c => c != null);
                        count = _numOfStocksKeptInPivot >= adjusted.Count ? adjusted.Count : _numOfStocksKeptInPivot;
                        for (int i = 0; i < count; ++i)
                        {
                            p.Value.Add(adjusted.ElementAt(i));
                        }

                        break;

                    case "ResidentialReturnOptimal":
                        
                        p.Value.RemoveAll(c => c.ResidentialReturn2 <= 0);
                        adjusted = p.Value.OrderByDescending(c => c.ResidentialReturn2).ToList();
                        p.Value.RemoveAll(c => c != null);
                        count = _numOfStocksKeptInPivot >= adjusted.Count ? adjusted.Count : _numOfStocksKeptInPivot;
                        for (int i = 0; i < count; ++i)
                        {
                            p.Value.Add(adjusted.ElementAt(i));
                        }

                        break;
                }
            }
        }

        private StockIndex CalculatePremiumReturn(string stockId, double marketType)
        {
            var rawDailyBeta = _astock.STK_MKT_RiskFactorDaily
                .Where(co => co.Symbol.Equals(stockId, StringComparison.InvariantCultureIgnoreCase) && co.TradingDate2 >= _startDate && co.TradingDate2 <= _endDate)
                .Select(co => new{ co.Beta1, co.SectorBeta1, date = co.TradingDate2, co.NonSysRisk1, co.Volatility })
                .OrderByDescending(co => co.date).ToList();

            //var convertedDailyBeta = rawDailyBeta.
            //    Select(co => new { co.Beta1, date = DateTime.Parse(co.TradingDate) }).ToList();

            //var timeConstrainedDailyBeta = convertedDailyBeta
            //    .Where(co => co.date >= _startDate && co.date <= _endDate)
            //    .OrderBy(co => co.date).ToList();

            var rawDailyReturn = _astock.STK_MKT_TradeDaily
                .Where(co => co.StockID.Equals(stockId, StringComparison.InvariantCultureIgnoreCase) && co.TradeStatus == 1 && co.TradeDate2 >= _startDate && co.TradeDate2 <= _endDate)
                .Select(co => new { co.ReturnRateReinvest, date = co.TradeDate2, price = co.ClosePrice, highPirce = co.HighPrice, lowPrice = co.LowPrice })
                .OrderByDescending(co => co.date).ToList();

            //var convertedDailyReturn = rawDailyReturn
            //    .Select(co => new { co.ReturnRateReinvest, date = DateTime.Parse(co.TradeDate) })
            //    .ToList();

            //var timeConstrainedDailyReturn = convertedDailyReturn
            //    .Where(co => co.date >= _startDate && co.date <= _endDate)
            //    .OrderBy(co => co.date).ToList();

            var rawDerivativeDaily = _astock.STK_MKT_DeriativeTradingIndexDaily
                .Where(co => co.Symbol.Equals(stockId, StringComparison.InvariantCultureIgnoreCase) && co.TradingDate2 >= _startDate && co.TradingDate2 <= _endDate)
                .Select(co => new { date = co.TradingDate2, co.CirculatedMarketValue, co.PE, co.Turnover, co.Liquidility })
                .OrderByDescending(co => co.date).ToList();

            //var convertedDerivativeDaily = rawDerivativeDaily
            //    .Select(co => new { co.CirculatedMarketValue, co.PE, co.Turnover, co.Liquidility, date = DateTime.Parse(co.TradingDate) }).ToList();

            //var timeConstrainedDerivativeDaily = convertedDerivativeDaily
            //    .Where(co => co.date >= _startDate && co.date <= _endDate)
            //    .OrderBy(co => co.date).ToList();
            
            double excessive1 = 0;
            double excessive2 = 1;
            double excessive = 0;
            double residential = 0;
            double premium1 = 0;
            double premium2 = 1;
            double premium = 0;
            double excessiveRisk = 0;
            double residentialRisk = 0;
            double excessiveSharpe = 0;
            double residentialSharpe = 0;
            double pe = 0;
            double turnover = 0;
            double circulateMarketValue = 0;
            double liquidity = 0;
            double volatility = 0;
            double nonSysRisk = 0;

            DateTime? previousWeek = 
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddDays(-7);
            DateTime? previousMonth =
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddMonths(-1);
            DateTime? previousTwoMonth =
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddMonths(-2);
            DateTime? previousThreeMonth =
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddMonths(-3);
            DateTime? previousSixMonth =
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddMonths(-6);
            DateTime? previousYear =
                rawDailyReturn.Count == 0 ? (DateTime?)null : ((DateTime)rawDailyReturn.ElementAt(0).date).AddYears(-1);

            // 1. Get the low/high/average price for the past week
            var priceListInOneWeek =
                previousWeek == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousWeek).Date).ToList();

            double? lowestPriceInOneWeek =
                priceListInOneWeek == null ? 
                    0 : priceListInOneWeek.Min(a => a.lowPrice);

            double? highestPriceInOneWeek = 
                previousWeek == null ?
                    0 : priceListInOneWeek.Max(a => a.highPirce);

            double? averagePriceInOneWeek = ( lowestPriceInOneWeek + highestPriceInOneWeek ) / 2;

            // 2. Get the low/high/average price for the past month
            var priceListInOneMonth =
                previousMonth == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousMonth).Date).ToList();

            double? lowestPriceInOneMonth =
                priceListInOneMonth == null ?
                    0 : priceListInOneMonth.Min(a => a.lowPrice);

            double? highestPriceInOneMonth =
                priceListInOneMonth == null ?
                    0 : priceListInOneMonth.Max(a => a.highPirce);

            double? averagePriceInOneMonth = (lowestPriceInOneMonth + highestPriceInOneMonth) / 2;

            // 3. Get the low/high price for the past 2 months
            var priceListInTwoMonth =
                previousTwoMonth == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousTwoMonth).Date).ToList();

            double? lowestPriceInTwoMonth =
                priceListInTwoMonth == null ?
                    0 : priceListInTwoMonth.Min(a => a.lowPrice);

            double? highestPriceInTwoMonth =
                priceListInTwoMonth == null ?
                    0 : priceListInTwoMonth.Max(a => a.highPirce);

            double? averagePriceInTwoMonth = (lowestPriceInTwoMonth + lowestPriceInTwoMonth) / 2;

            // 4. Get the low/high/average price for the past 3W months
            var priceListInThreeMonth =
                previousThreeMonth == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousThreeMonth).Date).ToList();

            double? lowestPriceInThreeMonth =
                priceListInThreeMonth == null ?
                    0 : priceListInThreeMonth.Min(a => a.lowPrice);

            double? highestPriceInThreeMonth =
                priceListInThreeMonth == null ?
                    0 : priceListInThreeMonth.Max(a => a.highPirce);

            double? averagePriceInThreeMonth = (lowestPriceInThreeMonth + highestPriceInThreeMonth) / 2;

            // 5. Get the low/high/average price for the past 6 months
            var priceListInSixMonth =
                previousSixMonth == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousSixMonth).Date).ToList();

            double? lowestPriceInSixMonth =
                priceListInSixMonth == null ?
                    0 : priceListInSixMonth.Min(a => a.lowPrice);

            double? highestPriceInSixMonth =
                priceListInSixMonth == null ?
                    0 : priceListInSixMonth.Max(a => a.highPirce);

            double? averagePriceInSixMonth = (lowestPriceInSixMonth + highestPriceInSixMonth) / 2;

            // 6. Get the low/high/average price for the past year
            var priceListInOneYear =
                previousYear == null ?
                    null :
                    rawDailyReturn
                        .Where(a => ((DateTime)a.date).Date >= ((DateTime)previousYear).Date).ToList();

            double? lowestPriceInOneYear =
                priceListInOneYear == null ?
                    0 : priceListInOneYear.Min(a => a.lowPrice);

            double? highestPriceInOneYear =
                priceListInOneYear == null ?
                    0 : priceListInOneYear.Max(a => a.highPirce);

            double? averagePriceInOneYear = (lowestPriceInOneYear + highestPriceInOneYear) / 2;

            var count = 0;
            var riskSampleCount = 0;
            
            List<double> residentialArray = new List<double>();
            List<double> returnArray = new List<double>();

            foreach (var beta in rawDailyBeta)
            {
                double? market;

                if(_source.ToUpper().Equals("INDEX"))
                {
                    var key = _isFilterByBoard ? 
                        new Tuple<DateTime, string>((DateTime)beta.date, BoardTypeToIndexMap[_boardType]) :
                        new Tuple<DateTime, string>((DateTime)beta.date, MarketTypeToIndexMap[marketType]);

                    if(!_mktIndex.ContainsKey(key))
                        continue;

                    market = _mktIndex[key] / 100;
                }
                else if(_source.ToUpper().Equals("CONSOLIDATED"))
                {
                    if (!_mktReturn.ContainsKey((DateTime)beta.date))
                        continue;

                    market = _mktReturn[(DateTime)beta.date];
                }
                else if(_source.ToUpper().Equals("SECTOR"))
                {
                    var stock = 
                        _astock.TRD_Co
                        .Where(a => a.StockId.Equals(stockId, StringComparison.InvariantCultureIgnoreCase))
                        .FirstOrDefault();

                    if(stock != null && !string.IsNullOrEmpty(stock.SectorTypeId))
                    {
                        var sector = stock.SectorTypeId;
                        var key = new Tuple<DateTime, string>((DateTime)beta.date, sector);
                        if(!_mktSector.ContainsKey(key))
                        {
                            continue;
                        }
                        market = _mktSector[key];
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    throw new ApplicationException(
                        string.Format("Unrecognized return rate source <{0}>", _source.ToUpper()));
                }

                var stockReturn = rawDailyReturn.Where(co => co.date == beta.date).FirstOrDefault();
                
                if(stockReturn != null)
                {
                    var betaCor = _source.ToUpper().Equals("SECTOR") ? beta.SectorBeta1 : beta.Beta1;
                    if(stockReturn.ReturnRateReinvest != null && betaCor != null & market != null)
                    {
                        excessive1 += (double)stockReturn.ReturnRateReinvest;
                        excessive2 = excessive2 * (1 + (double)stockReturn.ReturnRateReinvest);
                        residential = (double)stockReturn.ReturnRateReinvest - (double)betaCor * (double)market;
                        residentialArray.Add(residential);
                        returnArray.Add((double)stockReturn.ReturnRateReinvest);
                        premium1 += residential;
                        premium2 = premium2 * (1 + residential);

                        ++count;
#if(DEBUG)
                        Console.Write('.');
#endif
                    }
                }
            }

            /* Instead of getting the average, the latest PE/Turnover/Liquidity/CirculatedMarketValue
             * might more meaningful
             */


            //int count2 = 0;
            //foreach(var de in rawDerivativeDaily)
            //{
            //    if (de.PE != null && de.Turnover != null && de.Liquidility != null && de.CirculatedMarketValue != null)
            //    {
            //        pe += (double)de.PE;
            //        turnover += (double)de.Turnover;
            //        liquidity += (double)de.Liquidility;
            //        circulateMarketValue += (double)de.CirculatedMarketValue;

            //        ++count2;
            //    }
            //}

            excessive1 = count == 0 ? 0 : excessive1 / count;
            excessive2 = count == 0 ? 0 : /*(excessive2 - 1) / count;*/Math.Pow(excessive2, 1d / count) - 1;
            premium1 = count == 0 ? 0 : premium1 / count;
            premium2 = count == 0 ? 0 : /*(premium2 - 1) / count*/Math.Pow(premium2, 1d / count) - 1;

            //pe = count2 == 0 ? 0 : pe / count2;
            //turnover = count2 == 0 ? 0 : turnover / count2;
            //circulateMarketValue = count2 == 0 ? 0 : circulateMarketValue / count2;
            //liquidity = count2 == 0 ? 0 : liquidity / count2;

            if(rawDerivativeDaily != null && rawDerivativeDaily.Count > 0)
            {
                pe = 
                    rawDerivativeDaily.ElementAt(0).PE == null ? 
                    0 : (double)rawDerivativeDaily.ElementAt(0).PE;
                turnover = 
                    rawDerivativeDaily.ElementAt(0).Turnover == null ? 
                    0 : (double)rawDerivativeDaily.ElementAt(0).Turnover;
                circulateMarketValue = 
                    rawDerivativeDaily.ElementAt(0).CirculatedMarketValue == null ? 
                    0 : (double)rawDerivativeDaily.ElementAt(0).CirculatedMarketValue;
                liquidity =
                    rawDerivativeDaily.ElementAt(0).Liquidility == null ?
                    0 : (double)rawDerivativeDaily.ElementAt(0).Liquidility;
            }

            if(rawDailyBeta != null && rawDailyBeta.Count > 0)
            {
                nonSysRisk = (double)rawDailyBeta.Average(a => a.NonSysRisk1);
                volatility = (double)rawDailyBeta.Average(a => a.Volatility);
            }
            
            switch(_avAlgo)
            {
                case "Geometric":

                    excessive = excessive2;
                    premium = premium2;
                    break;

                case "Arithmetic":

                    excessive = excessive1;
                    premium = premium1;
                    break;

                default:

                    throw new ApplicationException(string.Format("Unrecognized average algorithm {0}", _avAlgo));
            }

            List<double> riskCalMatrix;
            if(excessive != 0)
            {
                switch(_riskType.ToUpper())
                {
                    case "DOWNSIDE":
                        riskCalMatrix = returnArray.Where(c => c < excessive).ToList();
                        riskSampleCount = riskCalMatrix.Count;
                        break;
                    case "BOTHSIDE":
                        riskCalMatrix = returnArray;
                        riskSampleCount = count;
                        break;
                    default:
                        throw new ApplicationException(string.Format("Unrecognized risk type : <{0}>", _riskType));
                }

                double deviation = 0;
                foreach(double t in riskCalMatrix)
                {
                    deviation += Math.Pow((t - excessive), 2);
                }
                if(deviation == 0)
                {
                    excessiveSharpe = double.MaxValue;
                }
                else
                {
                    excessiveRisk = riskSampleCount > 1 ? 
                    Math.Sqrt(deviation / (riskSampleCount - 1)) :
                    Math.Sqrt(deviation);
                    excessiveSharpe = excessive / excessiveRisk;
                }
            }

            if(premium != 0)
            {
                switch (_riskType.ToUpper())
                {
                    case "DOWNSIDE":
                        riskCalMatrix = residentialArray.Where(c => c < premium).ToList();
                        riskSampleCount = riskCalMatrix.Count;
                        break;
                    case "BOTHSIDE":
                        riskCalMatrix = residentialArray;
                        riskSampleCount = count;
                        break;
                    default:
                        throw new ApplicationException(string.Format("Unrecognized risk type : <{0}>", _riskType));
                }

                double deviation = 0;
                foreach(double r in riskCalMatrix)
                {
                    deviation += Math.Pow((r - premium), 2);
                }

                if (deviation == 0)
                {
                    residentialSharpe = double.MaxValue;
                }
                else
                {
                    residentialRisk = riskSampleCount > 1 ?
                    Math.Sqrt(deviation / (riskSampleCount - 1)) :
                    Math.Sqrt(deviation);
                    residentialSharpe = premium / residentialRisk;
                }
            }

            var index = new StockIndex
            {
                StockId = stockId,
                ExcessiveReturn1 = excessive1 * 100,
                ExcessiveReturn2 = excessive2 * 100,
                ResidentialReturn1 = premium1 * 100,
                ResidentialReturn2 = premium2 * 100,
                ExcessiveReturnRisk = excessiveRisk * 100,
                ResidentialReturnRisk = residentialRisk * 100,
                ExcessiveSharpeRatio = excessiveSharpe * 100,
                ResidentialSharpeRatio = residentialSharpe * 100,
                PriceEarningRate = pe,
                Turnover = turnover,
                CirculatedMarketValue = circulateMarketValue,
                Liquidity = liquidity,
                LatestClosePrice = rawDailyReturn.Count == 0 ? 0 : (double)rawDailyReturn.ElementAt(0).price,
                AveragePricePastWeek = averagePriceInOneWeek == null ? 0 : (double)averagePriceInOneWeek,
                AveragePricePastMonth = averagePriceInOneMonth == null ? 0 : (double)averagePriceInOneMonth,
                AveragePricePastTwoMonth = averagePriceInTwoMonth == null ? 0 : (double)averagePriceInTwoMonth,
                AveragePricePastThreeMonth = averagePriceInThreeMonth == null ? 0 : (double)averagePriceInThreeMonth,
                AveragePricePastSixMonth = averagePriceInSixMonth == null ? 0 : (double)averagePriceInSixMonth,
                AveragePricePastYear = averagePriceInOneYear == null ? 0 : (double)averagePriceInOneYear,
                Volatility = volatility,
                CirculatedMarketValueWeightedNonSysRisk = nonSysRisk
            };
            

#if(DEBUG)
            Console.WriteLine(string.Format("\nStockID = {0}, StockName = {1}, Premium1 = {2:F}%, Premium2 = {3:F}%, Risk = {4:F}%, Sharpe Radio = {5:F}%, Count = {6}",
                stockId,
                _astock.TRD_Co.Where(c => c.StockId.Equals(stockId, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault().StockName,
                index.ResidentialReturn1, index.ResidentialReturn2, index.ResidentialReturnRisk, index.ResidentialSharpeRatio, count));
#endif
            if (_logger.IsDebugEnabled)
            {
                _logger.DebugFormat(
                    string.Format("StockID = {0}, ExcessiveReturn1 = {1:F}%, ExcessiveReturn2 = {2:F}%, ResidentialReturn1 = {3:F}%, ResidentialReturn2 = {4:F}%, ExcessiveRisk = {5:F}%, ResidentialRisk = {6:F}%, ExcessiveSharpe = {7:F}%, ResidentialSharpe = {8:F}%, NumOfValidTradeDays = {9}",
                    stockId, index.ExcessiveReturn1, index.ExcessiveReturn2, index.ResidentialReturn1,
                    index.ResidentialReturn2, index.ExcessiveReturnRisk, index.ResidentialReturnRisk,
                    index.ExcessiveSharpeRatio, index.ResidentialSharpeRatio, count));
            }

            return index;
        }

        private void Initialise()
        {
            GetIndustryLevel();
            GetMarketType();
            GetStartDate();
            GetEndDate();
            GetAlgorithm();
            GetBoardType();
            GetRiskType();
            GetReturnRateSource();
            GetPorfolioNumOfStocks();
            GetNumOfStocksKeptInPivot();
            GetMarketReturn();
            GetMarketIndex();
            GetIsFilterByBoardConfig();
            GetInterestedSymbols();
            GetSectorReturnRate();
        }

        private void GetMarketType()
        {
            bool isValid = int.TryParse(ConfigurationManager.AppSettings["MarketType"], out _mktType);
            if (isValid)
            {
                switch (_mktType)
                {
                    case 5:
                    case 10:
                    case 15:
                    case 21:
                    case 31:
                        break;
                    default:
                        throw new ApplicationException(string.Format("Unrecognized MarketType : <{0}>", _mktType));

                }
            }
            else
            {
                throw new ApplicationException(string.Format("Configuration item 'MarketType' must be present and valid"));
            }
        }

        private void GetStartDate()
        {
            bool isValid = DateTime.TryParse(ConfigurationManager.AppSettings["StartDate"], out _startDate);
            if (!isValid)
                throw new ApplicationException(string.Format("Configuration item 'StartDate' must be present and valid"));
        }

        private void GetEndDate()
        {
            var endDate = ConfigurationManager.AppSettings["EndDate"];
            if(!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out _endDate))
            {
                // Do nothing
            }
            else
            {
                _endDate = DateTime.Now.Date;
            }
        }

        private void GetMarketReturn()
        {
            if(_startDate > _endDate)
            {
                throw new ApplicationException(
                    string.Format("The time scope specified in the config file is incorrect, StartDate cannot be later than EndDate",
                    _startDate.ToShortDateString(), _endDate.ToShortDateString()));
            }
            var rawDailyMktReturn = _astock.STK_MKT_ConsolidatedReturn
                .Where(mkt => mkt.MarketType == _mktType && mkt.TradeDate2 >= _startDate && mkt.TradeDate2 <= _endDate)
                .Select(co => new { Date = co.TradeDate2, Ret = co.CirculatedMarketCapitalWeightedReturnRateReinvest }).ToList()
                .OrderBy(co => co.Date);

            foreach (var v in rawDailyMktReturn)
            {
               _mktReturn.Add((DateTime)v.Date, v.Ret);
            }
        }

        private void GetMarketIndex()
        {
            if (_startDate > _endDate)
            {
                throw new ApplicationException(
                    string.Format("The time scope specified in the config file is incorrect, StartDate cannot be later than EndDate",
                    _startDate.ToShortDateString(), _endDate.ToShortDateString()));
            }

            var rawMktIndexDaily = _astock.STK_MKT_IndexDaily.Where(tmp => tmp.TradingDate2 >= _startDate && tmp.TradingDate2 <= _endDate)
                .Select(idx => new { Id = idx.IndexID, Date = idx.TradingDate2, Ret = idx.IndexReturnRate }).ToList()
                .OrderBy(idx => idx.Date);

            //var ConvertedIndexDaily = rawMktIndexDaily
            //    .Select(idx => new { idx.Id, idx.Ret, Date = DateTime.Parse(idx.TradingDate) });

            //var timeConstrainedIndexDaily = ConvertedIndexDaily
            //    .Where(idx => idx.Date >= _startDate && idx.Date <= _endDate)
            //    .OrderBy(idx => idx.Date).ToList();

            foreach (var v in rawMktIndexDaily)
            {
                //if(MarketIndexMap[_mktType].Any(a => a.Equals(v.Id, StringComparison.InvariantCultureIgnoreCase)))
                //{
                    _mktIndex.Add(new Tuple<DateTime, string>((DateTime)v.Date, v.Id), v.Ret);
                //}
            }
        }

        private void GetSectorReturnRate()
        {
            if (_startDate > _endDate)
            {
                throw new ApplicationException(
                    string.Format("The time scope specified in the config file is incorrect, StartDate cannot be later than EndDate",
                    _startDate.ToShortDateString(), _endDate.ToShortDateString()));
            }

            var rawSectorDaily =
                _astock.STK_MKT_SectorDaily
                .Where(a => a.TradeDate >= _startDate && a.TradeDate <= _endDate)
                .OrderBy(a => a.TradeDate).ToList();

            foreach(var s in rawSectorDaily)
            {
                _mktSector.Add(
                    new Tuple<DateTime, string>((DateTime)s.TradeDate, s.SectorTypeId), 
                    s.CirculatedMarketValueWeightedReturnRateReinvest);
            }
        }

        private void GetIndustryLevel()
        {
            _industry = ConfigurationManager.AppSettings["IndustryLevel"];
            if (string.IsNullOrEmpty(_industry))
            {
                throw new ApplicationException(string.Format("Configuration item 'IndustryLevel' must be present and valid"));
            }

            switch (_industry)
            {
                case "IndustryTopLevel":
                case "Industry2ndLevel2001":
                case "Industry2ndLevel2012":
                case "IndustrySector":
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized industry level : <{0}>", _industry));
            }
        }

        private void GetBoardType()
        {
            _boardType = ConfigurationManager.AppSettings["BoardType"];
            if (string.IsNullOrEmpty(_boardType))
            {
                throw new ApplicationException(string.Format("Configuration item 'BoardType' must be present and valid"));
            }

            switch (_boardType.ToUpper())
            {
                case "SZMBA":
                case "SZMBB":
                case "SHMBA":
                case "SHMBB":
                case "MSB":
                case "GEMB":
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized board type : <{0}>", _boardType));
            }
        }

        private void GetReturnRateSource()
        {
            _source = ConfigurationManager.AppSettings["MarketReturnRatesSource"];
            if (string.IsNullOrEmpty(_source))
            {
                throw new ApplicationException(string.Format("Configuration item 'MarketReturnRatesSource' must be present and valid"));
            }

            switch (_source.ToUpper())
            {
                case "INDEX":
                case "CONSOLIDATED":
                case "SECTOR":
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized return rate source : <{0}>", _source));
            }
        }

        private void GetRiskType()
        {
            _riskType = ConfigurationManager.AppSettings["RiskType"];
            if (string.IsNullOrEmpty(_riskType))
            {
                throw new ApplicationException(string.Format("Configuration item 'RiskType' must be present and valid"));
            }

            switch (_riskType.ToUpper())
            {
                case "DOWNSIDE":
                case "BOTHSIDE":
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized risk type : <{0}>", _riskType));
            }
        }

        private void GetPorfolioNumOfStocks()
        {
            bool isSuccess = int.TryParse(ConfigurationManager.AppSettings["PorfolioNumOfStocks"], out _porfolioNumOfStocks);
            if (!isSuccess)
            {
                throw new ApplicationException(string.Format("Configuration item 'PorfolioNumOfStocks' must be present and valid"));
            }
        }

        private void GetNumOfStocksKeptInPivot()
        {
            bool isSuccess = int.TryParse(ConfigurationManager.AppSettings["NumOfTopNCompaniesInSameIndustry"], out _numOfStocksKeptInPivot);
            if (!isSuccess)
            {
                throw new ApplicationException(string.Format("Configuration item 'NumOfTopNCompaniesInSameIndustry' must be present and valid"));
            }
        }

        private void GetInterestedSymbols() 
        {
            string configString = ConfigurationManager.AppSettings["IsFilterBySpecifiedSymbol"];

            if (!string.IsNullOrEmpty(configString)
                && configString.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                _isFilterBySymbol = true;
            } 

            if(_isFilterBySymbol)
            {
                string symbols = ConfigurationManager.AppSettings["InterestedSymbols"];

                if(!string.IsNullOrEmpty(symbols))
                {
                    _interestedSymbols = new List<string>();
                    string[] candidates = symbols.Split(',');
                    foreach(var c in candidates)
                    {
                        _interestedSymbols.Add(c);
                    }
                }
            }
        }

        private void GetIsFilterByBoardConfig()
        {
            string configString = ConfigurationManager.AppSettings["IsFilterByBoard"];

            if (!string.IsNullOrEmpty(configString) 
                && configString.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                _isFilterByBoard = true;
            }
        }

        private void GetAlgorithm()
        {
            _avAlgo = ConfigurationManager.AppSettings["AverageAlgorithm"];

            if (string.IsNullOrEmpty(_avAlgo))
            {
                throw new ApplicationException(string.Format("Configuration item 'AverageAlgorithm' must be present and valid"));
            }

            switch (_avAlgo)
            {
                case "Geometric":
                case "Arithmetic":
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized average algorithm : <{0}>", _avAlgo));
            }

            _pfAlgo = ConfigurationManager.AppSettings["PortfolioAlgo"];

            if (string.IsNullOrEmpty(_pfAlgo))
            {
                throw new ApplicationException(string.Format("Configuration item 'PortfolioAlgo' must be present and valid"));
            }

            switch (_pfAlgo)
            {
                case "ExcessiveSharpeOptimal":
                case "ResidentialSharpeOptimal":
                case "ExcessiveReturnOptimal":
                case "ResidentialReturnOptimal": 
                    break;
                default:
                    throw new ApplicationException(string.Format("Unrecognized portfolio determination algorithm : <{0}>", _pfAlgo));
            }
        }
    }
}
