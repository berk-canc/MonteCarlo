using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonteCarlo
{
    public class UserData
    {
        public UserData()
        {
        }

        public UserData(UserData param)
        {
            //investor
            this.PortVal           = param.PortVal;
            this.Years             = param.Years;
            this.Rebalance         = param.Rebalance;
            this.Glide             = param.Glide;
            this.AnnContr          = param.AnnContr;
            this.AnnWithdrwlDlr    = param.AnnWithdrwlDlr;
            this.GlidePerct        = param.GlidePerct;
            this.FeesPerct         = param.FeesPerct;
            this.Inflation         = param.Inflation;
            //alloc - stocks
            this.TotalMarketAlloc  = param.TotalMarketAlloc;
            this.LargeCapAlloc     = param.LargeCapAlloc;
            this.SmallCapAlloc     = param.SmallCapAlloc;
            this.SmallCapValAlloc  = param.SmallCapValAlloc;
            //intl
            this.IntlAlloc         = param.IntlAlloc;
            this.IntlSmallCapAlloc = param.IntlSmallCapAlloc;
            this.EmerAlloc         = param.EmerAlloc;
            //bonds
            this.AggBondAlloc      = param.AggBondAlloc;
            this.Bill90Alloc       = param.Bill90Alloc;
            this.LongCorpAlloc     = param.LongCorpAlloc;
            this.MunisAlloc        = param.MunisAlloc;
            this.Trsry10YrAlloc    = param.Trsry10YrAlloc;
            //alternative
            this.ReitAlloc         = param.ReitAlloc;
            this.GoldAlloc         = param.GoldAlloc;
            this.CmdtyAlloc        = param.CmdtyAlloc;
        }

        //investor
        public double   PortVal           { get; set; }
        public int      Years             { get; set; }
        public bool     Rebalance         { get; set; }
        public bool     Glide             { get; set; }
        public double   Inflation         { get; set; }
        public double   AnnContr          { get; set; }
        public double   AnnWithdrwlDlr    { get; set; }
        public double   GlidePerct        { get; set; }
        public double   FeesPerct         { get; set; }
        //bonds
        public double   AggBondAlloc      { get; set; }
        public double   Bill90Alloc       { get; set; }
        public double   LongCorpAlloc     { get; set; }
        public double   MunisAlloc        { get; set; }
        public double   Trsry10YrAlloc    { get; set; }
        //stocks
        public double   LargeCapAlloc     { get; set; }
        public double   TotalMarketAlloc  { get; set; }
        public double   SmallCapAlloc     { get; set; }
        public double   SmallCapValAlloc  { get; set; }
        //intl
        public double   IntlAlloc         { get; set; }
        public double   IntlSmallCapAlloc { get; set; } 
        public double   EmerAlloc         { get; set; }
        //alternative
        public double   ReitAlloc         { get; set; }
        public double   GoldAlloc         { get; set; }
        public double   CmdtyAlloc        { get; set; }

        public int AssetClassCount
        {
            get
            {
                int count = 0;

                //bonds
                if (AggBondAlloc      > 0)   count++;
                if (Bill90Alloc       > 0)   count++;
                if (LongCorpAlloc     > 0)   count++;
                if (MunisAlloc        > 0)   count++;
                if (Trsry10YrAlloc    > 0)   count++;
                //stocks
                if (LargeCapAlloc     > 0)   count++;
                if (TotalMarketAlloc  > 0)   count++;
                if (SmallCapAlloc     > 0)   count++;
                if (SmallCapValAlloc  > 0)   count++;
                //intl
                if (IntlAlloc         > 0)   count++;
                if (IntlSmallCapAlloc > 0)   count++;
                if (EmerAlloc         > 0)   count++;
                //alternative
                if (ReitAlloc         > 0)   count++;
                if (GoldAlloc         > 0)   count++;
                if (CmdtyAlloc        > 0)   count++;

                return count;
            }
        }

        //includes REIT, gold and cmdty.
        public int StocksCount
        {
            get
            {
                int count = 0;

                //stocks
                if (LargeCapAlloc     > 0)   count++;
                if (TotalMarketAlloc  > 0)   count++;
                if (SmallCapAlloc     > 0)   count++;
                if (SmallCapValAlloc  > 0)   count++;
                //intl
                if (IntlAlloc         > 0)   count++;
                if (IntlSmallCapAlloc > 0)   count++;
                if (EmerAlloc         > 0)   count++;
                //alternative
                if (ReitAlloc         > 0)   count++;
                if (GoldAlloc         > 0)   count++;
                if (CmdtyAlloc        > 0)   count++;

                return count;
            }
        }


        public int BondsCount
        {
            get
            {
                int count = 0;

                if (AggBondAlloc     > 0)   count++;
                if (Bill90Alloc      > 0)   count++;
                if (LongCorpAlloc    > 0)   count++;
                if (MunisAlloc       > 0)   count++;
                if (Trsry10YrAlloc   > 0)   count++;

                return count;
            }
        }
    }
}