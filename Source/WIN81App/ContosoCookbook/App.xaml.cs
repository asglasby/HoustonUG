using ContosoCookbook.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
#if WINDOWS_APP
using Windows.UI.ApplicationSettings;
#endif
using Windows.Storage;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using System.Net.Http;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;


// The Hub App template is documented at http://go.microsoft.com/fwlink/?LinkId=286574

namespace ContosoCookbook
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton Application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            if (e.PreviousExecutionState == ApplicationExecutionState.Running)
            {
                if (!String.IsNullOrEmpty(e.Arguments))
                {
                    ((Frame)Window.Current.Content).Navigate(typeof(ItemPage), e.Arguments);
                }
                Window.Current.Activate();
                return;
            }

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active

            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                //Associate the frame with a SuspensionManager key                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Restore the saved session state only when appropriate
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //Something went wrong restoring state.
                        //Assume there is no state and continue
                    }
                }

#if WINDOWS_APP
                // Register handler for CommandsRequested events from the settings pane
                SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;
#endif
                // Configure Notifications
                await ConfigureNotifications();

                // Initialize CurrentAppSimulator
                var file = await Package.Current.InstalledLocation.GetFileAsync("DataModel\\license.xml");
                await Windows.ApplicationModel.Store.CurrentAppSimulator.ReloadSimulatorAsync(file);

                // If the app was activated from a secondary tile, show the recipe
                if (!String.IsNullOrEmpty(e.Arguments))
                {
                    rootFrame.Navigate(typeof(ItemPage), e.Arguments);
                    Window.Current.Content = rootFrame;
                    Window.Current.Activate();
                    return;
                }

                // If the app was closed by the user the last time it ran, and if "Remember
                // "where I was" is enabled, restore the navigation state
                if (e.PreviousExecutionState == ApplicationExecutionState.ClosedByUser)
                {
                    if (ApplicationData.Current.RoamingSettings.Values.ContainsKey("Remember"))
                    {
                        bool remember = (bool)ApplicationData.Current.RoamingSettings.Values["Remember"];
                        if (remember)
                        {
                            await SuspensionManager.RestoreAsync();
                        }
                    }
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(HubPage)))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

#if WINDOWS_APP
        private void OnCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // Add an About command
            var about = new SettingsCommand("about", "About", (handler) =>
            {
                var aboutFlyout = new AboutFlyout();
                aboutFlyout.Show();
            });

            args.Request.ApplicationCommands.Add(about);

            // Add a Preferences command
            var preferences = new SettingsCommand("preferences", "Preferences", (handler) =>
            {
                var preferencesFlyout = new PreferencesFlyout();
                preferencesFlyout.Show();
            });

            args.Request.ApplicationCommands.Add(preferences);
        }
#endif
        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        private async static Task ConfigureNotifications()
        {
            // Send local notifications
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueueForSquare310x310(true);

            var topRated = await ContosoCookbook.Data.SampleDataSource.GetTopRatedRecipesAsync(5);

            foreach (var recipe in topRated.Items)
            {
                var templateContent = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310BlockAndText02);

                var imageAttributes = templateContent.GetElementsByTagName("image");
                ((XmlElement)imageAttributes[0]).SetAttribute("src", "ms-appx:///" + recipe.ImagePath);
                ((XmlElement)imageAttributes[0]).SetAttribute("alt", recipe.Description);

                var tileTextAttributes = templateContent.GetElementsByTagName("text");
                tileTextAttributes[1].InnerText = recipe.Title;
                tileTextAttributes[3].InnerText = "Preparation Time";
                tileTextAttributes[4].InnerText = recipe.PreparationTime.ToString() + " minutes";
                tileTextAttributes[5].InnerText = "Rating";
                tileTextAttributes[6].InnerText = recipe.Rating.ToString() + " stars";

                var tileNotification = new TileNotification(templateContent);
                tileNotification.Tag = recipe.UniqueId;

                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);
            }
            #region Push Notifications
            // Register for push notifications
            //var profile = NetworkInformation.GetInternetConnectionProfile();

            //if (profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            //{
            //    var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            //    var buffer = CryptographicBuffer.ConvertStringToBinary(channel.Uri, BinaryStringEncoding.Utf8);
            //    var uri = CryptographicBuffer.EncodeToBase64String(buffer);
            //    var client = new HttpClient();

            //    try
            //    {
            //        var response = await client.GetAsync(new Uri("http://ContosoRecipes8.cloudapp.net?uri=" + uri + "&type=tile"));

            //        if (!response.IsSuccessStatusCode)
            //        {
            //            var dialog = new MessageDialog("Unable to open push notification channel");
            //            dialog.ShowAsync();
            //        }
            //    }
            //    catch (HttpRequestException)
            //    {
            //        var dialog = new MessageDialog("Unable to open push notification channel");
            //        dialog.ShowAsync();
            //    }
            //}
            #endregion
        }
    }
}
