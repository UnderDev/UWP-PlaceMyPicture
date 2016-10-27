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
        private Dictionary<String, Datum> picDataDict = new Dictionary<String, Datum>();
        public MainPage()
        {
            this.InitializeComponent();
            OnLogin();
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

                //string source = results.data[1].source.ToString();
                //pic1.Source = new BitmapImage(new Uri(pictures[results.id].source.ToString(), UriKind.Absolute));
                //pic2.Source = new BitmapImage(new Uri(results.data[2].source.ToString(), UriKind.Absolute));
                //pic3.Source = new BitmapImage(new Uri(results.data[3].source.ToString(), UriKind.Absolute));
                //pic4.Source = new BitmapImage(new Uri(results.data[4].source.ToString(), UriKind.Absolute));

                foreach (var p in picDataDict)
                {
                    //Dont add pictures that dont have a long/lat
                    if (p.Value.place != null && p.Value.place.location != null)
                    {
                        BitmapImage img = new BitmapImage(new Uri(p.Value.source.ToString(), UriKind.Absolute));
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

            Button btn = new Button();

            //var pinDesign = new Grid()
            var pinDesign = new Canvas()
            {
                Height = 30,
                Width = 30,
                Margin = new Windows.UI.Xaml.Thickness(-10),
            };

            pinDesign.Children.Add(new Ellipse()
            {
                Fill = imageBrush,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                Width = 24,
                Height = 24,
            });

            //Lambda Expresion has a click event then changes the picture source
            btn.Click += (sender, eventArgs) =>
            {
                pic4.Source = urlImage;//Image Control on MainPage.xmaml
                Canvas.SetZIndex(btn, -1); //Not Working zindex changes position 
            };

            btn.Opacity = 0;
            btn.Width = 24;
            btn.Height = 24;

            //Canvas.SetZIndex(btn, 0); NOT WORKING

            pinDesign.Children.Add(btn);//Add the btn as a child element      
            MapControl.SetLocation(pinDesign, new Geopoint(location));
            Map.Children.Add(pinDesign);//Add the pin to the map
            Map.ZoomLevel = 7;//SETS THE ZOOM LVL ON THE MAP
        }
    }
}
