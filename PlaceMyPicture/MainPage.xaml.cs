using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using winsdkfb;
using winsdkfb.Graph;

//maps 
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using System.Dynamic;
using Windows.ApplicationModel.Core;

namespace PlaceMyPicture
{
    public sealed partial class MainPage : Page
    {

        //private Dictionary<String, Datum> picDataDict;
        private Dictionary<BitmapImage, BasicGeoposition> _geoLocDict;
        private Dictionary<Uri, FbPicInfo> _tempInfoDict;
        private const string FB_API_KEY = "1083564175045990";
        private FBSession session;

        public MainPage()
        {
            this.InitializeComponent();
            Init();         
        }

        /*Init method initialises everything at the start
         */
        private void Init()
        {
            _geoLocDict = new Dictionary<BitmapImage, BasicGeoposition>();
            _tempInfoDict = new Dictionary<Uri, FbPicInfo>();
            GetUserLocation();
            OnLogin();
            DefaultView();
        }

        /*Method gets the users Current location and passes the Long/Lat to the Bing Map showing you your current location
         */
        private async void GetUserLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();

            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:

                    try
                    {
                        Geolocator _geolocator = new Geolocator { DesiredAccuracyInMeters = 10 };

                        Geoposition pos = await _geolocator.GetGeopositionAsync();
                        BasicGeoposition geo = new BasicGeoposition();

                        geo.Latitude = pos.Coordinate.Point.Position.Latitude;
                        geo.Longitude = pos.Coordinate.Point.Position.Longitude;

                        BingMap.Center = new Geopoint(geo);//Center Map on geolocation
                        BingMap.ZoomLevel = 7;//Sets the zoom level on the map
                    }
                    catch (Exception) { }

                    BingMap.Height = SpMap.ActualHeight;//important, sets height to stackpannels height
                    break;

                case GeolocationAccessStatus.Denied:
                    //Please turn your location on 
                    break;

                case GeolocationAccessStatus.Unspecified:
                    GetUserLocation();
                    break;
            }
        }

        /*Facebook LogIn Authorization/Permissions
         */
        private async void OnLogin()
        {
            string Sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            session = FBSession.ActiveSession;

            session.WinAppId = Sid;//not used (only for windows 8 phone etc)
            session.FBAppId = FB_API_KEY;//AppId From App Created on facebook

            List<String> permissionList = new List<String>();//list of all the permissions needed from the user
            permissionList.Add("public_profile");
            permissionList.Add("user_location");
            permissionList.Add("user_photos");

            FBPermissions permissions = new FBPermissions(permissionList);

            var result = await session.LoginAsync(permissions);
            if (result.Succeeded)
            {
                string name = session.User.Name;
                OnSuccessLogin();//once the user has given permission and logged on
            }
            else
            {
                MessageDialog dialog = new MessageDialog("1) Re-check Credentials \n2) Check your internet connection.\n\n YES To Retry\n NO To Quit");
                dialog.Title = "Error Logging In";
                dialog.Commands.Add(new UICommand("Yes") { Id = 0 });

                dialog.Commands.Add(new UICommand("No") { Id = 1 });
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;

                var messagResult = await dialog.ShowAsync();

                if ((int)messagResult.Id == 0)//If log out was yes, log out otherwise do nothing
                    LogOut();
                else
                    CloseApp();
            }
        }

        public void CloseApp()
        {
            CoreApplication.Exit();
        }

        /*On Successful login, send GET request to FB endpoint with params and DeserializeJson the results into objects/Dictonary
         */
        private async void OnSuccessLogin()
        {
            string endpoint = "/me/photos";//where the url starts from 

            PropertySet parameters = new PropertySet();
            parameters.Add("fields", "source,place");//Required fields needed

            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();

            if (graphResult.Succeeded)//check to see if the Request Succeeded
            {
                PicturePlaceObject results = graphResult.Object as PicturePlaceObject;

                var db = new PicturePlaceDb();
                GetAllFbPic(results, parameters, endpoint, db);
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Try Again Later!");
                await dialog.ShowAsync();
            }
        }

        /*Method keeps sending get requets to facebook api using paging, untill all pictures have been added
         */
        private async void GetAllFbPic(PicturePlaceObject results, PropertySet parameters, string endpoint, PicturePlaceDb db)
        {
            Boolean addedAllPics = false;
            do//only do this while there is a next page and all pics have not been added
            {
                addedAllPics = SortPictures(results, db);//Add Results to a list

                if (addedAllPics == false)
                {
                    parameters.Remove("after");//Remove previous parameters
                    parameters.Add("after", results.paging.cursors.after);//the next page to send the request too

                    FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
                    FBResult graphResult = await value.GetAsync();//check to see if the Requets Succeeded 
                    results = graphResult.Object as PicturePlaceObject;
                }

            } while ((results.paging != null || results.data.Count() != 0) && addedAllPics == false);
            PaintPins(db);
        }

        /*Checks to see if there are any new pics in the results returned from facebook, if yes, they are added to the database, otherwise return true "All Pics Added"
         */
        private Boolean SortPictures(PicturePlaceObject results, PicturePlaceDb db)
        {
            Boolean addedAllPics = true;
            foreach (var pic in results.data)//loop through all the Pics in results.data
            {
                if (pic.place != null && pic.place.location != null)//Only add Pics that have a place/location from results
                {
                    var placeId = db.data.Select(id => id.id);//COLLECTION OF ALL THE IDS IN THE DATABASE
                    //check to see if the collection of ids already contains the new id, if not a new pic is found etc.
                    if (!placeId.Contains(pic.id))
                    {
                        addedAllPics = false;
                        StorePicture(pic, db);
                    }
                }
            }
            return addedAllPics;
        }

        /*Method stores pics in the local database
         */
        private void StorePicture(Datum pic, PicturePlaceDb db)
        {
            var picInfo = new FbPicInfo
            {
                city = pic.place.location.city,
                country = pic.place.location.country,
                latitude = pic.place.location.latitude,
                longitude = pic.place.location.longitude,
                id = pic.id,
                name = pic.place.name,
                source = pic.source
            };

            db.Add(picInfo);
            db.SaveChanges();
        }

        /*Method takes in the Database object and creates a temporary list which is then looped through and passed into   AddPinToMap() to paint the pin to map     
         */
        private async void PaintPins(PicturePlaceDb db)
        {
            var tempList = db.data.AsEnumerable().Select(pic => new FbPicInfo
            {
                longitude = pic.longitude,
                latitude = pic.latitude,
                source = pic.source,
                name = pic.name,
                country = pic.country,
                city = pic.city
            }).ToList();

            foreach (var pic in tempList)//loop through all the Pics in tempList
            {
                AddPinToMap(DesignPin(pic), CreateBasicGeoPosition(pic));
            }
            if (tempList.Count == 0)
            {
                var dialog = new MessageDialog("You are not currently not tagged in any facebook pictures with locations.\nPlease Tag youself in a picture on facebook and give it a location and run the app again");
                dialog.Title = "No Tagged Photos Found With Locations";
                await dialog.ShowAsync();
            }
        }

        /*Method creates/returns a BasicGeoposition of lat/lon from the picture passed in.
        */
        private BasicGeoposition CreateBasicGeoPosition(FbPicInfo pic)
        {
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = pic.latitude;
            location.Longitude = pic.longitude;
            return location;
        }

        /*Method designs a new pin and adds it to the Bing Map
         */
        public Canvas DesignPin(FbPicInfo pic)
        {
            BitmapImage img = new BitmapImage(new Uri(pic.source, UriKind.Absolute));
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = img;

            Color color = Colors.Silver;
            Canvas canvasPinDesign = NewCanvas();//creates a new canvas

            var location = CreateBasicGeoPosition(pic);

            if (_geoLocDict.ContainsValue(location))//if duplicate location in the dictonary geoLocDict change ellipse colour to red
            {
                color = Colors.OrangeRed;
            }

            Ellipse ellipse = NewEllipse(color, imageBrush);//Create a new elipse
            canvasPinDesign.Children.Add(ellipse);//Add Ellipse to Canvas

            Button btn = CreateNewBtn(imageBrush);//Create A New Button
            canvasPinDesign.Children.Add(btn);//Add Btn to Canvas

            _geoLocDict.Add(img, location);//add the location to the list
            _tempInfoDict.Add(img.UriSource, pic);

            return canvasPinDesign;
        }

        /*Creates a new canvas object
        */
        private Canvas NewCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Height = 30;
            canvas.Width = 30;
            canvas.Margin = new Windows.UI.Xaml.Thickness(-10);
            return canvas;
        }

        /*Method designs a new Ellipse
         */
        private Ellipse NewEllipse(Color color, ImageBrush imgBrush)
        {
            Ellipse elip = new Ellipse();
            elip.Fill = imgBrush;
            elip.Stroke = new SolidColorBrush(color);
            elip.StrokeThickness = 1;
            elip.Width = 26;
            elip.Height = 26;

            return elip;
        }

        /*Creates a new button control with Lambda Click events to get the senders ImageBrush
         */
        private Button CreateNewBtn(ImageBrush imgBrush)
        {
            Button btn = new Button();
            btn.Opacity = 0;//Hide Btn Visability
            btn.Width = 26;
            btn.Height = 26;
            btn.Background = imgBrush;//Only use PointerEntered Event 

            //Lambda Expresion, Button Click event 
            btn.Click += (sender, eventArgs) =>
            {
                BitmapImage source = GetBtnBitmap(sender);
                DisplayImages(source);
                SpMap.Visibility = Visibility.Collapsed;
                FlipViewImgs.Visibility = Visibility.Visible;
                SpBackToMap.Visibility = Visibility.Visible;
            };
            return btn;
        }

        /* Method gets the sender and casts it as a button. 
         * it then gets the backround image and returns it as a BitMapImage
         */
        private BitmapImage GetBtnBitmap(object sender)
        {
            Button btnSender = sender as Button;//Cast the sender as a Button
            ImageBrush brush = btnSender.Background as ImageBrush;//Cast the img.background as a brush
            BitmapImage source = brush.ImageSource as BitmapImage;//Cast the imageSource as a BitMapImage
            return source;
        }

        /* Method used to get all images in a certain location and display them in a Flipview
        */
        private void DisplayImages(BitmapImage source)
        {
            BasicGeoposition geo;
            _geoLocDict.TryGetValue(source, out geo);//Search Dict for Key BMI and get value GEO
            var imgKeys = _geoLocDict.Where(pair => pair.Value.Equals(geo)).Select(pair => pair.Key);
            FbPicInfo picDetails;
            FlipViewImgs.Items.Clear();

            //For each img in imgKeys
            foreach (var i in imgKeys)
            {
                Image img = new Image();
                img.VerticalAlignment = VerticalAlignment.Center;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Source = new BitmapImage(i.UriSource);
                img.Stretch = Stretch.UniformToFill;
                _tempInfoDict.TryGetValue(i.UriSource, out picDetails);
                img.Name = picDetails.name;
                FlipViewImgs.Items.Add(img);//Add an Image to the Control        
            }
        }

        /*Adds a Custom pin(canvas) at the given location to the map
         */
        private void AddPinToMap(Canvas canvasPinDesign, BasicGeoposition location)
        {
            //Add The Pin to the map with the location passed in
            MapControl.SetLocation(canvasPinDesign, new Geopoint(location));
            BingMap.Children.Add(canvasPinDesign);//Add the pin to the map            
        }

        /*Method gets the currently selected item in the flipView, extracts the BitmapImage from it, then searches
         * the dictionary for the key and returns the value.
         */
        private void FlipViewImgs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FlipView name = sender as FlipView;
            var selectedItem = name.SelectedItem;
            var img = selectedItem as Image;

            if (img != null)
            {
                ImageSource imgSrc = img.Source;
                BitmapImage bmi = imgSrc as BitmapImage;

                FbPicInfo picDetails;
                var uri = bmi.UriSource;

                _tempInfoDict.TryGetValue(uri, out picDetails);
                textBlock.Text = picDetails.name;
                if (picDetails.city != null || picDetails.country != null)
                {
                    textBlock.Text += "\n" + picDetails.city + ", " + picDetails.country;
                }
                SpPicName.Visibility = Visibility.Visible;
            }
        }

        /*Method hides and unhides controls once clicked
         * Btn located inside flipview
         */
        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            DefaultView();
        }

        /*Method Reverts back to default view (the view from start up) 
         */
        private void DefaultView()
        {
            SpMap.Visibility = Visibility.Visible;
            FlipViewImgs.Visibility = Visibility.Collapsed;
            SpBackToMap.Visibility = Visibility.Collapsed;
            SpPicName.Visibility = Visibility.Collapsed;
            FlipViewImgs.Items.Clear();//Clear all the images in the flipView (imortant)
        }

        /*Deletes everything from the database
         */
        private void DeleteFromDB()
        {
            using (var db = new PicturePlaceDb())
            {
                foreach (var item in db.data)
                {
                    db.data.Remove(item);
                }
                db.SaveChanges();
            }
        }

        /*Logs the user out of facebook, erases the database and reverts back to default views etc
         */
        private async void LogOut()
        {
            await session.LogoutAsync();
            DeleteFromDB();
            OnLogin();
            DefaultView();
            BingMap.Children.Clear();
            _tempInfoDict.Clear();
            _geoLocDict.Clear();
        }

        /*LogoutBtn method displays a dialog box to the user with an option to log out or stay logged in
         */
        private async void LogOutBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog dialog = new MessageDialog("Logging out will Remove all user information");
            dialog.Title = "Log Out";
            dialog.Commands.Add(new UICommand("Yes") { Id = 0 });

            dialog.Commands.Add(new UICommand("No") { Id = 1 });
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            var result = await dialog.ShowAsync();

            if ((int)result.Id == 0)//If log out was yes, log out otherwise do nothing
                LogOut();
        }
    }
}