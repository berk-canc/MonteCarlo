using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Storage;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MonteCarloPro
{
    public sealed partial class BackTestContentDialog : ContentDialog
    {
        public ContentDialogResult Result
        {
            get;
            set;
        }


        public int StartYear
        {
            get;
            set;
        }


        public int EndYear
        {
            get;
            set;
        }


        public bool UseHistorialInflation
        {
            get
            {
                return (checkBoxInflation.IsChecked == true) ? true : false;
            }
        }


        public BackTestContentDialog()
        {
            this.InitializeComponent();

            Result    = ContentDialogResult.Secondary; //cancel click
            StartYear = 0;
            EndYear   = 0;

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            //load input if already saved
            if (localSettings.Values["textBoxStartYear"] != null)
            {
                textBoxStartYear.Text = localSettings.Values["textBoxStartYear"].ToString();
                textBoxEndYear.Text   = localSettings.Values["textBoxEndYear"].ToString();
            }
        }


        private void ButtonOk_Click(object sender, RoutedEventArgs e)
        {
            if (VerifyInput())
            {
                Result = ContentDialogResult.Primary;

                this.Hide();
            }
        }


        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = ContentDialogResult.Secondary;

            this.Hide();
        }


        private void ContentDialog_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key                   == Windows.System.VirtualKey.Enter 
                && e.KeyStatus.ScanCode == 0 //we get 0 or 28, only catch one of them.
                && VerifyInput())
            {
                Result    = ContentDialogResult.Primary; //enable
                e.Handled = true;

                this.Hide();
            }
        }


        private bool VerifyInput()
        {
            bool retVal = false;

            try
            {
                StartYear = Convert.ToInt32(textBoxStartYear.Text);
                EndYear   = Convert.ToInt32(textBoxEndYear.Text);
            }
            catch (FormatException)
            {
                MonteCarlo.MainPage.ShowDialog("Enter 4 digit year.");
                goto Exit;
            }

            if(StartYear < 1926 || StartYear > 2010)
            {
                MonteCarlo.MainPage.ShowDialog("Start year must be between 1926 and 2010.");
                goto Exit;
            }

            retVal = true;

            //save input
            ApplicationDataContainer localSettings   = ApplicationData.Current.LocalSettings;
            localSettings.Values["textBoxStartYear"] = textBoxStartYear.Text;
            localSettings.Values["textBoxEndYear"]   = textBoxEndYear.Text;

        Exit:
            return retVal;
        }

    }//class
}//namespace