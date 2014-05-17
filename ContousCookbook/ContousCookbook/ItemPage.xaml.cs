using ContousCookbook.Common;
using ContousCookbook.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using System.Text;
using Windows.Storage.Streams;
using Windows.Media.Capture;
using Windows.Storage;


// The Item Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234232

namespace ContousCookbook
{
    /// <summary>
    /// A page that displays details for a single item within a group.
    /// </summary>
    public sealed partial class ItemPage : Page
    {
        private NavigationHelper navigationHelper;
        private StorageFile _photo; // Photo file to share
        private StorageFile _video; // Video file to share
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public ItemPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
            this.SizeChanged += ItemPage_SizeChanged;
        }

        private void ItemPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width <= 800)
            {
                VisualStateManager.GoToState(this, "NarrowLayout", true);
            }
            else if (e.NewSize.Width > 800 && e.NewSize.Width <= 1250)
            {
                VisualStateManager.GoToState(this, "IntermediateLayout", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "DefaultLayout", true);
            }

        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var item = await SampleDataSource.GetItemAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
        /// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
            // Register for DataRequested events
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
            // Deregister the DataRequested event handler
            DataTransferManager.GetForCurrentView().DataRequested -= OnDataRequested;

        }

        #endregion

        void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            var item = (SampleDataItem)this.DefaultViewModel["Item"];
            request.Data.Properties.Title = item.Title;

            if (_photo != null)
            {
                request.Data.Properties.Description = "Recipe photo";
                var reference = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(_photo);
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
                _photo = null;
            }
            else if (_video != null)
            {
                request.Data.Properties.Description = "Recipe video";
                List<StorageFile> items = new List<StorageFile>();
                items.Add(_video);
                request.Data.SetStorageItems(items);
                _video = null;
            }

            else
            {
                request.Data.Properties.Description = "Recipe ingredients and directions";

                // Share recipe text
                var recipe = "\r\nINGREDIENTS\r\n";
                recipe += String.Join("\r\n", item.Ingredients);
                recipe += ("\r\n\r\nDIRECTIONS\r\n" + item.Content);
                request.Data.SetText(recipe);

                // Share recipe image
                var reference = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///" + item.ImagePath));
                request.Data.Properties.Thumbnail = reference;
                request.Data.SetBitmap(reference);
            }

        }

        private async void OnCapturePhoto(object sender, RoutedEventArgs e)
        {
            var camera = new CameraCaptureUI();
            var file = await camera.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (file != null)
            {
                var item = (SampleDataItem)this.DefaultViewModel["Item"];
                item.Media.Add(file);
                _photo = file;
                DataTransferManager.ShowShareUI();
            }
        }

        private async void OnCaptureVideo(object sender, RoutedEventArgs e)
        {
            var camera = new CameraCaptureUI();
            camera.VideoSettings.Format = CameraCaptureUIVideoFormat.Wmv;
            var file = await camera.CaptureFileAsync(CameraCaptureUIMode.Video);

            if (file != null)
            {
                var item = (SampleDataItem)this.DefaultViewModel["Item"];
                item.Media.Add(file);
                _video = file;
                DataTransferManager.ShowShareUI();
            }

        }

        private void OnNavigateToMedia(object sender, RoutedEventArgs e)
        {
            var item = (SampleDataItem)this.DefaultViewModel["Item"];
            this.Frame.Navigate(typeof(ItemMediaPage), item.UniqueId);
        }

    }
}