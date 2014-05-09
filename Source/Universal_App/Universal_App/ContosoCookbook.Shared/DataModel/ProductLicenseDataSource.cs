// ----------------------------------------------------------------------------------
// Microsoft Developer & Platform Evangelism
// 
// Copyright (c) Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
// OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
// The example companies, organizations, products, domain names,
// e-mail addresses, logos, people, places, and events depicted
// herein are fictitious.  No association with any real company,
// organization, product, domain name, email address, logo, person,
// places, or events is intended or should be inferred.
// ----------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Store;
using Windows.UI.Core;

namespace ContosoCookbook.Data
{
    class ProductLicenseDataSource : INotifyPropertyChanged
    {
        private const string _name = "ItalianRecipes";

        private bool _licensed = false;
        private string _price;

        public event PropertyChangedEventHandler PropertyChanged;

        public string GroupTitle
        {
            set
            {
                if (value != "Italian")
                {
                    _licensed = true;
                }
                else if (CurrentAppSimulator.LicenseInformation.ProductLicenses[_name].IsActive)
                {
                    _licensed = true;
                }
                else
                {
                    CurrentAppSimulator.LicenseInformation.LicenseChanged += OnLicenseChanged;
                    GetListingInformationAsync();
                }
            }
        }

        private async void GetListingInformationAsync()
        {
            var listing = await CurrentAppSimulator.LoadListingInformationAsync();
            _price = listing.ProductListings[_name].FormattedPrice;
        }

        private async void OnLicenseChanged()
        {
            if (CurrentAppSimulator.LicenseInformation.ProductLicenses[_name].IsActive)
            {
                _licensed = true;
                CurrentAppSimulator.LicenseInformation.LicenseChanged -= OnLicenseChanged;

                var mainWindow = CoreApplication.MainView.CoreWindow;
                if (mainWindow != null)
                {
                    await mainWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        if (PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs("IsLicensed"));
                            PropertyChanged(this, new PropertyChangedEventArgs("IsTrial"));
                        }
                    });
                }
            }
        }

        public bool IsLicensed
        {
            get { return _licensed; }
        }

        public bool IsTrial
        {
            get { return !_licensed; }
        }

        public string FormattedPrice
        {
            get
            {
                if (!String.IsNullOrEmpty(_price))
                {
                    return "Purchase Italian recipes for " + _price;
                }
                else
                {
                    return "Purchase Italian recipes";
                }
            }
        }
    }
}
