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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PlaceMyPicture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private Dictionary<String, Datum> picDataDict;
        private Dictionary<BitmapImage, BasicGeoposition> geoLocList;

        List<Image> imgControlList = new List<Image>();


        public MainPage()
        {
            this.InitializeComponent();
            OnLogin();
            init();
        }

        private void init()
        {
            picDataDict = new Dictionary<String, Datum>();
            geoLocList = new Dictionary<BitmapImage, BasicGeoposition>();
            //getImgControls();
        }

        /*
         * Facebook LogIn Authorization/Permissions
         */
        private async void OnLogin()
        {
            string Sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            FBSession session = FBSession.ActiveSession;

            session.WinAppId = Sid;//not used (only for windows 8 phone etc)
            session.FBAppId = (1083564175045990).ToString();//AppId From App Created on facebook

            List<String> permissionList = new List<String>();//list of all the permissions needed from the user
            permissionList.Add("public_profile");
            permissionList.Add("user_likes");
            permissionList.Add("user_location");
            permissionList.Add("user_photos");
            permissionList.Add("publish_actions");
            permissionList.Add("email");

            FBPermissions permissions = new FBPermissions(permissionList);

            var result = await session.LoginAsync(permissions);
            if (result.Succeeded)
            {
                string name = session.User.Name;
                OnGet();//once the user has given permission and logged on
            }
            else
            {
                //Tell user they must log in to use the app
            }
        }

        private async void OnGet()
        {
            //Loop over pictures till all have been added 

            string endpoint = "/me/photos";//where the url starts from 

            PropertySet parameters = new PropertySet();
            parameters.Add("fields", "source,place");//the fields you want to get

            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();//check to see if the Requets Succeeded 

            if (graphResult.Succeeded)
            {
                PicturePlaceObject results = graphResult.Object as PicturePlaceObject;
                int i = 0;
                while (results.paging != null || results.data.Count != 0)
                {
                    addPicToList(results);//Add Results to a list

                    parameters.Remove("after");
                    parameters.Add("after", results.paging.cursors.after);//the next page to send the request too

                    value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
                    graphResult = await value.GetAsync();//check to see if the Requets Succeeded 
                    results = graphResult.Object as PicturePlaceObject;
                }

                foreach (var p in picDataDict)
                {
                    //Dont add pictures that dont have a long/lat
                    if (p.Value.place != null && p.Value.place.location != null)
                    {
                        BitmapImage img = new BitmapImage(new Uri(p.Value.source, UriKind.Absolute));
                        double lon = p.Value.place.location.longitude;
                        double lat = p.Value.place.location.latitude;

                        AddMapPin(img, lon, lat);//Make A New Pin
                    }
                }

            }
            else
            {
                MessageDialog dialog = new MessageDialog("Couldnt Find!");
                await dialog.ShowAsync();
            }

        }

        private void addPicToList(PicturePlaceObject results)
        {
            foreach (var p in results.data)
            {
                picDataDict.Add(p.id, p);
            }
        }




        private void AddMapPin(BitmapImage img, double lon, double lat)
        {
            BasicGeoposition location = new BasicGeoposition();
            location.Latitude = lat;
            location.Longitude = lon;

            //Create the New Pin
            CreateNewPin(location, img);

            //Map.MapElements.Add(mapIcon);
            Map.Center = new Geopoint(location);//center the map on the given location (MABY CURRENT)
        }

        public void CreateNewPin(BasicGeoposition location, BitmapImage urlImage)
        {
            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = urlImage;

            Color color = Colors.Coral;
            Canvas pinDesign = newCanvas();//creates a new canvas


            if (geoLocList.ContainsValue(location))
            {
                color = Colors.Red;
            }

            Ellipse ellipse = newEllipse(color, imageBrush);//Create a new elipse
            pinDesign.Children.Add(ellipse);//Add Ellipse to Canvas

            Button btn = newButton(imageBrush);//Create A New Button
            pinDesign.Children.Add(btn);//Add Btn to Canvas


            //Add The Pin to the map and the location passed in
            MapControl.SetLocation(pinDesign, new Geopoint(location));
            Map.Children.Add(pinDesign);//Add the pin to the map
            Map.ZoomLevel = 7;//SETS THE ZOOM LVL ON THE MAP


            geoLocList.Add(urlImage, location);//add the location to the list
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
            };

            //Lambda Expression, When The Button Gets Hovered Over Do Something (NOT USED) MABY BLOW MAIN IMAGE UP -------------------------------------
            btn.PointerEntered += (sender, eventArgs) =>
            {
                BitmapImage source = getBtnBitmap(sender);
            };

            return btn;
        }

        /* Method gets the sendr and casts it as a button. 
         * it then gets the backround image and returns it as a BitMapImage
         */
        private BitmapImage getBtnBitmap(object sender)
        {
            Button btnSender = sender as Button;//Cast the sender as a Button
            ImageBrush brush = btnSender.Background as ImageBrush;//Cast the img.background as a
            BitmapImage source = brush.ImageSource as BitmapImage;//Cast the imageSource as a BitMapImage
            return source;
        }

        /* Displayes the image in the header
         * Method used to get all images that are over lapping eachother
        */
        private void displayImages(BitmapImage source)
        {
            BasicGeoposition geo;
            geoLocList.TryGetValue(source, out geo);//Search Dict for Key BMI and get value BG
            var imgKeys = geoLocList.Where(pair => pair.Value.Equals(geo)).Select(pair => pair.Key);

            FlipViewImgs.Items.Clear();//CLEAR ALL THE PRIVIOUS IMAGES (MABY KEEP) *DUPLICATES ADDED TOO OTHERWISE ------------------------------

            //For each img in imgKeys
            foreach (var img in imgKeys)
            {
                //If statment just used until i create the images At Runtime and stick them in a stackpanel/Slider
                    Image i = new Image();
                    
                    i.Source = new BitmapImage(img.UriSource);
                    FlipViewImgs.Items.Add(i);//Add an Image to the Control        
            }

 

        }

        //private void getImgControls()
        //{         
        //    foreach (Image b in imgStackPanel.Children)
        //    {
        //        imgControlList.Add(b);
        //    }
        //}


        private Canvas newCanvas()
        {
            Canvas canvas = new Canvas();
            canvas.Height = 30;
            canvas.Width = 30;
            canvas.Margin = new Windows.UI.Xaml.Thickness(-10);

            return canvas;
        }
    }
}
