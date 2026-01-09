using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Syncfusion.UI.Xaml.Charts;
using Windows.UI.Text;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Storage;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409


namespace MonteCarlo
{
    class LineSeriesData
    {
        public LineSeriesData(string year, double portVal)
        {
            Year    = year;
            PortVal = portVal;
        }

        public string Year    { get; set; }
        public double PortVal { get; set; }
    }


    class PieSeriesData
    {
        public PieSeriesData(string assetClass, double percentage)
        {
            AssetClass = assetClass;
            Percentage = percentage;
        }

        public string AssetClass { get; set; }
        public double Percentage { get; set; }
    }


    public class AboutInfo
    {
        public string Title
        {
            get
            {
                Assembly asm = typeof(App).GetTypeInfo().Assembly;
                return ((AssemblyTitleAttribute)asm.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title;
            }
        }


        public string Version
        {
            get
            {
                Package        package   = Package.Current;
                PackageId      packageId = package.Id;
                PackageVersion version   = packageId.Version;

                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
        }


        public string Copyright
        {
            get
            {
                Assembly asm = typeof(App).GetTypeInfo().Assembly;
                return ((AssemblyCopyrightAttribute)asm.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
            }
        }


        public string Company
        {
            get
            {
                Assembly asm = typeof(App).GetTypeInfo().Assembly;
                return ((AssemblyCompanyAttribute)asm.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company;
            }
        }


        public string About
        {
            get
            {
                return Title + "\n\nBy " + Company + " | info@sharpdojo.com" + "\n\nVersion " + Version + "\n\n" + Copyright + " " + Company + ". All Rights Reserved.";
            }
        }
    }


    public class AboutInfoViewModel
    {
        private AboutInfo m_instance = new AboutInfo();
        public  AboutInfo Instance { get { return m_instance; } }
    }


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public const int     MIN_YEAR                               = 5;
        public const int     MAX_YEAR                               = 70;       //start investing at age 20 till 90, 90-20=70
        public const int     MIN_ANN_CONTR                          = 1000;
        public const int     MAX_ANN_CONTR                          = 100000;
        public const int     MIN_ANN_WTHDRW                         = 1000;
        public const int     MAX_WTHDRW_PRCT                        = 20;
        public const int     MAX_INFLATION                          = 15;
        public const int     MIN_INFLATION                          = -15;
        public const int     MIN_PORT_VAL                           = 5000;     //same as SUCCESS_RATE_MIN, REBALANCE_PORT_MIN_AMOUNT
        public const int     MAX_PORT_VAL                           = 10000000; //10mil
        public const int     MAX_FEE                                = 10;
        public const int     MAX_GLIDE                              = 9;
        public const int     RUN_COUNT                              = 1000;
        public const int     SUCCESS_RATE_MIN                       = 5000;     //same as MIN_PORT_VAL and used in m_yearZero
        public const double  RISK_MODERATE_SD_BEGIN                 = 0.09;
        public const double  RISK_MODERATE_SD_END                   = 0.16;
        public const int     REBALANCE_THRESHOLD_PERCT              = 3;
        public const int     REBALANCE_PORT_MIN_AMOUNT              = 5000;     //same as MIN_PORT_VAL
        public const int     REBALANCE_CACHE_LIMIT                  = 2000;
        public const int     TRADE_FEE                              = 5;
        public const string  HISTORICAL_INFLATION                   = "Using actual inflation data";
        public const string  TEXTBOX_MEDIAN_TEXT_MEDIAN             = "Median portfolio value";
        public const string  TEXTBOX_MEDIAN_TEXT_BACKTEST           = "Backtested portfolio value";
        public const string  LABEL_MEDIAN_TEXT_MEDIAN               = "Below are based on the median portfolio:";
        public const string  LABEL_MEDIAN_TEXT_BACKTEST             = "Below are based on the backtested portfolio:";
        ///////////////////////////////////////////////////////////////////////////////////////////
        List<double>         m_cummRets                             = new List<double>(RUN_COUNT); //list of port. ending values
        List<double>         m_totalMarketRets                      = new List<double>(MAX_YEAR);
        List<double>         m_largeCapRets                         = new List<double>(MAX_YEAR);
        List<double>         m_smallCapRets                         = new List<double>(MAX_YEAR);
        List<double>         m_smallCapValRets                      = new List<double>(MAX_YEAR);
        List<double>         m_aggBondRets                          = new List<double>(MAX_YEAR);
        List<double>         m_bills90Rets                          = new List<double>(MAX_YEAR);
        List<double>         m_trsry10Rets                          = new List<double>(MAX_YEAR);
        List<double>         m_longCorpRets                         = new List<double>(MAX_YEAR);
        List<double>         m_munisRets                            = new List<double>(MAX_YEAR);
        List<double>         m_reitRets                             = new List<double>(MAX_YEAR);
        List<double>         m_intlRets                             = new List<double>(MAX_YEAR);
        List<double>         m_intlSmallCapRets                     = new List<double>(MAX_YEAR);
        List<double>         m_emerRets                             = new List<double>(MAX_YEAR);
        List<double>         m_goldRets                             = new List<double>(MAX_YEAR);
        List<double>         m_cmdtyRets                            = new List<double>(MAX_YEAR);
        List<Portfolio>      m_portfolios                           = new List<Portfolio>(RUN_COUNT); //list of all portfolios
        List<double>         m_portMedianRets                       = new List<double>(MAX_YEAR);     //annual returns of med. portf.
        Portfolio            m_portMedian                           = null;                           //median port.   , taken from m_portfolios
        Portfolio            m_port25Perctl                         = null;                           //75 perct. port., taken from m_portfolios
        Portfolio            m_port75Perctl                         = null;                           //25 perct. port., taken from m_portfolios
        Portfolio            m_backTestPortfolio                    = null;
        double               m_portValMin                           = 0;
        double               m_portValMed                           = 0;
        double               m_portValMean                          = 0;
        double               m_portValMax                           = 0;
        double               m_retDollar                            = 0;
        double               m_retPerct                             = 0;
        double               m_retAnn                               = 0;
        int                  m_MDDBegin                             = 0;
        int                  m_MDDEnd                               = 0;
        double               m_MDDLoss                              = 0;
        double               m_SD                                   = 0;
        double               m_MDD                                  = 0;
        double               m_success                              = 0;
        double               m_feesDlr                              = 0;
        int                  m_yearZero                             = 0; //the year when port. ran out of money
        NumberFormatInfo     m_formatInfo                           = CultureInfo.CurrentUICulture.NumberFormat;
        Random               m_rand                                 = new Random(); //this way we don't get the same rand. val. in tight loops
        UserData             m_userData                             = new UserData();
        string               m_riskLevel                            = "";
        List<string>         m_desc                                 = new List<string>(20); //20 lines capacity for description
        MonteCarloPro.BackTestContentDialog  m_backTestDlg          = null;
        ObservableCollection<LineSeriesData> m_obsvrCollLineChart   = null;
        ObservableCollection<PieSeriesData>  m_obsvrCollPieChart    = null;
        LineSeries           m_lineSeries                           = null;
        PieSeries            m_pieSeries                            = null;
        bool                 m_backTest                             = false;
        bool                 m_trimmed                              = false; //time horizon trimmed for backtesting
        ///////////////////////////////////////////////////////////////////////////////////////////


        public MainPage()
        {
            Logger.Init();

            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            //InvestorGrid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(122, 133, 144, 155));
            //AssetsGrid.Background   = new SolidColorBrush(Windows.UI.Color.FromArgb(212, 213, 214, 215));
            //ResultsGrid.Background  = new SolidColorBrush(Windows.UI.Color.FromArgb(102, 103, 104, 105));
            //VisualGrid.Background   = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 210, 230, 240));

            m_formatInfo.CurrencyDecimalDigits = 0;
            m_formatInfo.PercentDecimalDigits  = 2;
            m_formatInfo.NumberDecimalDigits   = 0;

            PortValAxisGrowth.LabelFormat   = String.Format("C", m_formatInfo);
            PortValAxisBackTest.LabelFormat = String.Format("C", m_formatInfo);
            chartGrowth.Visibility          = Visibility.Collapsed;
            chartAlloc.Visibility           = Visibility.Collapsed;
            chartBackTest.Visibility        = Visibility.Collapsed;
            AboutInfoViewModel              = new AboutInfoViewModel();

            LoadInput();
            FormatInputStrings();
        }


        public AboutInfoViewModel AboutInfoViewModel
        {
            get;
            set;
        }


        private void ToggleSwitchRebalance_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
            {
                goto Exit;
            }

            m_userData.Rebalance = toggleSwitch.IsOn;

            //null when program first starts cause object is not yet created
            if(toggleSwitchGlide != null)
            {
                toggleSwitchGlide.IsEnabled = toggleSwitch.IsOn;
                textBoxGlide.IsEnabled      = toggleSwitchGlide.IsEnabled && toggleSwitchGlide.IsOn;
            }

        Exit:
            return;
        }


        private void ToggleSwitchGlide_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;

            if (toggleSwitch == null)
            {
                goto Exit;
            }

            m_userData.Glide       = toggleSwitch.IsOn;
            textBoxGlide.IsEnabled = toggleSwitch.IsOn;

        Exit:
            return;
        }


        public static async void ShowDialog(string content)
        {
            MessageDialog dlg = new MessageDialog(content);
            await dlg.ShowAsync();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool VerifyInput()
        {
            const string funcName         = "VerifyInput";
            bool   retVal                 = false;
            string message                = "";

            //input
            textBoxPortVal.Text           = textBoxPortVal.Text.Trim();
            textBoxYears.Text             = textBoxYears.Text.Trim();
            textBoxAnnualContr.Text       = textBoxAnnualContr.Text.Trim();
            textBoxWithdrwl.Text          = textBoxWithdrwl.Text.Trim();
            textBoxGlide.Text             = textBoxGlide.Text.Trim();
            textBoxFeesPerct.Text         = textBoxFeesPerct.Text.Trim();
            textBoxInflation.Text         = textBoxInflation.Text.Trim();
            //stocks
            textBoxTotalMarketAlloc.Text  = textBoxTotalMarketAlloc.Text.Trim();
            textBoxLargeCapAlloc.Text     = textBoxLargeCapAlloc.Text.Trim();
            textBoxSmallCapAlloc.Text     = textBoxSmallCapAlloc.Text.Trim();
            textBoxSmallValAlloc.Text     = textBoxSmallValAlloc.Text.Trim();
            //bonds
            textBoxAggBondAlloc.Text      = textBoxAggBondAlloc.Text.Trim();
            textBox90DayBillAlloc.Text    = textBox90DayBillAlloc.Text.Trim();
            textBox10YrTresAlloc.Text     = textBox10YrTresAlloc.Text.Trim();
            textBoxMuniAlloc.Text         = textBoxMuniAlloc.Text.Trim();
            textBoxLongCorpAlloc.Text     = textBoxLongCorpAlloc.Text.Trim();
            //intl
            textBoxIntlAlloc.Text         = textBoxIntlAlloc.Text.Trim();
            textBoxIntlSmallCapAlloc.Text = textBoxIntlSmallCapAlloc.Text.Trim();
            textBoxEmerAlloc.Text         = textBoxEmerAlloc.Text.Trim();
            //alternative
            textBoxGoldAlloc.Text         = textBoxGoldAlloc.Text.Trim();
            textBoxCmdtyAlloc.Text        = textBoxCmdtyAlloc.Text.Trim();
            textBoxReitAlloc.Text         = textBoxReitAlloc.Text.Trim();

            try
            {
                UnformatInputStrings();

                //switching from backtest w/historical data to normal mode
                if(textBoxInflation.Text == HISTORICAL_INFLATION)
                {
                    message = "inflation";
                    textBoxInflation.Text = (m_userData.Inflation == 0) ? "" : Convert.ToString(m_userData.Inflation);
                }
                
                //investor info
                message = "portfolio value";            m_userData.PortVal           =  Convert.ToInt64(textBoxPortVal.Text);    //do not set to 0 if empty, it's a required field
                message = "time horizon";               m_userData.Years             =  Convert.ToInt32(textBoxYears.Text);      //ditto
                message = "annual contribution";        m_userData.AnnContr          = (textBoxAnnualContr.Text       == "") ? 0 : Convert.ToInt32(textBoxAnnualContr.Text);
                message = "annual withdrawal";          m_userData.AnnWithdrwlDlr    = (textBoxWithdrwl.Text          == "") ? 0 : Convert.ToInt32(textBoxWithdrwl.Text);
                message = "bond increase";              m_userData.GlidePerct        = (textBoxGlide.Text             == "") ? 0 : Convert.ToDouble(textBoxGlide.Text);
                message = "inflation";                  m_userData.Inflation         = (textBoxInflation.Text         == "") ? 0 : Convert.ToDouble(textBoxInflation.Text);
                message = "fees/expenses";              m_userData.FeesPerct         = (textBoxFeesPerct.Text         == "") ? 0 : Convert.ToDouble(textBoxFeesPerct.Text);
                                                        m_userData.Rebalance         = toggleSwitchRebalance.IsOn;
                //stocks
                message = "total market";               m_userData.TotalMarketAlloc  = (textBoxTotalMarketAlloc.Text  == "") ? 0 : Convert.ToInt32(textBoxTotalMarketAlloc.Text);
                message = "large cap";                  m_userData.LargeCapAlloc     = (textBoxLargeCapAlloc.Text     == "") ? 0 : Convert.ToInt32(textBoxLargeCapAlloc.Text);
                message = "small cap";                  m_userData.SmallCapAlloc     = (textBoxSmallCapAlloc.Text     == "") ? 0 : Convert.ToInt32(textBoxSmallCapAlloc.Text);
                message = "small cap value";            m_userData.SmallCapValAlloc  = (textBoxSmallValAlloc.Text     == "") ? 0 : Convert.ToInt32(textBoxSmallValAlloc.Text); 
                //bonds
                message = "US aggregate bond";          m_userData.AggBondAlloc      = (textBoxAggBondAlloc.Text      == "") ? 0 : Convert.ToInt32(textBoxAggBondAlloc.Text);
                message = "90-day bill";                m_userData.Bill90Alloc       = (textBox90DayBillAlloc.Text    == "") ? 0 : Convert.ToInt32(textBox90DayBillAlloc.Text);
                message = "10-yr treasury bond";        m_userData.Trsry10YrAlloc    = (textBox10YrTresAlloc.Text     == "") ? 0 : Convert.ToInt32(textBox10YrTresAlloc.Text);
                message = "long-term corporate bond";   m_userData.LongCorpAlloc     = (textBoxLongCorpAlloc.Text     == "") ? 0 : Convert.ToInt32(textBoxLongCorpAlloc.Text);
                message = "municipal";                  m_userData.MunisAlloc        = (textBoxMuniAlloc.Text         == "") ? 0 : Convert.ToInt32(textBoxMuniAlloc.Text);
                //intl
                message = "intl ex-US";                 m_userData.IntlAlloc         = (textBoxIntlAlloc.Text         == "") ? 0 : Convert.ToInt32(textBoxIntlAlloc.Text);
                message = "intl small cap";             m_userData.IntlSmallCapAlloc = (textBoxIntlSmallCapAlloc.Text == "") ? 0 : Convert.ToInt32(textBoxIntlSmallCapAlloc.Text);
                message = "intl emerging markets";      m_userData.EmerAlloc         = (textBoxEmerAlloc.Text         == "") ? 0 : Convert.ToInt32(textBoxEmerAlloc.Text);
                //alternative
                message = "gold";                       m_userData.GoldAlloc         = (textBoxGoldAlloc.Text         == "") ? 0 : Convert.ToInt32(textBoxGoldAlloc.Text);
                message = "commodity";                  m_userData.CmdtyAlloc        = (textBoxCmdtyAlloc.Text        == "") ? 0 : Convert.ToInt32(textBoxCmdtyAlloc.Text);
                message = "REIT";                       m_userData.ReitAlloc         = (textBoxReitAlloc.Text         == "") ? 0 : Convert.ToInt32(textBoxReitAlloc.Text);

                m_formatInfo.PercentDecimalDigits = 0;
                textBoxTotalAlloc.Text            = (GetTotalAlloc() / 100).ToString("P", m_formatInfo);
                m_formatInfo.PercentDecimalDigits = 2;
            }
            catch(FormatException)
            {
                message = "Enter a numeric value in " + message + " textbox.";
                goto Exit;
            }

            if (m_userData.PortVal < MIN_PORT_VAL || m_userData.PortVal > MAX_PORT_VAL)
            {
                message = String.Format("Portfolio must be between {0} and {1}.", MIN_PORT_VAL.ToString("C", m_formatInfo), MAX_PORT_VAL.ToString("C", m_formatInfo));
                goto Exit;
            }

            if (m_userData.Years < MIN_YEAR || m_userData.Years > MAX_YEAR)
            {
                message = String.Format("Time horizon must be between {0} and {1} years.", MIN_YEAR, MAX_YEAR);
                goto Exit;
            }

            if (     m_userData.AnnContr != 0
                 && (m_userData.AnnContr < MIN_ANN_CONTR || m_userData.AnnContr > MAX_ANN_CONTR))
            {
                message = String.Format("Annual contribution must be between {0} and {1}.", MIN_ANN_CONTR.ToString("C", m_formatInfo), MAX_ANN_CONTR.ToString("C", m_formatInfo));
                goto Exit;
            }

            if (    m_userData.AnnWithdrwlDlr != 0 
                && (m_userData.AnnWithdrwlDlr  < MIN_ANN_WTHDRW || m_userData.AnnWithdrwlDlr > (m_userData.PortVal * (Convert.ToDouble(MAX_WTHDRW_PRCT) / 100))))
            {
                message = String.Format("Annual withdrawal must be greater than {0} and less than {1} of the portfolio value.", MIN_ANN_WTHDRW.ToString("C", m_formatInfo), (Convert.ToDouble(MAX_WTHDRW_PRCT) / 100).ToString("P", m_formatInfo));
                goto Exit;
            }

            if(     m_userData.Glide 
                && (m_userData.GlidePerct <= 0 || m_userData.GlidePerct > MAX_GLIDE))
            {
                message = String.Format("Percentage to increase bonds must be greater than 0 and smaller than {0}.", (Convert.ToDouble(MAX_GLIDE) / 100).ToString("P", m_formatInfo));
                goto Exit;
            }

            if(m_userData.Glide && m_userData.AnnWithdrwlDlr > 0)
            {
                message = String.Format("Bond increase option only applies to growth scenarios. Set annual withdrawal to 0.");
                goto Exit;
            }

            if (m_userData.Inflation < MIN_INFLATION || m_userData.Inflation > MAX_INFLATION)
            {
                message = String.Format("Inflation must be between {0} and {1}.", (Convert.ToDouble(MIN_INFLATION) / 100).ToString("P", m_formatInfo), (Convert.ToDouble(MAX_INFLATION) / 100).ToString("P", m_formatInfo));
                goto Exit;
            }

            if (m_userData.FeesPerct < 0 || m_userData.FeesPerct > MAX_FEE)
            {
                message = String.Format("Fees/expenses must be between {0} and {1}.", (0).ToString("P", m_formatInfo), (Convert.ToDouble(MAX_FEE) / 100).ToString("P", m_formatInfo));
                goto Exit;
            }

            if (m_userData.TotalMarketAlloc < 0 || m_userData.TotalMarketAlloc > 100)
            {
                message = "Invalid total stock market allocation.";
                goto Exit;
            }

            if (m_userData.LargeCapAlloc < 0 || m_userData.LargeCapAlloc > 100)
            {
                message = "Invalid large-cap stock allocation.";
                goto Exit;
            }

            if (m_userData.SmallCapAlloc < 0 || m_userData.SmallCapAlloc > 100)
            {
                message = "Invalid small-cap stock allocation.";
                goto Exit;
            }

            if (m_userData.SmallCapValAlloc < 0 || m_userData.SmallCapValAlloc > 100)
            {
                message = "Invalid small-cap value stock allocation.";
                goto Exit;
            }

            if (m_userData.IntlAlloc < 0 || m_userData.IntlAlloc > 100)
            {
                message = "Invalid international stock allocation.";
                goto Exit;
            }

            if (m_userData.IntlSmallCapAlloc < 0 || m_userData.IntlSmallCapAlloc > 100)
            {
                message = "Invalid international small-cap stock allocation.";
                goto Exit;
            }

            if (m_userData.EmerAlloc < 0 || m_userData.EmerAlloc > 100)
            {
                message = "Invalid emerging market stock allocation.";
                goto Exit;
            }

            if (m_userData.AggBondAlloc < 0 || m_userData.AggBondAlloc > 100)
            {
                message = "Invalid aggregate bond allocation.";
                goto Exit;
            }

            if (m_userData.Trsry10YrAlloc < 0 || m_userData.Trsry10YrAlloc > 100)
            {
                message = "Invalid 10-Year treasury allocation.";
                goto Exit;
            }

            if (m_userData.Bill90Alloc < 0 || m_userData.Bill90Alloc > 100)
            {
                message = "Invalid 90-Day bill allocation.";
                goto Exit;
            }

            if (m_userData.LongCorpAlloc < 0 || m_userData.LongCorpAlloc > 100)
            {
                message = "Invalid long-term corporate bond allocation.";
                goto Exit;
            }

            if (m_userData.MunisAlloc < 0 || m_userData.MunisAlloc > 100)
            {
                message = "Invalid municipal bond allocation.";
                goto Exit;
            }

            if (m_userData.ReitAlloc < 0 || m_userData.ReitAlloc > 100)
            {
                message = "Invalid REIT allocation.";
                goto Exit;
            }

            if (m_userData.GoldAlloc < 0 || m_userData.GoldAlloc > 100)
            {
                message = "Invalid gold allocation.";
                goto Exit;
            }

            if (m_userData.CmdtyAlloc < 0 || m_userData.CmdtyAlloc > 100)
            {
                message = "Invalid commodity allocation.";
                goto Exit;
            }

            if (GetTotalAlloc() != 100)
            {
                textBoxTotalAlloc.FontWeight      = FontWeights.Bold;
                m_formatInfo.PercentDecimalDigits = 0;

                message = String.Format("Total allocation must be {0}.", (1).ToString("P", m_formatInfo));

                m_formatInfo.PercentDecimalDigits = 2;
                goto Exit;
            }

            textBoxTotalAlloc.FontWeight = FontWeights.Normal;
            retVal = true;

            //nothing to glide...
            if(m_userData.StocksCount == 0 || m_userData.BondsCount == 0)
            {
                toggleSwitchGlide.IsOn = false;
                textBoxGlide.IsEnabled = false;
            }

            //nothing to rebalance...
            if(m_userData.AssetClassCount == 1)
            {
                toggleSwitchRebalance.IsOn = false;
            }

            SaveInput();

        Exit:
            if(!retVal)
            {
                ShowDialog(message);
                Logger.Log(funcName + ": " + message);
            }
            else
            {
                FormatInputStrings();

                Logger.Log(funcName + "         : ----- INPUT -----");
                Logger.Log(funcName + "         : PortVal.    = " + (m_userData.PortVal).ToString("C", m_formatInfo));
                Logger.Log(funcName + "         : Time        = " + (m_userData.Years).ToString("N"  , m_formatInfo));
                Logger.Log(funcName + "         : Backtest    = " + m_backTest);

                if (m_backTest                      )   Logger.Log(funcName +  "         : Hist. Inf.  = " + m_backTestDlg.UseHistorialInflation);
                if (m_userData.AnnContr          > 0)   Logger.Log(funcName +  "         : AnnContr.   = " + (m_userData.AnnContr).ToString("C"      , m_formatInfo));
                if (m_userData.AnnWithdrwlDlr    > 0)   Logger.Log(funcName +  "         : AnnWthdrwl. = " + (m_userData.AnnWithdrwlDlr).ToString("C", m_formatInfo));

                m_formatInfo.NumberDecimalDigits = 2; //change temporarly

                if (m_userData.Inflation        != 0)    Logger.Log(funcName + "         : Inf.        = " + (m_userData.Inflation         / 100).ToString("P", m_formatInfo));
                if (m_userData.FeesPerct         > 0)    Logger.Log(funcName + "         : Fees        = " + (m_userData.FeesPerct         / 100).ToString("P", m_formatInfo));
                                                         Logger.Log(funcName + "         : Rebalance   = " + m_userData.Rebalance);
                                                         Logger.Log(funcName + "         : Glide       = " + m_userData.Glide);
                if (m_userData.Glide                )    Logger.Log(funcName + "         : Glide Perct.= " + (m_userData.GlidePerct        / 100).ToString("P", m_formatInfo));

                if (m_userData.TotalMarketAlloc  > 0)    Logger.Log(funcName + "         : Total       = " + (m_userData.TotalMarketAlloc  / 100).ToString("P", m_formatInfo));
                if (m_userData.LargeCapAlloc     > 0)    Logger.Log(funcName + "         : Large       = " + (m_userData.LargeCapAlloc     / 100).ToString("P", m_formatInfo));
                if (m_userData.SmallCapAlloc     > 0)    Logger.Log(funcName + "         : Small       = " + (m_userData.SmallCapAlloc     / 100).ToString("P", m_formatInfo));
                if (m_userData.SmallCapValAlloc  > 0)    Logger.Log(funcName + "         : S-val       = " + (m_userData.SmallCapValAlloc  / 100).ToString("P", m_formatInfo));
                if (m_userData.IntlAlloc         > 0)    Logger.Log(funcName + "         : Intl        = " + (m_userData.IntlAlloc         / 100).ToString("P", m_formatInfo));
                if (m_userData.IntlSmallCapAlloc > 0)    Logger.Log(funcName + "         : Intl Small  = " + (m_userData.IntlSmallCapAlloc / 100).ToString("P", m_formatInfo));
                if (m_userData.EmerAlloc         > 0)    Logger.Log(funcName + "         : Emer        = " + (m_userData.EmerAlloc         / 100).ToString("P", m_formatInfo));
                if (m_userData.AggBondAlloc      > 0)    Logger.Log(funcName + "         : Agg         = " + (m_userData.AggBondAlloc      / 100).ToString("P", m_formatInfo));
                if (m_userData.Trsry10YrAlloc    > 0)    Logger.Log(funcName + "         : Trsry       = " + (m_userData.Trsry10YrAlloc    / 100).ToString("P", m_formatInfo));
                if (m_userData.Bill90Alloc       > 0)    Logger.Log(funcName + "         : 90-bill     = " + (m_userData.Bill90Alloc       / 100).ToString("P", m_formatInfo));
                if (m_userData.LongCorpAlloc     > 0)    Logger.Log(funcName + "         : L-corp      = " + (m_userData.LongCorpAlloc     / 100).ToString("P", m_formatInfo));
                if (m_userData.MunisAlloc        > 0)    Logger.Log(funcName + "         : Munis       = " + (m_userData.MunisAlloc        / 100).ToString("P", m_formatInfo));
                if (m_userData.ReitAlloc         > 0)    Logger.Log(funcName + "         : Reit        = " + (m_userData.ReitAlloc         / 100).ToString("P", m_formatInfo));
                if (m_userData.GoldAlloc         > 0)    Logger.Log(funcName + "         : Gold        = " + (m_userData.GoldAlloc         / 100).ToString("P", m_formatInfo));
                if (m_userData.CmdtyAlloc        > 0)    Logger.Log(funcName + "         : Cmdty       = " + (m_userData.CmdtyAlloc        / 100).ToString("P", m_formatInfo));
                Logger.Log(funcName + "         : ------------------");

                m_formatInfo.NumberDecimalDigits = 0; //restore
            }

            return retVal;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FormatInputStrings()
        {
            if (textBoxAnnualContr.Text       != "")  textBoxAnnualContr.Text      = (Convert.ToDouble(textBoxAnnualContr.Text)).ToString("C"           , m_formatInfo);
            if (textBoxWithdrwl.Text          != "")  textBoxWithdrwl.Text         = (Convert.ToDouble(textBoxWithdrwl.Text)).ToString("C"              , m_formatInfo);
            if (textBoxPortVal.Text           != "")  textBoxPortVal.Text          = (Convert.ToDouble(textBoxPortVal.Text)).ToString("C"               , m_formatInfo);
            if (textBoxInflation.Text         != "")  textBoxInflation.Text        = (Convert.ToDouble(textBoxInflation.Text)         / 100).ToString("P", m_formatInfo);
            if (textBoxFeesPerct.Text         != "")  textBoxFeesPerct.Text        = (Convert.ToDouble(textBoxFeesPerct.Text)         / 100).ToString("P", m_formatInfo);

            m_formatInfo.PercentDecimalDigits = 0; //change to 0 decimal point temporarly

            //stocks
            if (textBoxTotalMarketAlloc.Text  != "") textBoxTotalMarketAlloc.Text  = (Convert.ToDouble(textBoxTotalMarketAlloc.Text)  / 100).ToString("P", m_formatInfo);
            if (textBoxLargeCapAlloc.Text     != "") textBoxLargeCapAlloc.Text     = (Convert.ToDouble(textBoxLargeCapAlloc.Text)     / 100).ToString("P", m_formatInfo);
            if (textBoxSmallCapAlloc.Text     != "") textBoxSmallCapAlloc.Text     = (Convert.ToDouble(textBoxSmallCapAlloc.Text)     / 100).ToString("P", m_formatInfo);
            if (textBoxSmallValAlloc.Text     != "") textBoxSmallValAlloc.Text     = (Convert.ToDouble(textBoxSmallValAlloc.Text)     / 100).ToString("P", m_formatInfo);
            //intl
            if (textBoxIntlAlloc.Text         != "") textBoxIntlAlloc.Text         = (Convert.ToDouble(textBoxIntlAlloc.Text)         / 100).ToString("P", m_formatInfo);
            if (textBoxIntlSmallCapAlloc.Text != "") textBoxIntlSmallCapAlloc.Text = (Convert.ToDouble(textBoxIntlSmallCapAlloc.Text) / 100).ToString("P", m_formatInfo);
            if (textBoxEmerAlloc.Text         != "") textBoxEmerAlloc.Text         = (Convert.ToDouble(textBoxEmerAlloc.Text)         / 100).ToString("P", m_formatInfo);
            //bonds
            if (textBoxAggBondAlloc.Text      != "") textBoxAggBondAlloc.Text      = (Convert.ToDouble(textBoxAggBondAlloc.Text)      / 100).ToString("P", m_formatInfo);
            if (textBox90DayBillAlloc.Text    != "") textBox90DayBillAlloc.Text    = (Convert.ToDouble(textBox90DayBillAlloc.Text)    / 100).ToString("P", m_formatInfo);
            if (textBox10YrTresAlloc.Text     != "") textBox10YrTresAlloc.Text     = (Convert.ToDouble(textBox10YrTresAlloc.Text)     / 100).ToString("P", m_formatInfo);
            if (textBoxLongCorpAlloc.Text     != "") textBoxLongCorpAlloc.Text     = (Convert.ToDouble(textBoxLongCorpAlloc.Text)     / 100).ToString("P", m_formatInfo);
            if (textBoxMuniAlloc.Text         != "") textBoxMuniAlloc.Text         = (Convert.ToDouble(textBoxMuniAlloc.Text)         / 100).ToString("P", m_formatInfo);
            //alternative
            if (textBoxGoldAlloc.Text         != "") textBoxGoldAlloc.Text         = (Convert.ToDouble(textBoxGoldAlloc.Text)         / 100).ToString("P", m_formatInfo);
            if (textBoxReitAlloc.Text         != "") textBoxReitAlloc.Text         = (Convert.ToDouble(textBoxReitAlloc.Text)         / 100).ToString("P", m_formatInfo);
            if (textBoxCmdtyAlloc.Text        != "") textBoxCmdtyAlloc.Text        = (Convert.ToDouble(textBoxCmdtyAlloc.Text)        / 100).ToString("P", m_formatInfo);

            m_formatInfo.PercentDecimalDigits = 2; //restore decimal points
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnformatInputStrings()
        {
            //input
            textBoxAnnualContr.Text      = textBoxAnnualContr.Text.Replace("$", "");
            textBoxAnnualContr.Text      = textBoxAnnualContr.Text.Replace(",", "");
            textBoxWithdrwl.Text         = textBoxWithdrwl.Text.Replace("$"   , "");
            textBoxWithdrwl.Text         = textBoxWithdrwl.Text.Replace(","   , "");
            textBoxPortVal.Text          = textBoxPortVal.Text.Replace("$"    , "");
            textBoxPortVal.Text          = textBoxPortVal.Text.Replace(","    , "");

            //check for non-empty cause Convert.ToDouble will crash on non-numeric values
            if (textBoxInflation.Text   != HISTORICAL_INFLATION) textBoxInflation.Text = textBoxInflation.Text.Replace("%", "");
                                                                 textBoxFeesPerct.Text = textBoxFeesPerct.Text.Replace("%", "");
            //stocks
            textBoxTotalMarketAlloc.Text  = textBoxTotalMarketAlloc.Text.Replace("%" , "");
            textBoxLargeCapAlloc.Text     = textBoxLargeCapAlloc.Text.Replace("%"    , "");
            textBoxSmallCapAlloc.Text     = textBoxSmallCapAlloc.Text.Replace("%"    , "");
            textBoxSmallValAlloc.Text     = textBoxSmallValAlloc.Text.Replace("%"    , "");
            //bonds
            textBoxAggBondAlloc.Text      = textBoxAggBondAlloc.Text.Replace("%"     , "");
            textBox90DayBillAlloc.Text    = textBox90DayBillAlloc.Text.Replace("%"   , "");
            textBox10YrTresAlloc.Text     = textBox10YrTresAlloc.Text.Replace("%"    , "");
            textBoxLongCorpAlloc.Text     = textBoxLongCorpAlloc.Text.Replace("%"    , "");
            textBoxMuniAlloc.Text         = textBoxMuniAlloc.Text.Replace("%"        , "");
            //intl
            textBoxIntlAlloc.Text         = textBoxIntlAlloc.Text.Replace("%"        , "");
            textBoxIntlSmallCapAlloc.Text = textBoxIntlSmallCapAlloc.Text.Replace("%", "");
            textBoxEmerAlloc.Text         = textBoxEmerAlloc.Text.Replace("%"        , "");
            //alternative
            textBoxGoldAlloc.Text         = textBoxGoldAlloc.Text.Replace("%"        , "");
            textBoxReitAlloc.Text         = textBoxReitAlloc.Text.Replace("%"        , "");
            textBoxCmdtyAlloc.Text        = textBoxCmdtyAlloc.Text.Replace("%"       , "");
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShowDescription(Portfolio portfolio)
        {
            m_yearZero = 0;

            for (int i = 1; i < m_userData.Years; i++)
            {
                if(portfolio.GetBalanceAt(i) < SUCCESS_RATE_MIN)
                {
                    m_yearZero = i+1;
                    break;
                }
            }

            if (m_SD < RISK_MODERATE_SD_BEGIN)
            {
                m_riskLevel = "low";
            }
            else if (m_SD >= RISK_MODERATE_SD_BEGIN && m_SD < RISK_MODERATE_SD_END)
            {
                m_riskLevel = "moderate";
            }
            else if (m_SD > RISK_MODERATE_SD_END)
            {
                m_riskLevel = "high";
            }
            else
            {
                Debug.Assert(false);
            }

            if (!m_backTest)
            {
                m_desc.Add(String.Format("{0} portfolios have been simulated using your input. Average of the portfolio ending balances is {1} and the median ending balance is {2}. Following are based on the median portfolio:\n",
                                     RUN_COUNT.ToString("N", m_formatInfo), m_portValMean.ToString("C", m_formatInfo), textBoxMed.Text));
            }
            else
            {
                string msgTrimmed = (m_trimmed) ? "Backtest start date was moved forward because one or more asset classes do not have enough historical returns." : "";

                m_desc.Add(String.Format(" - Your portfolio has been backtested using the returns from {0} to {1}. " + msgTrimmed + "\n", m_backTestDlg.StartYear, m_backTestDlg.EndYear));
                m_desc.Add(String.Format(" - Your portfolio's ending balance is {0}.\n", textBoxMed.Text));
            }

            if (m_yearZero == 0)
            {
                m_desc.Add(String.Format(" - Your total gain/loss is {0}, which indicates {1} of cumulative (total) return.\n", textBoxGainLoss.Text, textBoxCummRet.Text));
                m_desc.Add(String.Format(" - Your annualized return (compound annual growth rate) is {0}.\n", textBoxAnnRet.Text));
            }
            else
            {
                m_desc.Add(" - Your portfolio ran out of money in " + m_yearZero + " years.\n");
            }

            if(m_backTest && m_backTestDlg.UseHistorialInflation)
            {
                double avgInf = 0;

                for(int i = 0; i < m_userData.Years; i++)
                {
                    avgInf += Data.CPI[Data.CPI.Count - m_userData.Years + i];
                }

                avgInf = (avgInf / m_userData.Years) / 100;

                m_desc.Add(String.Format(" - In this time period average inflation was {0}.\n", avgInf.ToString("P", m_formatInfo)));
            }

            if (m_userData.FeesPerct > 0)
            {
                int years = (m_yearZero == 0) ? m_userData.Years : m_yearZero;

                m_desc.Add(String.Format(" - In {0} years, you spent a total of {1} on fees/expenses.\n", years, textBoxFeesDlr.Text));
            }

            if (m_SD > 0)
            {
                m_desc.Add(String.Format(" - The standard deviation of {0} indicates that your portfolio's risk/volatility is {1}.\n", textBoxStdDev.Text, m_riskLevel));
            }

            if (m_MDD < 0 && m_MDD != -1) //-1 means lost all money
            {
                string years = "years";

                if (m_MDDEnd - m_MDDBegin == 1)
                {
                    years = "year";
                }

                m_desc.Add(String.Format(" - The largest drop in portfolio value (from year {0} through year {1}) is {2} or {3}. This decline lasted {4} {5}.\n", 
                                         m_MDDBegin, m_MDDEnd, m_MDDLoss.ToString("C", m_formatInfo), textBoxMDD.Text, m_MDDEnd - m_MDDBegin, years));
            }

            if(!m_backTest)
            {
                m_desc.Add(String.Format(" - {0} of portfolios finished simulation with a balance greater than {1}.", textBoxSuccess.Text, SUCCESS_RATE_MIN.ToString("C", m_formatInfo)));
            }

            textBoxDesc.Text = "";

            for (int i=0; i<m_desc.Count; i++)
            {
                textBoxDesc.Text += m_desc[i] + "\n";
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleButtonClearClicked()
        {
            //input - investor
            textBoxPortVal.Text           = "";
            textBoxYears.Text             = "";
            textBoxAnnualContr.Text       = "";
            textBoxWithdrwl.Text          = "";
            textBoxFeesPerct.Text         = "";
            textBoxInflation.IsReadOnly   = false;
            textBoxInflation.Text         = "";
            toggleSwitchRebalance.IsOn    = false;
            toggleSwitchGlide.IsOn        = false;
            textBoxGlide.Text             = "";

            //bonds                      
            textBoxAggBondAlloc.Text      = "";
            textBox90DayBillAlloc.Text    = "";
            textBoxLongCorpAlloc.Text     = "";
            textBoxMuniAlloc.Text         = "";
            textBox10YrTresAlloc.Text     = "";
            //stocks
            textBoxTotalMarketAlloc.Text  = "";
            textBoxLargeCapAlloc.Text     = "";
            textBoxSmallCapAlloc.Text     = "";
            textBoxSmallValAlloc.Text     = "";
            //intl
            textBoxIntlAlloc.Text         = "";
            textBoxIntlSmallCapAlloc.Text = "";
            textBoxEmerAlloc.Text         = "";
            //alternative
            textBoxReitAlloc.Text         = "";
            textBoxGoldAlloc.Text         = "";
            textBoxCmdtyAlloc.Text        = "";

            //output
            textBoxMin.Text               = "";
            textBoxMed.Text               = "";
            textBoxMed.Header             = TEXTBOX_MEDIAN_TEXT_MEDIAN;
            textBoxMax.Text               = "";
            labelMedianResults.Text       = LABEL_MEDIAN_TEXT_MEDIAN;
            textBoxGainLoss.Text          = "";
            textBoxCummRet.Text           = "";
            textBoxAnnRet.Text            = "";
            textBoxFeesDlr.Text           = "";
            textBox25Perctl.Text          = "";
            textBox75Perctl.Text          = "";
            textBoxStdDev.Text            = "";
            textBoxMDD.Text               = "";
            textBoxSuccess.Text           = "";
            textBoxTotalAlloc.Text        = "";
            textBoxDesc.Text              = "";
            textBoxTotalAlloc.FontWeight  = FontWeights.Normal;

            chartGrowth.Visibility        = Visibility.Collapsed;
            chartBackTest.Visibility      = Visibility.Collapsed;
            chartAlloc.Visibility         = Visibility.Collapsed;

            m_backTestDlg                 = null;
            m_backTest                    = false;
            m_trimmed                     = false; 
        }


        private double GetTotalAlloc()
        {
            double retVal = 0;

                     //stocks
            retVal = m_userData.LargeCapAlloc     +
                     m_userData.TotalMarketAlloc  +
                     m_userData.SmallCapAlloc     +
                     m_userData.SmallCapValAlloc  +
                     //bonds
                     m_userData.AggBondAlloc      +
                     m_userData.Bill90Alloc       +
                     m_userData.LongCorpAlloc     +
                     m_userData.Trsry10YrAlloc    +
                     m_userData.MunisAlloc        +
                     //intl
                     m_userData.IntlAlloc         +
                     m_userData.IntlSmallCapAlloc +
                     m_userData.EmerAlloc         +
                     //alernative
                     m_userData.ReitAlloc         +
                     m_userData.GoldAlloc         +
                     m_userData.CmdtyAlloc;


            if (retVal > 99.9 && retVal <= 100.1)
            {
                retVal = 100;
            }

            return retVal;
        }


        //NOTE: portfolios must be sorted
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetSuccessRate(List<Portfolio> portfolios)
        {
            double zeroCount = 0;

            for (int i = 0; i < portfolios.Count; i++)
            {
                List<double> temp = portfolios[i].GetBalanceList();

                if (temp[temp.Count-1] < SUCCESS_RATE_MIN)
                {
                    zeroCount++;
                }
                else
                {
                    break;
                }
            }

            return (RUN_COUNT-zeroCount) / portfolios.Count;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetSD(List<double> list)
        {
            List<double> deviations = new List<double>(list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                deviations.Add(list[i] - list.Average());
                deviations[i] *= deviations[i];
            }

            return Math.Sqrt(deviations.Sum() / (deviations.Count - 1));
        }


        /* 
         Max drawdown (MDD) = The largest single drop from peak to bottom in portfolio value.

           Yr.   Port. Val.
            1	  $500,000
            2	  $750,000   peak
            3	  $400,000
            4	  $600,000
            5	  $350,000   bottom
            6	  $800,000
            7	  $790,000

            MDD = (bottom-peak) / peak
        */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetMDD(List<double> list)
        {
            double peak        = list[0];
            double bottom      = list[0];
            int    peakIndex   = 0;
            int    bottomIndex = 0;
            double mdd         = 0;
            m_MDDBegin         = 0;
            m_MDDEnd           = 0;
            m_MDDLoss          = 0;

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i] >= peak)
                {
                    peak      = list[i];
                    peakIndex = i;
                }
                //if peak is found, new bottom can be higher than prev. bottom
                else if (list[i] <= bottom || peakIndex > bottomIndex)
                {
                    bottom      = list[i];
                    bottomIndex = i;
                }

                //peak must come before bottom
                if (bottomIndex > peakIndex)
                {
                    double tempMDDLoss = list[peakIndex] - list[bottomIndex];

                    if (tempMDDLoss > m_MDDLoss) //only save the loss if it's greater
                    {
                        mdd        = (bottom - peak) / peak;
                        m_MDDBegin = peakIndex   + 1;
                        m_MDDEnd   = bottomIndex + 1;
                        m_MDDLoss  = tempMDDLoss;
                    }
                }
            }

            return mdd;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RandomizeReturns()
        {
            ClearReturns();

            for (int i = 0; i < m_userData.Years; i++)
            {
                //stocks
                if (m_userData.TotalMarketAlloc  > 0) m_totalMarketRets.Add(Data.TOTAL_MARKET[m_rand.Next(0 , Data.TOTAL_MARKET.Count)]);
                if (m_userData.LargeCapAlloc     > 0) m_largeCapRets.Add(Data.LARGE_CAP[m_rand.Next(0       , Data.LARGE_CAP.Count)]);
                if (m_userData.SmallCapAlloc     > 0) m_smallCapRets.Add(Data.SMALL_CAP[m_rand.Next(0       , Data.SMALL_CAP.Count)]);
                if (m_userData.SmallCapValAlloc  > 0) m_smallCapValRets.Add(Data.SMALL_CAP_VAL[m_rand.Next(0, Data.SMALL_CAP_VAL.Count)]);
                //bonds
                if (m_userData.AggBondAlloc      > 0) m_aggBondRets.Add(Data.AGG_BOND[m_rand.Next(0         , Data.AGG_BOND.Count)]);
                if (m_userData.Bill90Alloc       > 0) m_bills90Rets.Add(Data.BILLS_90_DAYS[m_rand.Next(0    , Data.BILLS_90_DAYS.Count)]);
                if (m_userData.Trsry10YrAlloc    > 0) m_trsry10Rets.Add(Data.TRSRY_10_YR[m_rand.Next(0      , Data.TRSRY_10_YR.Count)]);
                if (m_userData.LongCorpAlloc     > 0) m_longCorpRets.Add(Data.LONG_CORP_BOND[m_rand.Next(0  , Data.LONG_CORP_BOND.Count)]);
                if (m_userData.MunisAlloc        > 0) m_munisRets.Add(Data.MUNIS[m_rand.Next(0              , Data.MUNIS.Count)]);
                //intl
                if (m_userData.IntlAlloc         > 0) m_intlRets.Add(Data.INTL_EAFE[m_rand.Next(0           , Data.INTL_EAFE.Count)]);
                if (m_userData.IntlSmallCapAlloc > 0) m_intlSmallCapRets.Add(Data.INTL_SMALL[m_rand.Next(0  , Data.INTL_SMALL.Count)]);
                if (m_userData.EmerAlloc         > 0) m_emerRets.Add(Data.INTL_EMER[m_rand.Next(0           , Data.INTL_EMER.Count)]);
                //alternative
                if (m_userData.ReitAlloc         > 0) m_reitRets.Add(Data.REIT[m_rand.Next(0                , Data.REIT.Count)]);
                if (m_userData.GoldAlloc         > 0) m_goldRets.Add(Data.GOLD[m_rand.Next(0                , Data.GOLD.Count)]);
                if (m_userData.CmdtyAlloc        > 0) m_cmdtyRets.Add(Data.CMDTY[m_rand.Next(0              , Data.CMDTY.Count)]);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetBackTestReturns()
        {
            Debug.Assert(m_backTest || m_backTestDlg != null);
            ClearReturns();

            int length = m_backTestDlg.EndYear - m_backTestDlg.StartYear;

            //stocks
            if(m_userData.TotalMarketAlloc > 0 && length > Data.TOTAL_MARKET.Count)
            {
                length    = Data.TOTAL_MARKET.Count; //first time, just overwrite length
                m_trimmed = true;
            }

            if(m_userData.LargeCapAlloc > 0 && length > Data.LARGE_CAP.Count)
            {
                if(!m_trimmed)
                {
                    length = Data.LARGE_CAP.Count;
                }
                else if (Data.LARGE_CAP.Count <= length)
                {
                    length = Data.LARGE_CAP.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.SmallCapAlloc > 0 && length > Data.SMALL_CAP.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.SMALL_CAP.Count;
                }
                else if (Data.SMALL_CAP.Count <= length)
                {
                    length = Data.SMALL_CAP.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.SmallCapValAlloc > 0 && length > Data.SMALL_CAP_VAL.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.SMALL_CAP_VAL.Count;
                }
                else if (Data.SMALL_CAP_VAL.Count <= length)
                {
                    length = Data.SMALL_CAP_VAL.Count;
                }

                m_trimmed = true;
            }

            //bonds
            if (m_userData.AggBondAlloc > 0 && length > Data.AGG_BOND.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.AGG_BOND.Count;
                }
                else if (Data.AGG_BOND.Count <= length)
                {
                    length = Data.AGG_BOND.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.Bill90Alloc > 0 && length > Data.BILLS_90_DAYS.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.BILLS_90_DAYS.Count;
                }
                else if (Data.BILLS_90_DAYS.Count <= length)
                {
                    length = Data.BILLS_90_DAYS.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.Trsry10YrAlloc > 0 && length > Data.TRSRY_10_YR.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.TRSRY_10_YR.Count;
                }
                else if (Data.TRSRY_10_YR.Count <= length)
                {
                    length = Data.TRSRY_10_YR.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.LongCorpAlloc > 0 && length > Data.LONG_CORP_BOND.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.LONG_CORP_BOND.Count;
                }
                else if (Data.LONG_CORP_BOND.Count <= length)
                {
                    length = Data.LONG_CORP_BOND.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.MunisAlloc > 0 && length > Data.MUNIS.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.MUNIS.Count;
                }
                else if (Data.MUNIS.Count <= length)
                {
                    length = Data.MUNIS.Count;
                }

                m_trimmed = true;
            }

            //intl
            if (m_userData.IntlAlloc > 0 && length > Data.INTL_EAFE.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.INTL_EAFE.Count;
                }
                else if (Data.INTL_EAFE.Count <= length)
                {
                    length = Data.INTL_EAFE.Count;
                }

                m_trimmed = true;
            }

            //intl small
            if (m_userData.IntlSmallCapAlloc > 0 && length > Data.INTL_SMALL.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.INTL_SMALL.Count;
                }
                else if (Data.INTL_SMALL.Count <= length)
                {
                    length = Data.INTL_SMALL.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.EmerAlloc > 0 && length > Data.INTL_EMER.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.INTL_EMER.Count;
                }
                else if (Data.INTL_EMER.Count <= length)
                {
                    length = Data.INTL_EMER.Count;
                }

                m_trimmed = true;
            }

            //alternative
            if (m_userData.ReitAlloc > 0 && length > Data.REIT.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.REIT.Count;
                }
                else if (Data.REIT.Count <= length)
                {
                    length = Data.REIT.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.GoldAlloc > 0 && length > Data.GOLD.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.GOLD.Count;
                }
                else if (Data.GOLD.Count <= length)
                {
                    length = Data.GOLD.Count;
                }

                m_trimmed = true;
            }

            if (m_userData.CmdtyAlloc > 0 && length > Data.CMDTY.Count)
            {
                if (!m_trimmed)
                {
                    length = Data.CMDTY.Count;
                }
                else if (Data.CMDTY.Count <= length)
                {
                    length = Data.CMDTY.Count;
                }

                m_trimmed = true;
            }

            m_userData.Years  = length;
            textBoxYears.Text = Convert.ToString(length);

            if (m_trimmed)
            {
                m_backTestDlg.StartYear = m_backTestDlg.EndYear - length;
            }

            for (int i = 0; i < m_userData.Years; i++)
            {
                //stocks
                if (m_userData.TotalMarketAlloc  > 0)    m_totalMarketRets.Add(Data.TOTAL_MARKET[Data.TOTAL_MARKET.Count   - m_userData.Years + i]);
                if (m_userData.LargeCapAlloc     > 0)    m_largeCapRets.Add(Data.LARGE_CAP[Data.LARGE_CAP.Count            - m_userData.Years + i]);
                if (m_userData.SmallCapAlloc     > 0)    m_smallCapRets.Add(Data.SMALL_CAP[Data.SMALL_CAP.Count            - m_userData.Years + i]);
                if (m_userData.SmallCapValAlloc  > 0)    m_smallCapValRets.Add(Data.SMALL_CAP_VAL[Data.SMALL_CAP_VAL.Count - m_userData.Years + i]);
                //bonds
                if (m_userData.AggBondAlloc      > 0)    m_aggBondRets.Add(Data.AGG_BOND[Data.AGG_BOND.Count               - m_userData.Years + i]);
                if (m_userData.Bill90Alloc       > 0)    m_bills90Rets.Add(Data.BILLS_90_DAYS[Data.BILLS_90_DAYS.Count     - m_userData.Years + i]);
                if (m_userData.Trsry10YrAlloc    > 0)    m_trsry10Rets.Add(Data.TRSRY_10_YR[Data.TRSRY_10_YR.Count         - m_userData.Years + i]);
                if (m_userData.LongCorpAlloc     > 0)    m_longCorpRets.Add(Data.LONG_CORP_BOND[Data.LONG_CORP_BOND.Count  - m_userData.Years + i]);
                if (m_userData.MunisAlloc        > 0)    m_munisRets.Add(Data.MUNIS[Data.MUNIS.Count                       - m_userData.Years + i]);
                //intl
                if (m_userData.IntlAlloc         > 0)    m_intlRets.Add(Data.INTL_EAFE[Data.INTL_EAFE.Count                - m_userData.Years + i]);
                if (m_userData.IntlSmallCapAlloc > 0)    m_intlSmallCapRets.Add(Data.INTL_SMALL[Data.INTL_SMALL.Count      - m_userData.Years + i]);
                if (m_userData.EmerAlloc         > 0)    m_emerRets.Add(Data.INTL_EMER[Data.INTL_EMER.Count                - m_userData.Years + i]);
                //alternative
                if (m_userData.ReitAlloc         > 0)    m_reitRets.Add(Data.REIT[Data.REIT.Count                          - m_userData.Years + i]);
                if (m_userData.GoldAlloc         > 0)    m_goldRets.Add(Data.GOLD[Data.GOLD.Count                          - m_userData.Years + i]);
                if (m_userData.CmdtyAlloc        > 0)    m_cmdtyRets.Add(Data.CMDTY[Data.CMDTY.Count                       - m_userData.Years + i]);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearReturns()
        {
            //stocks
            if(m_totalMarketRets.Count  > 0) m_totalMarketRets.Clear();
            if(m_largeCapRets.Count     > 0) m_largeCapRets.Clear();
            if(m_smallCapRets.Count     > 0) m_smallCapRets.Clear();
            if(m_smallCapValRets.Count  > 0) m_smallCapValRets.Clear();
            //intl
            if(m_intlRets.Count         > 0) m_intlRets.Clear();
            if(m_intlSmallCapRets.Count > 0) m_intlSmallCapRets.Clear();
            if(m_emerRets.Count         > 0) m_emerRets.Clear();
            //bonds
            if(m_aggBondRets.Count      > 0) m_aggBondRets.Clear();
            if(m_trsry10Rets.Count      > 0) m_trsry10Rets.Clear();
            if(m_bills90Rets.Count      > 0) m_bills90Rets.Clear();
            if(m_longCorpRets.Count     > 0) m_longCorpRets.Clear();
            if(m_munisRets.Count        > 0) m_munisRets.Clear();
            //alternative
            if(m_reitRets.Count         > 0) m_reitRets.Clear();
            if(m_goldRets.Count         > 0) m_goldRets.Clear();
            if(m_cmdtyRets.Count        > 0) m_cmdtyRets.Clear();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleButtonRunClick()
        {
            if (!VerifyInput())
            {
                return;
            }

            const string funcName      = "HandleButtonRunClick";
            int          indexMedian   = 0;
            int          index25Perctl = 0;
            int          index75Perctl = 0;
            m_feesDlr                  = 0;
            m_backTestPortfolio        = null;
            m_portMedian               = null;   //median port.   , taken from m_portfolios
            m_port75Perctl             = null;   //75 perct. port., taken from m_portfolios
            m_port25Perctl             = null;   //25 perct. port., taken from m_portfolios
            m_cummRets.Clear();                  //list of port. ending values
            m_portfolios.Clear();                //list of all portfolios
            m_portMedianRets.Clear();            //annual returns of med. portf.
            chartGrowth.Series.Clear();
            chartBackTest.Series.Clear();
            chartAlloc.Series.Clear();
            m_desc.Clear();

            //these may have been changed because of backtest
            textBoxMed.Header           = (m_backTest) ? TEXTBOX_MEDIAN_TEXT_BACKTEST : TEXTBOX_MEDIAN_TEXT_MEDIAN;
            labelMedianResults.Text     = (m_backTest) ? LABEL_MEDIAN_TEXT_BACKTEST   : LABEL_MEDIAN_TEXT_MEDIAN;
            textBoxInflation.IsReadOnly = false;

            if (m_backTest)
            {
                if(m_backTestDlg.UseHistorialInflation)
                {
                    textBoxInflation.Text       = HISTORICAL_INFLATION;
                    textBoxInflation.IsReadOnly = true;
                }

                m_trimmed = false;

                GetBackTestReturns(); //we call this instead of RandomizeReturns()
                GetCummRet(m_userData.PortVal);
                m_backTestPortfolio = m_portfolios[0];

                //todo: dup code, see below
                for (int i = 0; i < m_userData.Years; i++)
                {
                    if (i == 0)
                    {
                        m_feesDlr = Convert.ToInt32(m_backTestPortfolio.GetBalanceAt(i) * (m_userData.FeesPerct / 100));
                        continue;
                    }

                    if (m_backTestPortfolio.GetBalanceAt(i) == 0)
                    {
                        continue;
                    }

                    m_portMedianRets.Add((m_backTestPortfolio.GetBalanceAt(i) - m_backTestPortfolio.GetBalanceAt(i-1)) / m_backTestPortfolio.GetBalanceAt(i-1));

                    if (m_userData.FeesPerct > 0)
                    {
                        m_feesDlr += Convert.ToInt32(m_backTestPortfolio.GetBalanceAt(i) * (m_userData.FeesPerct / 100));
                    }
                }

                m_retDollar = m_backTestPortfolio.Value - m_userData.PortVal;
                m_retPerct  = m_retDollar / m_userData.PortVal;
                m_retAnn    = Math.Pow(m_backTestPortfolio.Value / m_userData.PortVal, 1 / Convert.ToDouble(m_userData.Years)) - 1;
                m_MDD       = GetMDD(m_backTestPortfolio.GetBalanceList());
                m_SD        = GetSD(m_portMedianRets);

                textBoxMin.Text       = "N/A";
                textBoxMax.Text       = "N/A";
                textBox25Perctl.Text  = "N/A";
                textBox75Perctl.Text  = "N/A";
                textBoxSuccess.Text   = "N/A";
                textBoxMed.Text       = m_backTestPortfolio.Value.ToString("C", m_formatInfo);
                textBoxStdDev.Text    = m_SD.ToString("P"       , m_formatInfo);
                textBoxMDD.Text       = m_MDD.ToString("P"      , m_formatInfo);
                textBoxGainLoss.Text  = m_retDollar.ToString("C", m_formatInfo);
                textBoxCummRet.Text   = m_retPerct.ToString("P" , m_formatInfo);
                textBoxAnnRet.Text    = m_retAnn.ToString("P"   , m_formatInfo);
                textBoxFeesDlr.Text   = m_feesDlr.ToString("C"  , m_formatInfo);

                ShowDescription(m_backTestPortfolio);

                DrawLineChart(m_backTestPortfolio.GetBalanceList(), "Portfolio");
                DrawPieChart(m_backTestPortfolio);

                goto Exit;
            }

            for (int i = 0; i < RUN_COUNT; i++)
            {
                RandomizeReturns();
                m_cummRets.Add(GetCummRet(m_userData.PortVal));
            }

            m_cummRets.Sort();
            m_portfolios     = m_portfolios.OrderBy(x => x.Value-1).ToList();
            indexMedian      = (m_portfolios.Count / 2) - 1;
            index75Perctl    = (m_portfolios.Count -  (m_portfolios.Count / 4)) - 1;
            index25Perctl    = (m_portfolios.Count - ((m_portfolios.Count / 4)  * 3)) - 1;
            m_portValMin     = m_cummRets.Min();
            m_portValMed     = m_cummRets[(m_cummRets.Count / 2) - 1];
            m_portValMax     = m_cummRets.Max();
            m_portValMean    = m_cummRets.Average();
            m_portMedian     = m_portfolios[indexMedian];
            m_port75Perctl   = m_portfolios[index75Perctl];
            m_port25Perctl   = m_portfolios[index25Perctl];
            m_retDollar      = m_portValMed - m_userData.PortVal;
            m_retPerct       = m_retDollar  / m_userData.PortVal;
            m_retAnn         = Math.Pow(m_portValMed / m_userData.PortVal, 1 / Convert.ToDouble(m_userData.Years)) - 1;
            m_MDD            = GetMDD(m_portMedian.GetBalanceList());
            m_success        = GetSuccessRate(m_portfolios);

            //todo: dup code, see above
            for (int i = 0; i < m_userData.Years; i++)
            {
                if (i == 0)
                {
                    m_feesDlr = Convert.ToInt32(m_portMedian.GetBalanceAt(i) * (m_userData.FeesPerct / 100));
                    continue;
                }

                if (m_portMedian.GetBalanceAt(i) == 0)
                {
                    continue;
                }

                m_portMedianRets.Add((m_portMedian.GetBalanceAt(i) - m_portMedian.GetBalanceAt(i-1)) / m_portMedian.GetBalanceAt(i-1));

                if (m_userData.FeesPerct > 0)
                {
                    m_feesDlr += Convert.ToInt32(m_portMedian.GetBalanceAt(i) * (m_userData.FeesPerct / 100));
                }
            }

            m_SD                  = GetSD(m_portMedianRets);
            textBoxMin.Text       = m_portValMin.ToString("C", m_formatInfo);
            textBoxMed.Text       = m_portValMed.ToString("C", m_formatInfo);
            textBoxMax.Text       = m_portValMax.ToString("C", m_formatInfo);
            textBox25Perctl.Text  = m_port25Perctl.GetBalanceAt(m_userData.Years - 1).ToString("C", m_formatInfo);
            textBox75Perctl.Text  = m_port75Perctl.GetBalanceAt(m_userData.Years - 1).ToString("C", m_formatInfo);
            textBoxGainLoss.Text  = m_retDollar.ToString("C", m_formatInfo);
            textBoxFeesDlr.Text   = m_feesDlr.ToString("C"  , m_formatInfo);
            textBoxCummRet.Text   = m_retPerct.ToString("P" , m_formatInfo);
            textBoxAnnRet.Text    = m_retAnn.ToString("P"   , m_formatInfo);
            textBoxStdDev.Text    = m_SD.ToString("P"       , m_formatInfo);
            textBoxMDD.Text       = m_MDD.ToString("P"      , m_formatInfo);
            textBoxSuccess.Text   = m_success.ToString("P"  , m_formatInfo);

#if DEBUG
            //verif min, max, 75, 25, median, SD, MDD and success
            if (    m_port25Perctl.GetBalanceAt(m_userData.Years - 1) > m_port75Perctl.GetBalanceAt(m_userData.Years - 1)
                 || m_portValMed                                      > m_port75Perctl.GetBalanceAt(m_userData.Years - 1)
                 || m_portValMed                                      < m_port25Perctl.GetBalanceAt(m_userData.Years - 1)
                 || m_portValMin > m_portValMax
                 || m_portValMed > m_portValMax || m_portValMed < m_portValMin
                 || m_SD         >  1           || m_SD         < -1
                 || m_MDD        >  1           || m_MDD        < -1
                 || m_success    >  1           || m_success    < -1)
            {
                Debugger.Break();
            }
#endif

            ShowDescription(m_portMedian);

            DrawLineChart(m_port75Perctl.GetBalanceList(), "75 percentile");
            DrawLineChart(m_portMedian.GetBalanceList()  , "Median");
            DrawLineChart(m_port25Perctl.GetBalanceList(), "25 percentile");
            DrawPieChart(m_portMedian);

        Exit:
            m_formatInfo.NumberDecimalDigits = 2; //change temporarly

            Portfolio tempPortf = (m_backTest) ? m_backTestPortfolio : m_portMedian;
            Logger.Log(funcName + ": ----- OUTPUT -----");

            if(m_userData.TotalMarketAlloc  > 0) Logger.Log(funcName + ": Total      = " + (tempPortf.TotalMarketAlloc  / 100).ToString("P", m_formatInfo));
            if(m_userData.LargeCapAlloc     > 0) Logger.Log(funcName + ": Large      = " + (tempPortf.LargeCapAlloc     / 100).ToString("P", m_formatInfo));
            if(m_userData.SmallCapAlloc     > 0) Logger.Log(funcName + ": Small      = " + (tempPortf.SmallCapAlloc     / 100).ToString("P", m_formatInfo));
            if(m_userData.SmallCapValAlloc  > 0) Logger.Log(funcName + ": S-val      = " + (tempPortf.SmallCapValAlloc  / 100).ToString("P", m_formatInfo));
            if(m_userData.IntlAlloc         > 0) Logger.Log(funcName + ": Intl       = " + (tempPortf.IntlAlloc         / 100).ToString("P", m_formatInfo));
            if(m_userData.IntlSmallCapAlloc > 0) Logger.Log(funcName + ": Intl Small = " + (tempPortf.IntlSmallAlloc    / 100).ToString("P", m_formatInfo));
            if(m_userData.EmerAlloc         > 0) Logger.Log(funcName + ": Emer       = " + (tempPortf.EmerAlloc         / 100).ToString("P", m_formatInfo));
            if(m_userData.AggBondAlloc      > 0) Logger.Log(funcName + ": Agg        = " + (tempPortf.AggBondAlloc      / 100).ToString("P", m_formatInfo));
            if(m_userData.Trsry10YrAlloc    > 0) Logger.Log(funcName + ": Trsry      = " + (tempPortf.Trsry10YrAlloc    / 100).ToString("P", m_formatInfo));
            if(m_userData.Bill90Alloc       > 0) Logger.Log(funcName + ": 90-bill    = " + (tempPortf.Bill90Alloc       / 100).ToString("P", m_formatInfo));
            if(m_userData.LongCorpAlloc     > 0) Logger.Log(funcName + ": L-corp     = " + (tempPortf.LongCorpAlloc     / 100).ToString("P", m_formatInfo));
            if(m_userData.MunisAlloc        > 0) Logger.Log(funcName + ": Munis      = " + (tempPortf.MunisAlloc        / 100).ToString("P", m_formatInfo));
            if(m_userData.ReitAlloc         > 0) Logger.Log(funcName + ": Reit       = " + (tempPortf.ReitAlloc         / 100).ToString("P", m_formatInfo));
            if(m_userData.GoldAlloc         > 0) Logger.Log(funcName + ": Gold       = " + (tempPortf.GoldAlloc         / 100).ToString("P", m_formatInfo));
            if(m_userData.CmdtyAlloc        > 0) Logger.Log(funcName + ": Cmdty      = " + (tempPortf.CmdtyAlloc        / 100).ToString("P", m_formatInfo));
            if(tempPortf.CacheAlloc         > 0) Logger.Log(funcName + ": Cache      = " + (tempPortf.CacheAlloc        / 100).ToString("P", m_formatInfo));

            Logger.Log(funcName + ": PortVal = " + tempPortf.Value.ToString("C", m_formatInfo));
            Logger.Log(funcName + ": ------------------");

            m_formatInfo.NumberDecimalDigits = 0; //restore

#if DEBUG
            if (m_userData.Rebalance)
            {
                //check for excess growth

                //stocks
                Debug.Assert(m_userData.TotalMarketAlloc  >= 0 && (int)tempPortf.TotalMarketAlloc <= m_userData.TotalMarketAlloc  + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.LargeCapAlloc     >= 0 && (int)tempPortf.LargeCapAlloc    <= m_userData.LargeCapAlloc     + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.SmallCapAlloc     >= 0 && (int)tempPortf.SmallCapAlloc    <= m_userData.SmallCapAlloc     + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.SmallCapValAlloc  >= 0 && (int)tempPortf.SmallCapValAlloc <= m_userData.SmallCapValAlloc  + REBALANCE_THRESHOLD_PERCT);
                //intl
                Debug.Assert(m_userData.IntlAlloc         >= 0 && (int)tempPortf.IntlAlloc        <= m_userData.IntlAlloc         + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.IntlSmallCapAlloc >= 0 && (int)tempPortf.IntlSmallAlloc   <= m_userData.IntlSmallCapAlloc + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.EmerAlloc         >= 0 && (int)tempPortf.EmerAlloc        <= m_userData.EmerAlloc         + REBALANCE_THRESHOLD_PERCT);

                //bonds - we do expect excess growth due to glide
                if(!m_userData.Glide)
                {
                    Debug.Assert(m_userData.AggBondAlloc    >= 0 && (int)tempPortf.AggBondAlloc   <= m_userData.AggBondAlloc      + REBALANCE_THRESHOLD_PERCT);
                    Debug.Assert(m_userData.Trsry10YrAlloc  >= 0 && (int)tempPortf.Trsry10YrAlloc <= m_userData.Trsry10YrAlloc    + REBALANCE_THRESHOLD_PERCT);
                    Debug.Assert(m_userData.Bill90Alloc     >= 0 && (int)tempPortf.Bill90Alloc    <= m_userData.Bill90Alloc       + REBALANCE_THRESHOLD_PERCT);
                    Debug.Assert(m_userData.LongCorpAlloc   >= 0 && (int)tempPortf.LongCorpAlloc  <= m_userData.LongCorpAlloc     + REBALANCE_THRESHOLD_PERCT);
                    Debug.Assert(m_userData.MunisAlloc      >= 0 && (int)tempPortf.MunisAlloc     <= m_userData.MunisAlloc        + REBALANCE_THRESHOLD_PERCT);
                }

                //alternative
                Debug.Assert(m_userData.ReitAlloc         >= 0 && (int)tempPortf.ReitAlloc        <= m_userData.ReitAlloc         + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.GoldAlloc         >= 0 && (int)tempPortf.GoldAlloc        <= m_userData.GoldAlloc         + REBALANCE_THRESHOLD_PERCT);
                Debug.Assert(m_userData.CmdtyAlloc        >= 0 && (int)tempPortf.CmdtyAlloc       <= m_userData.CmdtyAlloc        + REBALANCE_THRESHOLD_PERCT);
            }
#endif
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawLineChart(List<double> portfolio, string legend)
        {
            int year                 = 0;
            chartGrowth.Visibility   = (m_backTest) ? Visibility.Collapsed : Visibility.Visible;
            chartBackTest.Visibility = (m_backTest) ? Visibility.Visible   : Visibility.Collapsed;
            m_obsvrCollLineChart     = new ObservableCollection<LineSeriesData>();
            m_lineSeries             = new LineSeries();

            for (int i = 0; i < m_userData.Years; i++)
            {
                year = i + 1;

                if (m_backTest)
                {
                    year = m_backTestDlg.StartYear + i + 1;
                }

                m_obsvrCollLineChart.Add(new LineSeriesData((year).ToString(), portfolio[i]));
            }

            m_lineSeries.ItemsSource  = m_obsvrCollLineChart;
            m_lineSeries.XBindingPath = "Year";
            m_lineSeries.YBindingPath = "PortVal";
            m_lineSeries.Label        = legend;

            if(!m_backTest)
            {
                chartGrowth.Series.Add(m_lineSeries);
            }
            else
            {
                chartBackTest.Series.Add(m_lineSeries);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawPieChart(Portfolio portfolio)
        {
            double bonds                         = portfolio.AggBondAlloc  + portfolio.Bill90Alloc    + portfolio.LongCorpAlloc    + portfolio.MunisAlloc + portfolio.Trsry10YrAlloc;
            double stocks                        = portfolio.LargeCapAlloc + portfolio.SmallCapAlloc  + portfolio.SmallCapValAlloc + portfolio.TotalMarketAlloc;
            double intlStocks                    = portfolio.IntlAlloc     + portfolio.IntlSmallAlloc + portfolio.EmerAlloc;

            Debug.Assert(portfolio.VerifyAssetAlloc());

            chartAlloc.Visibility                = Visibility.Visible;
            m_obsvrCollPieChart                  = new ObservableCollection<PieSeriesData>();
            m_pieSeries                          = new PieSeries();
            m_pieSeries.AdornmentsInfo           = new ChartAdornmentInfo();
            m_pieSeries.AdornmentsInfo.SegmentLabelContent = LabelContent.Percentage;
            m_pieSeries.AdornmentsInfo.ShowLabel = true;

            if (stocks               > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("US Stocks"   , stocks));
            if (intlStocks           > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("Intl Stocks" , intlStocks));
            if (bonds                > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("Bonds"       , bonds));
            if (portfolio.ReitAlloc  > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("REIT"        , portfolio.ReitAlloc));
            if (portfolio.GoldAlloc  > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("Gold"        , portfolio.GoldAlloc));
            if (portfolio.CmdtyAlloc > 0)  m_obsvrCollPieChart.Add(new PieSeriesData("Commodities" , portfolio.CmdtyAlloc));

            m_pieSeries.ItemsSource                  = m_obsvrCollPieChart;
            m_pieSeries.XBindingPath                 = "AssetClass";
            m_pieSeries.YBindingPath                 = "Percentage";
            m_pieSeries.Label                        = "Asset Allocation";
            m_pieSeries.EnableSmartLabels            = false;
            chartAlloc.Watermark                     = new Watermark();
            chartAlloc.Watermark.FontSize            = 12;
            chartAlloc.Watermark.Foreground          = new SolidColorBrush(Windows.UI.Colors.Black);
            chartAlloc.Watermark.Content             = (m_backTest) ? "Backtested portfolio" : "Median portfolio";
            chartAlloc.Watermark.HorizontalAlignment = HorizontalAlignment.Center;
            chartAlloc.Watermark.VerticalAlignment   = VerticalAlignment.Bottom;

            if(   stocks 
                + bonds 
                + intlStocks 
                + portfolio.ReitAlloc 
                + portfolio.GoldAlloc 
                + portfolio.CmdtyAlloc == 0 || portfolio.Value == 0)
            {
                chartAlloc.Watermark.FontSize          = 48;
                chartAlloc.Watermark.Content           = "Portfolio has no assets";
                chartAlloc.Watermark.Foreground        = new SolidColorBrush(Windows.UI.Colors.Gray);
                chartAlloc.Watermark.VerticalAlignment = VerticalAlignment.Center;
            }

            chartAlloc.Series.Add(m_pieSeries);
        }


        private void AppBarButtonRun_Click(object sender, RoutedEventArgs e)
        {
            m_backTest = false;
            HandleButtonRunClick();
        }


        private void AppBarButtonClear_Click(object sender, RoutedEventArgs e)
        {
            HandleButtonClearClicked();
        }


        private async void AppBarButtonBackTest_Click(object sender, RoutedEventArgs e)
        {
            m_backTestDlg = new MonteCarloPro.BackTestContentDialog();
            await m_backTestDlg.ShowAsync();

            if (m_backTestDlg.Result == ContentDialogResult.Primary)
            {
                m_backTest = true;

                HandleButtonRunClick();
            }
            else if (m_backTestDlg.Result == ContentDialogResult.Secondary)
            {
                m_backTest = false;
            }
            else
            {
                Debug.Assert(false);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SaveInput()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            //investor
            localSettings.Values["textBoxPortVal"]           = textBoxPortVal.Text;
            localSettings.Values["textBoxYears"]             = textBoxYears.Text;
            localSettings.Values["textBoxAnnualContr"]       = textBoxAnnualContr.Text;
            localSettings.Values["textBoxWithdrwl"]          = textBoxWithdrwl.Text;
            localSettings.Values["toggleSwitchRebalance"]    = toggleSwitchRebalance.IsOn;
            localSettings.Values["toggleSwitchGlide"]        = toggleSwitchGlide.IsOn;
            localSettings.Values["textBoxGlide"]             = textBoxGlide.Text;
            localSettings.Values["textBoxInflation"]         = textBoxInflation.Text;
            localSettings.Values["textBoxFeesPerct"]         = textBoxFeesPerct.Text;
            //stocks
            localSettings.Values["textBoxTotalMarketAlloc"]  = textBoxTotalMarketAlloc.Text;
            localSettings.Values["textBoxLargeCapAlloc"]     = textBoxLargeCapAlloc.Text;
            localSettings.Values["textBoxSmallCapAlloc"]     = textBoxSmallCapAlloc.Text;
            localSettings.Values["textBoxSmallValAlloc"]     = textBoxSmallValAlloc.Text;
            //intl
            localSettings.Values["textBoxIntlAlloc"]         = textBoxIntlAlloc.Text;
            localSettings.Values["textBoxIntlSmallCapAlloc"] = textBoxIntlSmallCapAlloc.Text;
            localSettings.Values["textBoxEmerAlloc"]         = textBoxEmerAlloc.Text;
            //bonds
            localSettings.Values["textBoxAggBondAlloc"]      = textBoxAggBondAlloc.Text;
            localSettings.Values["textBox10YrTresAlloc"]     = textBox10YrTresAlloc.Text;
            localSettings.Values["textBox90DayBillAlloc"]    = textBox90DayBillAlloc.Text;
            localSettings.Values["textBoxLongCorpAlloc"]     = textBoxLongCorpAlloc.Text;
            localSettings.Values["textBoxMuniAlloc"]         = textBoxMuniAlloc.Text;
            //alternative
            localSettings.Values["textBoxReitAlloc"]         = textBoxReitAlloc.Text;
            localSettings.Values["textBoxGoldAlloc"]         = textBoxGoldAlloc.Text;
            localSettings.Values["textBoxCmdtyAlloc"]        = textBoxCmdtyAlloc.Text;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadInput()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            //no settings saved yet?
            if(localSettings.Values["textBoxPortVal"] == null)
            {
                return;
            }

            //investor
            textBoxPortVal.Text           = localSettings.Values["textBoxPortVal"].ToString();
            textBoxYears.Text             = localSettings.Values["textBoxYears"].ToString();
            textBoxAnnualContr.Text       = localSettings.Values["textBoxAnnualContr"].ToString();
            textBoxWithdrwl.Text          = localSettings.Values["textBoxWithdrwl"].ToString();
            toggleSwitchRebalance.IsOn    = (bool) localSettings.Values["toggleSwitchRebalance"];
            toggleSwitchGlide.IsOn        = (bool) localSettings.Values["toggleSwitchGlide"];
            textBoxGlide.Text             = localSettings.Values["textBoxGlide"].ToString();
            textBoxInflation.Text         = localSettings.Values["textBoxInflation"].ToString();
            textBoxFeesPerct.Text         = localSettings.Values["textBoxFeesPerct"].ToString();
            //stocks
            textBoxTotalMarketAlloc.Text  = localSettings.Values["textBoxTotalMarketAlloc"].ToString();
            textBoxLargeCapAlloc.Text     = localSettings.Values["textBoxLargeCapAlloc"].ToString();
            textBoxSmallCapAlloc.Text     = localSettings.Values["textBoxSmallCapAlloc"].ToString();
            textBoxSmallValAlloc.Text     = localSettings.Values["textBoxSmallValAlloc"].ToString();
            //intl
            textBoxIntlAlloc.Text         = localSettings.Values["textBoxIntlAlloc"].ToString();
            textBoxIntlSmallCapAlloc.Text = localSettings.Values["textBoxIntlSmallCapAlloc"].ToString();
            textBoxEmerAlloc.Text         = localSettings.Values["textBoxEmerAlloc"].ToString();
            //bonds
            textBoxAggBondAlloc.Text      = localSettings.Values["textBoxAggBondAlloc"].ToString();
            textBox10YrTresAlloc.Text     = localSettings.Values["textBox10YrTresAlloc"].ToString();
            textBox90DayBillAlloc.Text    = localSettings.Values["textBox90DayBillAlloc"].ToString();
            textBoxLongCorpAlloc.Text     = localSettings.Values["textBoxLongCorpAlloc"].ToString();
            textBoxMuniAlloc.Text         = localSettings.Values["textBoxMuniAlloc"].ToString();
            //alternative
            textBoxReitAlloc.Text         = localSettings.Values["textBoxReitAlloc"].ToString();
            textBoxGoldAlloc.Text         = localSettings.Values["textBoxGoldAlloc"].ToString();
            textBoxCmdtyAlloc.Text        = localSettings.Values["textBoxCmdtyAlloc"].ToString();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetCummRet(double initVal)
        {
            Debug.Assert(GetTotalAlloc() == 100);

            Portfolio portfolio = new Portfolio(m_userData, m_userData.AssetClassCount);

            //stocks
            portfolio.LargeCapValue     = initVal * (m_userData.LargeCapAlloc       / 100);
            portfolio.TotalMarketValue  = initVal * (m_userData.TotalMarketAlloc    / 100);
            portfolio.SmallCapValue     = initVal * (m_userData.SmallCapAlloc       / 100);
            portfolio.SmallCapValValue  = initVal * (m_userData.SmallCapValAlloc    / 100);
            //bonds
            portfolio.AggBondValue      = initVal * (m_userData.AggBondAlloc        / 100);
            portfolio.Bill90Value       = initVal * (m_userData.Bill90Alloc         / 100);
            portfolio.Trsry10YrValue    = initVal * (m_userData.Trsry10YrAlloc      / 100);
            portfolio.MunisValue        = initVal * (m_userData.MunisAlloc          / 100);
            portfolio.LongCorpValue     = initVal * (m_userData.LongCorpAlloc       / 100);
            //intl
            portfolio.IntlValue         = initVal * (m_userData.IntlAlloc           / 100);
            portfolio.IntlSmallCapValue = initVal * (m_userData.IntlSmallCapAlloc   / 100);
            portfolio.EmerValue         = initVal * (m_userData.EmerAlloc           / 100);
            //alternative
            portfolio.CmdtyValue        = initVal * (m_userData.CmdtyAlloc          / 100);
            portfolio.GoldValue         = initVal * (m_userData.GoldAlloc           / 100);
            portfolio.ReitValue         = initVal * (m_userData.ReitAlloc           / 100);

            //backup AnnWithdrwlDlr because it'll change depending on inflation.
            double annWithdrwBak = m_userData.AnnWithdrwlDlr;

            for (int i = 0; i < m_userData.Years; i++)
            {
                //stocks
                if (m_userData.LargeCapAlloc     > 0)    {   portfolio.LargeCapValue     += (portfolio.LargeCapValue     * (m_largeCapRets[i]     / 100));      portfolio.TradeCount++; }
                if (m_userData.TotalMarketAlloc  > 0)    {   portfolio.TotalMarketValue  += (portfolio.TotalMarketValue  * (m_totalMarketRets[i]  / 100));      portfolio.TradeCount++; }
                if (m_userData.SmallCapAlloc     > 0)    {   portfolio.SmallCapValue     += (portfolio.SmallCapValue     * (m_smallCapRets[i]     / 100));      portfolio.TradeCount++; }
                if (m_userData.SmallCapValAlloc  > 0)    {   portfolio.SmallCapValValue  += (portfolio.SmallCapValValue  * (m_smallCapValRets[i]  / 100));      portfolio.TradeCount++; }
                //bonds
                if (m_userData.AggBondAlloc      > 0)    {   portfolio.AggBondValue      += (portfolio.AggBondValue      * (m_aggBondRets[i]      / 100));      portfolio.TradeCount++; }
                if (m_userData.Bill90Alloc       > 0)    {   portfolio.Bill90Value       += (portfolio.Bill90Value       * (m_bills90Rets[i]      / 100));      portfolio.TradeCount++; }
                if (m_userData.Trsry10YrAlloc    > 0)    {   portfolio.Trsry10YrValue    += (portfolio.Trsry10YrValue    * (m_trsry10Rets[i]      / 100));      portfolio.TradeCount++; }
                if (m_userData.MunisAlloc        > 0)    {   portfolio.MunisValue        += (portfolio.MunisValue        * (m_munisRets[i]        / 100));      portfolio.TradeCount++; }
                if (m_userData.LongCorpAlloc     > 0)    {   portfolio.LongCorpValue     += (portfolio.LongCorpValue     * (m_longCorpRets[i]     / 100));      portfolio.TradeCount++; }
                //intl
                if (m_userData.IntlAlloc         > 0)    {   portfolio.IntlValue         += (portfolio.IntlValue         * (m_intlRets[i]         / 100));      portfolio.TradeCount++; }
                if (m_userData.IntlSmallCapAlloc > 0)    {   portfolio.IntlSmallCapValue += (portfolio.IntlSmallCapValue * (m_intlSmallCapRets[i] / 100));      portfolio.TradeCount++; }
                if (m_userData.EmerAlloc         > 0)    {   portfolio.EmerValue         += (portfolio.EmerValue         * (m_emerRets[i]         / 100));      portfolio.TradeCount++; }
                //alternative
                if (m_userData.CmdtyAlloc        > 0)    {   portfolio.CmdtyValue        += (portfolio.CmdtyValue        * (m_cmdtyRets[i]        / 100));      portfolio.TradeCount++; }
                if (m_userData.GoldAlloc         > 0)    {   portfolio.GoldValue         += (portfolio.GoldValue         * (m_goldRets[i]         / 100));      portfolio.TradeCount++; }
                if (m_userData.ReitAlloc         > 0)    {   portfolio.ReitValue         += (portfolio.ReitValue         * (m_reitRets[i]         / 100));      portfolio.TradeCount++; }

                //after applying market returns, make sure no asset class has a value less than 0
                portfolio.ResetToZero();

                if (m_userData.AnnContr       > 0)                          portfolio.IncreaseAssets(m_userData.AnnContr);
                if (m_userData.AnnWithdrwlDlr > 0 && portfolio.Value > 0)   portfolio.ReduceAssets(m_userData.AnnWithdrwlDlr);
                if (m_userData.FeesPerct      > 0 && portfolio.Value > 0)   portfolio.ReduceAssets(portfolio.Value * (m_userData.FeesPerct / 100));

                //infliation is experienced at the end of the year so keep this block of code here
                if (  portfolio.Value > 0 
                    && ((!m_backTest && m_userData.Inflation                != 0)  //regular mode  w/inflation
                    ||  ( m_backTest && m_userData.Inflation                != 0)  //backtest mode w/inflation
                    ||  ( m_backTest && m_backTestDlg.UseHistorialInflation))   )  //backtest mode w/hist. inflation
                {
                    double inflationPerct = 0;
                    double inflationVal   = 0;

                    if (m_backTest && m_backTestDlg.UseHistorialInflation)
                    {
                        inflationPerct = Data.CPI[Data.CPI.Count - m_userData.Years + i];
                    }
                    else
                    {
                        inflationPerct = m_userData.Inflation;
                    }

                    inflationVal = portfolio.Value * (inflationPerct / 100);

                    if (inflationPerct < 0)
                    {
                        portfolio.IncreaseAssets(inflationVal * -1);
                    }
                    else if (inflationPerct > 0)
                    {
                        portfolio.ReduceAssets(inflationVal);
                    }

                    //apply inflation to AnnWithdrwlDlr. By having this here we don't apply inflation to first year's withdrawal
                    if (m_userData.AnnWithdrwlDlr != 0)
                    {
                        m_userData.AnnWithdrwlDlr += m_userData.AnnWithdrwlDlr * (inflationPerct / 100);
                    }
                }

                if (   m_userData.Rebalance
                    && portfolio.Value            > REBALANCE_PORT_MIN_AMOUNT
                    && m_userData.AssetClassCount > 1)
                {
                    if(m_userData.Glide && m_userData.GlidePerct > 0)
                    {
                        portfolio.DoGlide(m_userData.GlidePerct);
                    }
                    
                    portfolio.Rebalance();
                }

                portfolio.AddToBalanceList(portfolio.Value);

#if DEBUG
                //stocks
                if (m_userData.LargeCapAlloc     > 0) Debug.Assert(Data.LARGE_CAP.Find        (x => x == m_largeCapRets[i])     == m_largeCapRets[i]);
                if (m_userData.TotalMarketAlloc  > 0) Debug.Assert(Data.TOTAL_MARKET.Find     (x => x == m_totalMarketRets[i])  == m_totalMarketRets[i]);
                if (m_userData.SmallCapAlloc     > 0) Debug.Assert(Data.SMALL_CAP.Find        (x => x == m_smallCapRets[i])     == m_smallCapRets[i]);
                if (m_userData.SmallCapValAlloc  > 0) Debug.Assert(Data.SMALL_CAP_VAL.Find    (x => x == m_smallCapValRets[i])  == m_smallCapValRets[i]);
                //bonds
                if (m_userData.AggBondAlloc      > 0) Debug.Assert(Data.AGG_BOND.Find         (x => x == m_aggBondRets[i])      == m_aggBondRets[i]);
                if (m_userData.Bill90Alloc       > 0) Debug.Assert(Data.BILLS_90_DAYS.Find    (x => x == m_bills90Rets[i])      == m_bills90Rets[i]);
                if (m_userData.Trsry10YrAlloc    > 0) Debug.Assert(Data.TRSRY_10_YR.Find      (x => x == m_trsry10Rets[i])      == m_trsry10Rets[i]);
                if (m_userData.MunisAlloc        > 0) Debug.Assert(Data.MUNIS.Find            (x => x == m_munisRets[i])        == m_munisRets[i]);
                if (m_userData.LongCorpAlloc     > 0) Debug.Assert(Data.LONG_CORP_BOND.Find   (x => x == m_longCorpRets[i])     == m_longCorpRets[i]);
                //intl
                if (m_userData.IntlAlloc         > 0) Debug.Assert(Data.INTL_EAFE.Find        (x => x == m_intlRets[i])         == m_intlRets[i]);
                if (m_userData.IntlSmallCapAlloc > 0) Debug.Assert(Data.INTL_SMALL.Find       (x => x == m_intlSmallCapRets[i]) == m_intlSmallCapRets[i]);
                if (m_userData.EmerAlloc         > 0) Debug.Assert(Data.INTL_EMER.Find        (x => x == m_emerRets[i])         == m_emerRets[i]);
                //alt.
                if (m_userData.CmdtyAlloc        > 0) Debug.Assert(Data.CMDTY.Find            (x => x == m_cmdtyRets[i])        == m_cmdtyRets[i]);
                if (m_userData.GoldAlloc         > 0) Debug.Assert(Data.GOLD.Find             (x => x == m_goldRets[i] )        == m_goldRets[i]);
                if (m_userData.ReitAlloc         > 0) Debug.Assert(Data.REIT.Find             (x => x == m_reitRets[i] )        == m_reitRets[i]);
#endif
            } //for

            //restore AnnWithdrwlDlr
            m_userData.AnnWithdrwlDlr = annWithdrwBak;
            m_portfolios.Add(portfolio);

            Debug.Assert(portfolio.CacheValue             <= REBALANCE_CACHE_LIMIT);
            Debug.Assert(portfolio.RebalanceCount         <= m_userData.Years);
            Debug.Assert(portfolio.Value                  >= 0);
            Debug.Assert(portfolio.GetBalanceList().Count == m_userData.Years);

#if DEBUG
            //Logger.Log(">>>>>>>>>>");
#endif
            return portfolio.Value;
        }


        private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (   e.Key                == Windows.System.VirtualKey.Enter
                && e.KeyStatus.ScanCode == 0) //we get 0 or 28, only catch one of them.
            {
                //hitting enter always runs in regular mode
                m_backTest = false;
                m_trimmed  = false;

                HandleButtonRunClick();
                e.Handled  = true;
            }
        }


        private void AppBarButtonDisclaimer_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DisclaimerPage));
        }


        private void AppBarButtondataSources_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(DataSourcesPage));
        }
    }//class
}//namespace