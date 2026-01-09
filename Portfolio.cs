using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MonteCarlo
{
    class Portfolio
    {
        readonly private UserData m_userData; //only some fields in UserData apply to us
        private List<double>      m_yearEndBalances  = new List<double>(MainPage.MAX_YEAR);
        private int               m_assetCount       = 0;
        private int               m_rebalanceCount   = 0;
        private int               m_tradeCount       = 0;
        //assets sold while rebalancing are cached in this var.
        private double            m_cache            = 0;
        //stocks
        private double            m_largeCapValue    = 0;
        private double            m_totalMarketValue = 0;
        private double            m_smallCapValue    = 0;
        private double            m_smallCapValValue = 0;
        //bonds
        private double            m_aggBondValue     = 0;
        private double            m_bill90Value      = 0;
        private double            m_longCorpValue    = 0;
        private double            m_munisValue       = 0;
        private double            m_trsry10YrValue   = 0;
        //intl
        private double            m_intlValue        = 0;
        private double            m_intlSmallValue   = 0;
        private double            m_emerValue        = 0;
        //alternative
        private double            m_reitValue        = 0;
        private double            m_goldValue        = 0;
        private double            m_cmdtyValue       = 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Portfolio(UserData userData, int assetCount)
        {
            Debug.Assert(assetCount > 0);

            m_userData   = new UserData(userData);
            m_assetCount = assetCount;
        }


        private Portfolio()
        {
        }


        //returns 1 if sold, else returns 0.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SellAssetForRebalance(double currAlloc, double trgtAlloc, ref double currVal)
        {
            Debug.Assert(currAlloc >= 0);
            Debug.Assert(trgtAlloc >= 0);

            int retVal = 0;

            if (    currAlloc == 0
                 || trgtAlloc == 0
                 || currVal   == 0
                 || Value      < MainPage.REBALANCE_PORT_MIN_AMOUNT)
            {
                goto Exit;
            }

            double perctGap = currAlloc - trgtAlloc;
            double amount   = (Value * perctGap) / 100;

            //this asset class grew too much or cache grew too much
            if (   perctGap > MainPage.REBALANCE_THRESHOLD_PERCT
                || amount   > MainPage.REBALANCE_CACHE_LIMIT)
            {  
                m_tradeCount++;
                m_cache += amount;
                currVal -= amount;
                retVal   = 1;

                Debug.Assert(currVal       >= 0);
                Debug.Assert(currVal/Value <= trgtAlloc);
            }

        Exit:
            return retVal;
        }


        //returns 1 if bought, else returns 0.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BuyAssetForRebalance(double currAlloc, double trgtAlloc, ref double currVal, double purchaseAmount)
        {
            Debug.Assert(currAlloc >= 0);
            Debug.Assert(trgtAlloc >= 0);

            int retVal = 0;

            if (   trgtAlloc      ==  0
                || Value           < MainPage.REBALANCE_PORT_MIN_AMOUNT
                || purchaseAmount ==  0)
            {
                goto Exit;
            }

            Debug.Assert((int)purchaseAmount <= (int)m_cache || (int)purchaseAmount + 1 <= (int)m_cache || (int)purchaseAmount - 1 <= (int)m_cache);

            double perctGap = trgtAlloc - currAlloc;

            //this asset class shrank too much or (cache has become too big and adding it purchaseAmount to this asset wont make it too big)
            if (    perctGap > MainPage.REBALANCE_THRESHOLD_PERCT
                || (m_cache  > MainPage.REBALANCE_CACHE_LIMIT && (currAlloc + ((purchaseAmount/Value)*100) < trgtAlloc + MainPage.REBALANCE_THRESHOLD_PERCT)))
            {
                Debug.Assert(   (int)m_cache     >= (int)purchaseAmount 
                             || (int)m_cache + 1 >= (int)purchaseAmount 
                             || (int)m_cache - 1 >= (int)purchaseAmount );

                m_tradeCount++;
                currVal += purchaseAmount;
                m_cache -= purchaseAmount;
                retVal   = 1;

                if(m_cache < 0)
                {
                    m_cache = 0;
                }
            }

        Exit:
            return retVal;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rebalance()
        {
            Debug.Assert(m_userData.Rebalance);
            Debug.Assert(m_assetCount > 1);

            int bakVal     = (int)Value;
            int rebalanced = 0;

            if (Value < MainPage.REBALANCE_PORT_MIN_AMOUNT || m_assetCount == 1)
            {
                goto Exit;
            }

            //scan asset classes getting too big and sell off
            //stocks
            SellAssetForRebalance(LargeCapAlloc   , m_userData.LargeCapAlloc     , ref m_largeCapValue);
            SellAssetForRebalance(TotalMarketAlloc, m_userData.TotalMarketAlloc  , ref m_totalMarketValue);
            SellAssetForRebalance(SmallCapAlloc   , m_userData.SmallCapAlloc     , ref m_smallCapValue);
            SellAssetForRebalance(SmallCapValAlloc, m_userData.SmallCapValAlloc  , ref m_smallCapValValue);
            //intl
            SellAssetForRebalance(IntlAlloc       , m_userData.IntlAlloc         , ref m_intlValue);
            SellAssetForRebalance(IntlSmallAlloc  , m_userData.IntlSmallCapAlloc , ref m_intlSmallValue);
            SellAssetForRebalance(EmerAlloc       , m_userData.EmerAlloc         , ref m_emerValue);
            //alternative
            SellAssetForRebalance(ReitAlloc       , m_userData.ReitAlloc         , ref m_reitValue);
            SellAssetForRebalance(GoldAlloc       , m_userData.GoldAlloc         , ref m_goldValue);
            SellAssetForRebalance(CmdtyAlloc      , m_userData.CmdtyAlloc        , ref m_cmdtyValue);
            //bonds
            SellAssetForRebalance(AggBondAlloc    , m_userData.AggBondAlloc      , ref m_aggBondValue);
            SellAssetForRebalance(Bill90Alloc     , m_userData.Bill90Alloc       , ref m_bill90Value);
            SellAssetForRebalance(Trsry10YrAlloc  , m_userData.Trsry10YrAlloc    , ref m_trsry10YrValue);
            SellAssetForRebalance(MunisAlloc      , m_userData.MunisAlloc        , ref m_munisValue);
            SellAssetForRebalance(LongCorpAlloc   , m_userData.LongCorpAlloc     , ref m_longCorpValue);

            Debug.Assert(m_cache >= 0);

            //nothing was sold, bail
            if (m_cache < MainPage.REBALANCE_CACHE_LIMIT)
            {
                goto Exit;
            }

            do
            {
                //TODO: buy based on who needs it the most

                //scan for asset classes getting too small and buy
                //bonds
                rebalanced += BuyAssetForRebalance(AggBondAlloc    , m_userData.AggBondAlloc      , ref m_aggBondValue    , m_cache * (m_userData.AggBondAlloc      / 100));
                rebalanced += BuyAssetForRebalance(Bill90Alloc     , m_userData.Bill90Alloc       , ref m_bill90Value     , m_cache * (m_userData.Bill90Alloc       / 100));
                rebalanced += BuyAssetForRebalance(Trsry10YrAlloc  , m_userData.Trsry10YrAlloc    , ref m_trsry10YrValue  , m_cache * (m_userData.Trsry10YrAlloc    / 100));
                rebalanced += BuyAssetForRebalance(MunisAlloc      , m_userData.MunisAlloc        , ref m_munisValue      , m_cache * (m_userData.MunisAlloc        / 100));
                rebalanced += BuyAssetForRebalance(LongCorpAlloc   , m_userData.LongCorpAlloc     , ref m_longCorpValue   , m_cache * (m_userData.LongCorpAlloc     / 100));
                //stocks
                rebalanced += BuyAssetForRebalance(LargeCapAlloc   , m_userData.LargeCapAlloc     , ref m_largeCapValue   , m_cache * (m_userData.LargeCapAlloc     / 100));
                rebalanced += BuyAssetForRebalance(TotalMarketAlloc, m_userData.TotalMarketAlloc  , ref m_totalMarketValue, m_cache * (m_userData.TotalMarketAlloc  / 100));
                rebalanced += BuyAssetForRebalance(SmallCapAlloc   , m_userData.SmallCapAlloc     , ref m_smallCapValue   , m_cache * (m_userData.SmallCapAlloc     / 100));
                rebalanced += BuyAssetForRebalance(SmallCapValAlloc, m_userData.SmallCapValAlloc  , ref m_smallCapValValue, m_cache * (m_userData.SmallCapValAlloc  / 100));
                //intl
                rebalanced += BuyAssetForRebalance(IntlAlloc       , m_userData.IntlAlloc         , ref m_intlValue       , m_cache * (m_userData.IntlAlloc         / 100));
                rebalanced += BuyAssetForRebalance(IntlSmallAlloc  , m_userData.IntlSmallCapAlloc , ref m_intlSmallValue  , m_cache * (m_userData.IntlSmallCapAlloc / 100));
                rebalanced += BuyAssetForRebalance(EmerAlloc       , m_userData.EmerAlloc         , ref m_emerValue       , m_cache * (m_userData.EmerAlloc         / 100));
                //alternative
                rebalanced += BuyAssetForRebalance(ReitAlloc       , m_userData.ReitAlloc         , ref m_reitValue       , m_cache * (m_userData.ReitAlloc         / 100));
                rebalanced += BuyAssetForRebalance(GoldAlloc       , m_userData.GoldAlloc         , ref m_goldValue       , m_cache * (m_userData.GoldAlloc         / 100));
                rebalanced += BuyAssetForRebalance(CmdtyAlloc      , m_userData.CmdtyAlloc        , ref m_cmdtyValue      , m_cache * (m_userData.CmdtyAlloc        / 100));
            } while (m_cache > MainPage.REBALANCE_CACHE_LIMIT);

        Exit:
            Debug.Assert(m_cache <= MainPage.REBALANCE_CACHE_LIMIT);
            Debug.Assert(VerifyAssetAlloc());

            if (rebalanced > 0)
            {
                m_rebalanceCount++;
            }

            Debug.Assert(bakVal == (int)Value || bakVal + 1 == (int)Value || bakVal - 1 == (int)Value);

#if DEBUG
            //Logger.Log( " Stocks:" + (int)(m_userData.TotalMarketAlloc + m_userData.LargeCapAlloc     + m_userData.SmallCapAlloc  + m_userData.SmallCapValAlloc) + 
            //         "\t\t  Intl:" + (int)(m_userData.IntlAlloc        + m_userData.IntlSmallCapAlloc + m_userData.EmerAlloc)     +
            //         "\t\t Bonds:" + (int)(m_userData.AggBondAlloc     + m_userData.LongCorpAlloc     + m_userData.Trsry10YrAlloc + m_userData.MunisAlloc + m_userData.Bill90Alloc) +
            //         "\t\t REIT: " + (int)m_userData.ReitAlloc  +
            //         "\t\t Cmdty:" + (int)m_userData.CmdtyAlloc +
            //         "\t\t Gold: " + (int)m_userData.GoldAlloc);

            //Logger.Log("-Stocks: " + (int)(TotalMarketAlloc + LargeCapAlloc       + SmallCapAlloc      + SmallCapValAlloc)
            //       + "\t\t -Intl:" + ((int)(IntlAlloc       + IntlSmallAlloc      + EmerAlloc))
            //       + "\t\t-Bonds:" + ((int)AggBondAlloc     + (int)Trsry10YrAlloc + (int)LongCorpAlloc + (int)MunisAlloc + (int)Bill90Alloc)
            //       + "\t\t-REIT: " + (int)ReitAlloc
            //       + "\t\t-Cmdty:" + (int)CmdtyAlloc
            //       + "\t\t-Gold: " + (int)GoldAlloc
            //       + "\t\tCache: " + (int)m_cache);
#endif
        }


        //add given amount to all assets
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncreaseAssets(double val)
        {
            Debug.Assert(val > 0);

            //stocks
            if (m_userData.LargeCapAlloc     > 0) m_largeCapValue    += val * (m_userData.LargeCapAlloc     / 100);
            if (m_userData.TotalMarketAlloc  > 0) m_totalMarketValue += val * (m_userData.TotalMarketAlloc  / 100);
            if (m_userData.SmallCapAlloc     > 0) m_smallCapValue    += val * (m_userData.SmallCapAlloc     / 100);
            if (m_userData.SmallCapValAlloc  > 0) m_smallCapValValue += val * (m_userData.SmallCapValAlloc  / 100);
            //bonds
            if (m_userData.AggBondAlloc      > 0) m_aggBondValue     += val * (m_userData.AggBondAlloc      / 100);
            if (m_userData.Bill90Alloc       > 0) m_bill90Value      += val * (m_userData.Bill90Alloc       / 100);
            if (m_userData.LongCorpAlloc     > 0) m_longCorpValue    += val * (m_userData.LongCorpAlloc     / 100);
            if (m_userData.MunisAlloc        > 0) m_munisValue       += val * (m_userData.MunisAlloc        / 100);
            if (m_userData.Trsry10YrAlloc    > 0) m_trsry10YrValue   += val * (m_userData.Trsry10YrAlloc    / 100);
            //intl
            if (m_userData.IntlAlloc         > 0) m_intlValue        += val * (m_userData.IntlAlloc         / 100);
            if (m_userData.IntlSmallCapAlloc > 0) m_intlSmallValue   += val * (m_userData.IntlSmallCapAlloc / 100);
            if (m_userData.EmerAlloc         > 0) m_emerValue        += val * (m_userData.EmerAlloc         / 100);
            //alternative
            if (m_userData.ReitAlloc         > 0) m_reitValue        += val * (m_userData.ReitAlloc         / 100);
            if (m_userData.GoldAlloc         > 0) m_goldValue        += val * (m_userData.GoldAlloc         / 100);
            if (m_userData.CmdtyAlloc        > 0) m_cmdtyValue       += val * (m_userData.CmdtyAlloc        / 100);
        }


        //reduce given amount from all assets
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReduceAssets(double val)
        {
            Debug.Assert(Value > 0);

            //TODO: when applying inf., if asset class goes to neg., we still need to apply inf. to something else...

            //stocks
            if (m_userData.LargeCapAlloc     > 0) m_largeCapValue    -= val * (m_userData.LargeCapAlloc     / 100);
            if (m_userData.TotalMarketAlloc  > 0) m_totalMarketValue -= val * (m_userData.TotalMarketAlloc  / 100);
            if (m_userData.SmallCapAlloc     > 0) m_smallCapValue    -= val * (m_userData.SmallCapAlloc     / 100);
            if (m_userData.SmallCapValAlloc  > 0) m_smallCapValValue -= val * (m_userData.SmallCapValAlloc  / 100);
            //bonds
            if (m_userData.AggBondAlloc      > 0) m_aggBondValue     -= val * (m_userData.AggBondAlloc      / 100);
            if (m_userData.Bill90Alloc       > 0) m_bill90Value      -= val * (m_userData.Bill90Alloc       / 100);
            if (m_userData.LongCorpAlloc     > 0) m_longCorpValue    -= val * (m_userData.LongCorpAlloc     / 100);
            if (m_userData.MunisAlloc        > 0) m_munisValue       -= val * (m_userData.MunisAlloc        / 100);
            if (m_userData.Trsry10YrAlloc    > 0) m_trsry10YrValue   -= val * (m_userData.Trsry10YrAlloc    / 100);
            //intl
            if (m_userData.IntlAlloc         > 0) m_intlValue        -= val * (m_userData.IntlAlloc         / 100);
            if (m_userData.IntlSmallCapAlloc > 0) m_intlSmallValue   -= val * (m_userData.IntlSmallCapAlloc / 100);
            if (m_userData.EmerAlloc         > 0) m_emerValue        -= val * (m_userData.EmerAlloc         / 100);
            //alternative
            if (m_userData.ReitAlloc         > 0) m_reitValue        -= val * (m_userData.ReitAlloc         / 100);
            if (m_userData.GoldAlloc         > 0) m_goldValue        -= val * (m_userData.GoldAlloc         / 100);
            if (m_userData.CmdtyAlloc        > 0) m_cmdtyValue       -= val * (m_userData.CmdtyAlloc        / 100);

            ResetToZero();
        }


        //reduce stocks by x% and increase bonds by x%
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoGlide(double perYear)
        {
            Debug.Assert(perYear > 0);

            bool retVal = true;

            if (m_userData.StocksCount == 0 || m_userData.BondsCount == 0)
            {
                //we cannot glide, all bonds or all stocks portfolio
                retVal = false;
                goto Exit;
            }

            double perctToDecrease = perYear / m_userData.StocksCount; //per asset class
            double perctDecreased  = 0;
            double perctIncreased  = 0;
            double spinCount       = 0;

            //reduce stocks alloc
            while(perctDecreased <= perYear) //keep reducing until we reach perYear
            {
                spinCount = perctDecreased;

                if (m_userData.LargeCapAlloc     > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.LargeCapAlloc     -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.TotalMarketAlloc  > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.TotalMarketAlloc  -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.SmallCapAlloc     > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.SmallCapAlloc     -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.SmallCapValAlloc  > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.SmallCapValAlloc  -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.IntlAlloc         > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.IntlAlloc         -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.IntlSmallCapAlloc > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.IntlSmallCapAlloc -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.EmerAlloc         > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.EmerAlloc         -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.ReitAlloc         > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.ReitAlloc         -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.CmdtyAlloc        > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.CmdtyAlloc        -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;
                if (m_userData.GoldAlloc         > perctToDecrease + MainPage.REBALANCE_THRESHOLD_PERCT)    { m_userData.GoldAlloc         -= perctToDecrease;   perctDecreased += perctToDecrease; }    if (perctDecreased >= perYear) break;

                //nothing was changed, bail to avoid getting stuck
                if(spinCount == perctDecreased)
                {
                    break;
                }
            }//while

            //increase bonds alloc
            while (perctIncreased <= perctDecreased) //keep increasing until we reach perctDecreased
            {
                if (m_userData.AggBondAlloc     > 0)    { perctIncreased += (perctDecreased / m_userData.BondsCount);    m_userData.AggBondAlloc   += (perctDecreased / m_userData.BondsCount); }    if (perctIncreased >= perctDecreased) break;
                if (m_userData.Bill90Alloc      > 0)    { perctIncreased += (perctDecreased / m_userData.BondsCount);    m_userData.Bill90Alloc    += (perctDecreased / m_userData.BondsCount); }    if (perctIncreased >= perctDecreased) break;
                if (m_userData.Trsry10YrAlloc   > 0)    { perctIncreased += (perctDecreased / m_userData.BondsCount);    m_userData.Trsry10YrAlloc += (perctDecreased / m_userData.BondsCount); }    if (perctIncreased >= perctDecreased) break;
                if (m_userData.MunisAlloc       > 0)    { perctIncreased += (perctDecreased / m_userData.BondsCount);    m_userData.MunisAlloc     += (perctDecreased / m_userData.BondsCount); }    if (perctIncreased >= perctDecreased) break;
                if (m_userData.LongCorpAlloc    > 0)    { perctIncreased += (perctDecreased / m_userData.BondsCount);    m_userData.LongCorpAlloc  += (perctDecreased / m_userData.BondsCount); }    if (perctIncreased >= perctDecreased) break;
            }//while

            Debug.Assert(VerifyAssetAlloc());

        Exit:
            return retVal;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyAssetAlloc()
        {
            bool retVal = false;

            if (Value == 0)
            {
                retVal = true;
                goto Exit;
            }

            double total = 
                            //stocks
                            LargeCapAlloc      +
                            TotalMarketAlloc   +
                            SmallCapAlloc      +
                            SmallCapValAlloc   +
                            //bonds
                            AggBondAlloc       +
                            Bill90Alloc        +
                            LongCorpAlloc      +
                            MunisAlloc         +
                            Trsry10YrAlloc     +
                            //intl
                            IntlAlloc          +
                            IntlSmallAlloc     +
                            EmerAlloc          +
                            //alternative
                            ReitAlloc          +
                            GoldAlloc          +
                            CmdtyAlloc         +
                            CacheAlloc;

            if (total > 99.9 && total <= 100.1)
            {
                retVal = true;
            }

        Exit:
            return retVal;
        }


        public double Value
        {
            get
            {
                return //stocks
                       m_largeCapValue     +
                       m_totalMarketValue  +
                       m_smallCapValue     +
                       m_smallCapValValue  +
                       //bonds
                       m_aggBondValue      +
                       m_bill90Value       +
                       m_longCorpValue     +
                       m_munisValue        +
                       m_trsry10YrValue    +
                       //intl
                       m_intlValue         +
                       m_intlSmallValue    +
                       m_emerValue         +
                       //alternative
                       m_reitValue         +
                       m_goldValue         +
                       m_cmdtyValue        +
                       //rebalance cache
                       m_cache;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetToZero()
        {
            //if any asset class has a negative value, make it 0
            //stocks
            if (m_largeCapValue    < 0)     m_largeCapValue    = 0;
            if (m_totalMarketValue < 0)     m_totalMarketValue = 0;
            if (m_smallCapValue    < 0)     m_smallCapValue    = 0;
            if (m_smallCapValValue < 0)     m_smallCapValValue = 0;
            //bonds
            if (m_aggBondValue     < 0)     m_aggBondValue     = 0;
            if (m_bill90Value      < 0)     m_bill90Value      = 0;
            if (m_longCorpValue    < 0)     m_longCorpValue    = 0;
            if (m_munisValue       < 0)     m_munisValue       = 0;
            if (m_trsry10YrValue   < 0)     m_trsry10YrValue   = 0;
            //intl
            if (m_intlValue        < 0)     m_intlValue        = 0;
            if (m_intlSmallValue   < 0)     m_intlSmallValue   = 0;
            if (m_emerValue        < 0)     m_emerValue        = 0;
            //alternative
            if (m_reitValue        < 0)     m_reitValue        = 0;
            if (m_goldValue        < 0)     m_goldValue        = 0;
            if (m_cmdtyValue       < 0)     m_cmdtyValue       = 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddToBalanceList(double val)
        {
            m_yearEndBalances.Add(val);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetBalanceAt(int index)
        {
            Debug.Assert(index < m_yearEndBalances.Count);

            return m_yearEndBalances[index];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<double> GetBalanceList()
        {
            return m_yearEndBalances;
        }


        //todo: what happens to Allocs if Value is 0?

        public int      RebalanceCount    {get  { return m_rebalanceCount;  }                   }
        public int      TradeCount        {get  { return m_tradeCount;      }
                                           set  { m_tradeCount = value;     }                   }

        //Current allocations (not target)
        //stocks
        public double   LargeCapAlloc     { get { return (m_largeCapValue     / Value) * 100; } }
        public double   TotalMarketAlloc  { get { return (m_totalMarketValue  / Value) * 100; } }
        public double   SmallCapAlloc     { get { return (m_smallCapValue     / Value) * 100; } }
        public double   SmallCapValAlloc  { get { return (m_smallCapValValue  / Value) * 100; } }
        //bonds
        public double   AggBondAlloc      { get { return (m_aggBondValue      / Value) * 100; } }
        public double   Bill90Alloc       { get { return (m_bill90Value       / Value) * 100; } }
        public double   LongCorpAlloc     { get { return (m_longCorpValue     / Value) * 100; } }
        public double   MunisAlloc        { get { return (m_munisValue        / Value) * 100; } }
        public double   Trsry10YrAlloc    { get { return (m_trsry10YrValue    / Value) * 100; } } 
        //intl
        public double   IntlAlloc         { get { return (m_intlValue         / Value) * 100; } }
        public double   IntlSmallAlloc    { get { return (m_intlSmallValue    / Value) * 100; } }
        public double   EmerAlloc         { get { return (m_emerValue         / Value) * 100; } }
        //alternative
        public double   ReitAlloc         { get { return (m_reitValue         / Value) * 100; } }
        public double   GoldAlloc         { get { return (m_goldValue         / Value) * 100; } }
        public double   CmdtyAlloc        { get { return (m_cmdtyValue        / Value) * 100; } }
        //rebalance cache
        public double   CacheAlloc        { get { return (m_cache             / Value) * 100; } }

        //Current values
        //stocks
        public double   LargeCapValue     { get { return m_largeCapValue;    } set { m_largeCapValue    = value; } }
        public double   TotalMarketValue  { get { return m_totalMarketValue; } set { m_totalMarketValue = value; } }
        public double   SmallCapValue     { get { return m_smallCapValue;    } set { m_smallCapValue    = value; } }
        public double   SmallCapValValue  { get { return m_smallCapValValue; } set { m_smallCapValValue = value; } }
        //bonds
        public double   AggBondValue      { get { return m_aggBondValue;     } set { m_aggBondValue     = value; } }
        public double   Bill90Value       { get { return m_bill90Value;      } set { m_bill90Value      = value; } }
        public double   LongCorpValue     { get { return m_longCorpValue;    } set { m_longCorpValue    = value; } }
        public double   MunisValue        { get { return m_munisValue;       } set { m_munisValue       = value; } }
        public double   Trsry10YrValue    { get { return m_trsry10YrValue;   } set { m_trsry10YrValue   = value; } }
        //intl
        public double   IntlValue         { get { return m_intlValue;        } set { m_intlValue        = value; } }
        public double   IntlSmallCapValue { get { return m_intlSmallValue;   } set { m_intlSmallValue   = value; } }
        public double   EmerValue         { get { return m_emerValue;        } set { m_emerValue        = value; } }
        //alternative
        public double   ReitValue         { get { return m_reitValue;        } set { m_reitValue        = value; } }
        public double   GoldValue         { get { return m_goldValue;        } set { m_goldValue        = value; } }
        public double   CmdtyValue        { get { return m_cmdtyValue;       } set { m_cmdtyValue       = value; } }
        //rebalance cache
        public double   CacheValue        { get { return m_cache; }          }
    }
}