﻿using ContousCookbook.Common;

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
using Windows.UI.ApplicationSettings;
using Windows.Storage;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.Security.Cryptography;
using System.Net.Http;
using Windows.Networking.Connectivity;
using Windows.UI.Popups;

// The Hub App template is documented at http://go.microsoft.com/fwlink/?LinkId=321221

namespace ContousCookbook
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

                rootFrame.NavigationFailed += OnNavigationFailed;

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

                // Register handler for CommandsRequested events from the settings pane
                SettingsPane.GetForCurrentView().CommandsRequested += OnCommandsRequested;

                // If the app was activated from a secondary tile, show the recipe
                if (!String.IsNullOrEmpty(e.Arguments))
                {
                    rootFrame.Navigate(typeof(ItemPage), e.Arguments);
                    Window.Current.Content = rootFrame;
                    Window.Current.Activate();
                    return;
                }

                // Configure Notifications
                await ConfigureNotifications();



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

                if (e.PreviousExecutionState == ApplicationExecutionState.Running)
                {
                    if (!String.IsNullOrEmpty(e.Arguments))
                    {
                        ((Frame)Window.Current.Content).Navigate(typeof(ItemPage), e.Arguments);
                    }
                    Window.Current.Activate();
                    return;
                }


                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(HubPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

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
                var preferenceFlyout = new PreferenceFlyout();
                preferenceFlyout.Show();
            });

            args.Request.ApplicationCommands.Add(preferences);
        }

        private async static Task ConfigureNotifications()
        {
            // Send local notifications
            TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueueForSquare310x310(true);

            var topRated = await ContousCookbook.Data.SampleDataSource.GetTopRatedRecipesAsync(5);

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

            // Register for push notifications
            var profile = NetworkInformation.GetInternetConnectionProfile();

            if (profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess)
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                var buffer = CryptographicBuffer.ConvertStringToBinary(channel.Uri, BinaryStringEncoding.Utf8);
                var uri = CryptographicBuffer.EncodeToBase64String(buffer);
                var client = new HttpClient();

                try
                {
                    var response = await client.GetAsync(new Uri("http://ContosoRecipes8.cloudapp.net?uri=" + uri + "&type=tile"));

                    if (!response.IsSuccessStatusCode)
                    {
                        var dialog = new MessageDialog("Unable to open push notification channel");
                        await dialog.ShowAsync();
                    }
                }
                catch (HttpRequestException)
                {
                    var dialog = new MessageDialog("Unable to open push notification channel");
                    dialog.ShowAsync();
                }
            }
        }

    }
}
