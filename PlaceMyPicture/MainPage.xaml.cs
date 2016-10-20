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
using static testApi_Facebook.PicturePlaceModel;

//maps 
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Shapes;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PlaceMyPicture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int picNum= 0;
        PicturePlaceObject profile;
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
                OnGet();//once the user has given permissin and logged on
            }
            else
            {
                //Tell user they must log in to use the app
            }
        }

        private async void OnGet()
        {
            string endpoint = "/me";//where the url starts from 

            PropertySet parameters = new PropertySet();
            //me? fields = photos{ source,place}
            parameters.Add("fields", "photos{source,place}");//the fields you want to get
            parameters.Add("after", "TVRBeE5UWTVPRGcxTURJd01UQTFNVEU2TVRRMk5UVXhNRFV6T0Rvek9UUXdPRGsyTkRBMk5EYzRNelk9");
            //parameters.Add("fields", "photos{images}");//the fields you want to get


            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();//check to see if the Requets Succeeded 

            if (graphResult.Succeeded)
            {
                profile = graphResult.Object as PicturePlaceObject;
                var lastImg = profile.photos.data.Count;

                //data[24] is the 24th image
                //string source = profile.photos.data[1].source.ToString();

                pic1.Source = new BitmapImage(new Uri(profile.photos.data[1].source.ToString(), UriKind.Absolute));
                pic2.Source = new BitmapImage(new Uri(profile.photos.data[2].source.ToString(), UriKind.Absolute));
                pic3.Source = new BitmapImage(new Uri(profile.photos.data[3].source.ToString(), UriKind.Absolute));
                pic4.Source = new BitmapImage(new Uri(profile.photos.data[4].source.ToString(), UriKind.Absolute));
                foreach (var p in profile.photos.data)
                {
                    //Dont add pictures that dont have a long/lat
                    if (p.place != null) {
                        BitmapImage img = new BitmapImage(new Uri(p.source.ToString(), UriKind.Absolute));
                        double lon = p.place.location.longitude;
                        double lat = p.place.location.latitude;
                        AddMapPin(img,lon,lat);
                    }
                }

            }
            else
            {
                MessageDialog dialog = new MessageDialog("Couldnt Find!");
                await dialog.ShowAsync();
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
            btn.Click += Btn_Click;
            btn.Opacity = 0;
            btn.Width = 24;
            btn.Height = 24;
            int t = 0;

            var pinDesign = new Grid()
            {
                Height = 30,
                Width = 30,
                Margin = new Windows.UI.Xaml.Thickness(-10),
                Name = t++.ToString(),
            };

            pinDesign.Children.Add(new Ellipse()
            {
                Fill = imageBrush,
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                Width = 24,
                Height = 24,        
            });
            pinDesign.Children.Add(btn);

            //pinDesign.Children.Add(new Image()
            //{
            //    Width = 30,                
            //    Source = new BitmapImage(new Uri(urlImage, UriKind.Absolute)),
            //    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
            //    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
            //});

            MapControl.SetLocation(pinDesign, new Geopoint(location));
            Map.Children.Add(pinDesign);
        }

        private void Btn_Click(object sender, RoutedEventArgs e)
        {           
            pic4.Source = new BitmapImage(new Uri(profile.photos.data[picNum++].source.ToString(), UriKind.Absolute));
        }
    }
}
