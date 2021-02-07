using System;
using System.Collections.Generic;
using Bloomberg.API.Model.Enriched.BloombergTypes;

namespace Bloomberg.API.Model.Enriched
{
    /// <summary>
    /// The set of supported bloomberg fields
    /// </summary>
    public enum BloombergFields
    {
        [BloombergField("NAME", typeof(string))]
        Name,
        [BloombergField("SECURITY_DES", typeof(string))]
        DescriptionTicker,
        [BloombergField("TICKER", typeof(string))]
        Ticker,
        [BloombergField("ID_CUSIP", typeof(string))]
        Cusip,
        [BloombergField("ID_ISIN", typeof(string))]
        Isin,
        [BloombergField("ID_BB_UNIQUE", typeof(string))]
        BloombergUnique,
        [BloombergField("ID_BB_GLOBAL", typeof(string))]
        BloombergGlobal,
        [BloombergField("ROLLING_SERIES", typeof(int))]
        CdsSeries,
        [BloombergField("VERSION", typeof(int))]
        CdsVersion,
        [BloombergField("CRNCY", typeof(Currency))]
        Currency,
        [BloombergField("MIN_PIECE", typeof(decimal))]
        MinimumPiece,
        [BloombergField("MIN_INCREMENT", typeof(decimal))]
        MiniumIncrement,
        [BloombergField("MTG_FACTOR", typeof(decimal))]
        PoolFactorMtge,
        [BloombergField("MTG_FACTOR_PAY_DT", typeof(DateTime))]
        PoolFactorDateMtge,
        [BloombergField("PRINCIPAL_FACTOR", typeof(decimal))]
        PoolFactorCorp,
        [BloombergField("LAST_FACTOR_DATE", typeof(DateTime))]
        PoolFactorDateCorp,
        [BloombergField("PX_LAST_EOD", typeof(decimal))]
        LastPriceEod,
        [BloombergField("PX_CLOSE_1D", typeof(decimal))]
        LastClosePrice,
        [BloombergField("INT_ACC", typeof(decimal))]
        InterestAccuredPer100Face,
        [BloombergField("ISSUE_DT", typeof(DateTime))]
        IssueDate,
        [BloombergField("CDS_FIRST_ACCRUAL_START_DATE", typeof(DateTime))]
        CdsStartDate,
        [BloombergField("MATURITY", typeof(DateTime))]
        MaturityDate,
        [BloombergField("MTG_ORIG_AMT", typeof(decimal))]
        OriginalIssuanceMtge,
        [BloombergField("ISSUER", typeof(string))]
        Issuer,
        [BloombergField("AMT_ISSUED", typeof(decimal))]
        OriginalIssuanceCorp,
        [BloombergField("MARKET_SECTOR_DES", typeof(string))]
        MarketSector,
        [BloombergField("INDUSTRY_GROUP", typeof(string))]
        IndustryGroup,
        [BloombergField("INDUSTRY_SECTOR", typeof(string))]
        IndustrySector,
        [BloombergField("CPN_FREQ", typeof(string))]
        CouponFrequency,
        [BloombergField("DAY_CNT", typeof(string))]
        DayCount,
        [BloombergField("RESET_IDX", typeof(string))]
        CouponBenchmark,
        [BloombergField("CPN", typeof(decimal))]
        Coupon,
        [BloombergField("CPN_TYP", typeof(string))]
        CouponType,
        [BloombergField("FLT_SPREAD", typeof(decimal))]
        CouponSpread,
        [BloombergField("FIRST_CPN_DT", typeof(DateTime))]
        FirstCouponDate,
        [BloombergField("CPN_CRNCY", typeof(Currency))]
        CouponCurrency,
        [BloombergField("MTG_PAY_DELAY", typeof(int))]
        PaymentDelay,
        [BloombergField("MTG_HIST_FACT", typeof(IDictionary<DateTime, decimal>))]
        MortageFactorHistory,
        [BloombergField("HIST_CUMUL_LOSS_BOND", typeof(IDictionary<DateTime, decimal>))]
        CumulativeLossHistory,
        [BloombergField("HIST_CASH_FLOW", typeof(IEnumerable<CashFlowHistory>))]
        CashFlowHistory,
        [BloombergField("IS_REG_S", typeof(bool))]
        IsRegS,
        [BloombergField("144A_FLAG", typeof(bool))]
        Is144A,
        [BloombergField("LINKED_BONDS_INFO", typeof(IDictionary<string, string>))]
        LinkedBondInfo,

        [BloombergField("RTG_MOODY", typeof(string))]
        MoodysRating,
        [BloombergField("MOODY_EFF_DT", typeof(DateTime))]
        MoodysRatingEffectiveDate,

        [BloombergField("RTG_FITCH", typeof(string))]
        FitchRating,
        [BloombergField("FITCH_EFF_DT", typeof(DateTime))]
        FitchRatingEffectiveDate,

        [BloombergField("RTG_DBRS", typeof(string))]
        DbrsRating,
        [BloombergField("DBRS_EFF_DT", typeof(DateTime))]
        DbrsRatingEffectiveDate,

        [BloombergField("SW_NET_ACC_INT", typeof(decimal))]
        CDSAccural,
        [BloombergField("SW_PAY_NOTL_AMT", typeof(decimal))]
        CDSNotional,

        [BloombergField("RTG_SP", typeof(string))]
        StandardsPoorsRating,
        [BloombergField("SP_EFF_DT", typeof(DateTime))]
        StandardPoorsRatingEffectiveDate,

        [BloombergField("RTG_MDY_INITIAL", typeof(string))]
        InitialMoodysRating,
        [BloombergField("RTG_FITCH_INITIAL", typeof(string))]
        InitialFitchRating,
        [BloombergField("RTG_SP_INITIAL", typeof(string))]
        InitialStandardsPoorsRating,
        [BloombergField("RTG_DBRS_INITIAL", typeof(string))]
        InitialDbrsRating,

        [BloombergField("MTG_DEAL_NAME", typeof(string))]
        DealName,

        [BloombergField("MTGE_CMO_GROUP_LIST", typeof(IDictionary<int, string>), UseIndexAsKeyDictionary = true)]
        TrancheInformation,

        [BloombergField("MOST_SENIOR_INDICATOR", typeof(bool))]
        MostSeniorTranche,

        [BloombergField("MTG_CMO_CLASS", typeof(string))]
        TrancheClass,

        [BloombergField("PX_BID", typeof(decimal))]
        PriceBid,
        [BloombergField("PX_ASK", typeof(decimal))]
        PriceAsk,
        [BloombergField("PX_MID", typeof(decimal))]
        PriceMid,
        [BloombergField("CDS_FLAT_SPREAD", typeof(decimal))]
        CdsFlatSpread,
        [BloombergField("CDS_QUOTED_PRICE", typeof(decimal))]
        CdsQuotedPrice,

        [BloombergField("BID_SENDERS_FIRM_RT", typeof(string))]
        BidSenderFirm,
        [BloombergField("BID", typeof(decimal))]
        Bid,
        [BloombergField("MID", typeof(decimal))]
        Mid,
        [BloombergField("ASK", typeof(decimal))]
        Ask,
        [BloombergField("ASK_SENDERS_FIRM_RT", typeof(string))]
        AskSenderFirm,
        [BloombergField("PRICING_SOURCE", typeof(string))]
        PricingSource,
        [BloombergField("DISC_MRGN_BID", typeof(decimal))]
        DiscountMarginBid,
        [BloombergField("DISC_MRGN_ASK", typeof(decimal))]
        DiscountMarginAsk,
        [BloombergField("YLD_YTM_BID", typeof(decimal))]
        YieldToMarketBid,
        [BloombergField("YLD_YTM_ASK", typeof(decimal))]
        YieldToMarketAsk,
        [BloombergField("MTG_WAL", typeof(decimal))]
        MortgageWal,
        [BloombergField("LAST_UPDATE_DT", typeof(DateTime))]
        LastUpdated,

        UnknownField,
        
    }

    public enum Currency
    {
        GBP,
        USD,
        EUR,
        CHF
    }
}