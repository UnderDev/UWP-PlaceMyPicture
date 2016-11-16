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


namespace PlaceMyPicture
{
    public sealed partial class MainPage : Page
    {

        //private Dictionary<String, Datum> picDataDict;
        private Dictionary<BitmapImage, BasicGeoposition> geoLocDict;

        public MainPage()
        {
            this.InitializeComponent();
            init();
            OnLogin();


            //deleteFromDB();
        }

        private void init()
        {
            //picDataDict = new Dictionary<String, Datum>();
            geoLocDict = new Dictionary<BitmapImage, BasicGeoposition>();
            getUserLocation();
        }


        /*Method gets the users Current location and passes the Long/Lat to the Bing Map showing you your current location
         */
        private async void getUserLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();

            switch (accessStatus)
            {
                case GeolocationAccessStatus.Allowed:

                    // If DesiredAccuracy or DesiredAccuracyInMeters are not set (or value is 0), DesiredAccuracy.Default is used.
                    Geolocator _geolocator = new Geolocator { DesiredAccuracyInMeters = 10 };

                    // Carry out the operation.
                    Geoposition pos = await _geolocator.GetGeopositionAsync();
                    BasicGeoposition geo = new BasicGeoposition();

                    geo.Latitude = pos.Coordinate.Point.Position.Latitude;
                    geo.Longitude = pos.Coordinate.Point.Position.Longitude;

                    BingMap.Center = new Geopoint(geo);//Center Map on geolocation
                    BingMap.ZoomLevel = 7;//Sets the zoom level on the map
                    BingMap.Height = SpMap.ActualHeight;//important, sets height to stackpannels height
                    break;

                case GeolocationAccessStatus.Denied:
                    //Please turn your location on 
                    break;

                case GeolocationAccessStatus.Unspecified:
                    //Please turn your location on 
                    break;
            }
        }


        /*Facebook LogIn Authorization/Permissions
         */
        private async void OnLogin()
        {
            string Sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            FBSession session = FBSession.ActiveSession;

            session.WinAppId = Sid;//not used (only for windows 8 phone etc)
            session.FBAppId = (1083564175045990).ToString();//AppId From App Created on facebook

            List<String> permissionList = new List<String>();//list of all the permissions needed from the user
            permissionList.Add("public_profile");
            permissionList.Add("user_location");
            permissionList.Add("user_photos");

            FBPermissions permissions = new FBPermissions(permissionList);

            var result = await session.LoginAsync(permissions);
            if (result.Succeeded)
            {
                string name = session.User.Name;
                onSuccessLogin();//once the user has given permission and logged on
            }
            else
            {
                //Tell user they must log in to use the app
            }
        }

        /*On Successful login, send GET request to FB endpoint with params and DeserializeJson the results into objects/Dictonary
         */
        private async void onSuccessLogin()
        {
            string endpoint = "/me/photos";//where the url starts from 

            PropertySet parameters = new PropertySet();
            parameters.Add("fields", "source,place");//Required fields needed

            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();

            if (graphResult.Succeeded)//check to see if the Request Succeeded
            {

                PicturePlaceObject results = graphResult.Object as PicturePlaceObject;

                var db = new PicturePlaceDb();//WIPES PLACES IF RUN TWICE (CREATES A NEW INSTANCE OF PicturePlaceDb)   *** FIX ***
                //var db = (dataBase == null) ? new PicturePlaceDb() : dataBase;

                getAllFbPic(results, parameters, endpoint, db);
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Couldnt Find!");
                await dialog.ShowAsync();
            }
        }

        private async void getAllFbPic(PicturePlaceObject results, PropertySet parameters, string endpoint, PicturePlaceDb db)
        {
            Boolean addedAllPics = false;
            do
            {
                addedAllPics = sortPictures(results, db);//Add Results to a list

                if (addedAllPics ==false) {
                    parameters.Remove("after");//Remove previous parameters
                    parameters.Add("after", results.paging.cursors.after);//the next page to send the request too

                    FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
                    FBResult graphResult = await value.GetAsync();//check to see if the Requets Succeeded 
                    results = graphResult.Object as PicturePlaceObject;
                }

            } while ((results.paging != null || results.data.Count() != 0) && addedAllPics == false);
        }

        //REMOVES ALL DATA FROM DB
        private void deleteFromDB()
        {
            using (var db = new PicturePlaceDb())
            {
                foreach (var item in db.data)
                {
                    db.data.Remove(item);//.SELECTED ITEM (when u click on something)?
                }
                db.SaveChanges();
            }
        }

        /*Creates a dictonary with the object id as the key and the object iteself as the value
         */
        /*Creates a dictonary with the object id as the key and the object iteself as the value
        */
        private Boolean sortPictures(PicturePlaceObject results, PicturePlaceDb db)
        {
            Boolean addedAllPics = true;
            foreach (var pic in results.data)//loop through all the Pics in results.data
            {
                if (pic.place != null && pic.place.location != null)//Only add Pics that have a place/location from results
                {
                    var placeId = db.data.Select(id => id.id);//COLLECTION OF ALL THE IDS

                    //CHECK TO SEE IF THE DATABASE ALREADY CONTAINS THE PICTURE IF YES, THEN ALL PICTURES HAVE BEEN ADDED....... pic.place.id   ***** FIX *******
                    //if there are no pics or the picture id wasnt found

                    if (!placeId.Contains(pic.id) || placeId == null)
                    {
                        addedAllPics = false;
                        storePicture(pic, db);
                    }
                }
            }
            var tempList = db.data.AsEnumerable().Select(pic => new FbPicInfo
            {
                longitude = pic.longitude,
                latitude = pic.latitude,
                source = pic.source,
                name = pic.name,
                country = pic.country,
                city = pic.city
            }).ToList();

            paintPins(tempList);
            return addedAllPics;
        }

        private void storePicture(Datum pic, PicturePlaceDb db)
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

        private void paintPins(List<FbPicInfo> tempList)
        {
            foreach (var pic in tempList)//loop through all the Pics in results.data
            {
                BasicGeoposition geo = createBasicGeoPosition(pic);
                BitmapImage img = createBitMapImage(pic);
                Canvas pinDesign = designPin(geo, img);
                addPinToMap(pinDesign, geo);
            }
        }


        /*Method loops through all the pictures in picDataDict that have a location, gets image source, Long/Lat coords and passes it to the method createNewPin()
        */
        private BasicGeoposition createBasicGeoPosition(FbPicInfo pic)
        {
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = pic.latitude;
            location.Longitude = pic.longitude;

            return location;
            //createNewPin(location, img);
        }

        private BitmapImage createBitMapImage(FbPicInfo pic)
        {
            BitmapImage img = new BitmapImage(new Uri(pic.source, UriKind.Absolute));
            return img;
        }

        /*Creates a new pin(canvas/ellipse/button) and adds it to the Bing Map
         */
        public Canvas designPin(BasicGeoposition location, BitmapImage urlImage)
        {
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = urlImage;

            Color color = Colors.Coral;
            Canvas canvasPinDesign = newCanvas();//creates a new canvas

            if (geoLocDict.ContainsValue(location))//if i have a duplicate location in the dictonary geoLocDict change ellipse colour to red
            {
                color = Colors.Red;
            }

            Ellipse ellipse = newEllipse(color, imageBrush);//Create a new elipse
            canvasPinDesign.Children.Add(ellipse);//Add Ellipse to Canvas

            Button btn = newButton(imageBrush);//Create A New Button
            canvasPinDesign.Children.Add(btn);//Add Btn to Canvas

            geoLocDict.Add(urlImage, location);//add the location to the list
            return canvasPinDesign;
        }

        /*Creates a new canvas object
        */
        private Canvas newCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Height = 30;
            canvas.Width = 30;
            canvas.Margin = new Windows.UI.Xaml.Thickness(-10);
            return canvas;
        }

        private Ellipse newEllipse(Color color, ImageBrush imgBrush)
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
        private Button newButton(ImageBrush imgBrush)
        {
            Button btn = new Button();
            btn.Opacity = 0;//Hide Btn Visability
            btn.Width = 26;
            btn.Height = 26;
            btn.Background = imgBrush;//Only use PointerEntered Event 

            //Lambda Expresion, Button Click event 
            btn.Click += (sender, eventArgs) =>
            {
                BitmapImage source = getBtnBitmap(sender);
                displayImages(source);
                SpMap.Visibility = Visibility.Collapsed;
                SpFlipImages.Visibility = Visibility.Visible;
                SpBackToMap.Visibility = Visibility.Visible;
            };

            //Lambda Expression, When The Button Gets Hovered Over Do Something (NOT USED) MABY BLOW MAIN IMAGE UP -------------------------------------
            btn.PointerEntered += (sender, eventArgs) =>
            {
                BitmapImage source = getBtnBitmap(sender);
            };

            return btn;
        }

        /* Method gets the sender and casts it as a button. 
         * it then gets the backround image and returns it as a BitMapImage
         */
        private BitmapImage getBtnBitmap(object sender)
        {
            Button btnSender = sender as Button;//Cast the sender as a Button
            ImageBrush brush = btnSender.Background as ImageBrush;//Cast the img.background as a brush
            BitmapImage source = brush.ImageSource as BitmapImage;//Cast the imageSource as a BitMapImage
            return source;
        }

        /* Displayes the image in the header
         * Method used to get all images that are over lapping eachother
        */
        private void displayImages(BitmapImage source)
        {
            BasicGeoposition geo;
            geoLocDict.TryGetValue(source, out geo);//Search Dict for Key BMI and get value GEO
            var imgKeys = geoLocDict.Where(pair => pair.Value.Equals(geo)).Select(pair => pair.Key);

            FlipViewImgs.Items.Clear();//CLEAR ALL THE PRIVIOUS IMAGES (MABY KEEP) *DUPLICATES ADDED TOO OTHERWISE ------------------------------

            //For each img in imgKeys
            foreach (var i in imgKeys)
            {
                Image img = new Image();
                img.VerticalAlignment = VerticalAlignment.Center;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Source = new BitmapImage(i.UriSource);
                FlipViewImgs.Items.Add(img);//Add an Image to the Control        
            }
        }

        /*Adds a Custom pin(canvas) at the given location to the map
         */
        private void addPinToMap(Canvas canvasPinDesign, BasicGeoposition location)
        {
            //Add The Pin to the map with the location passed in
            MapControl.SetLocation(canvasPinDesign, new Geopoint(location));
            BingMap.Children.Add(canvasPinDesign);//Add the pin to the map            
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SpMap.Visibility = Visibility.Visible;
            SpFlipImages.Visibility = Visibility.Collapsed;
            SpBackToMap.Visibility = Visibility.Collapsed;
        }

    }
}
