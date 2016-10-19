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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PlaceMyPicture
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            OnLogin();
        }
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
        }

        private async void OnGet()
        {
            string endpoint = "/me";//where the url starts from 



            //parameters.offset = 10;


            PropertySet parameters = new PropertySet();
            //me? fields = photos{ source,place}
            parameters.Add("fields", "photos{source,place}");//the fields you want to get
            //parameters.Add("fields", "photos{images}");//the fields you want to get


            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();//check to see if the Requets Succeeded 

            if (graphResult.Succeeded)
            {
                PicturePlaceObject profile = graphResult.Object as PicturePlaceObject;
                var lastImg = profile.photos.data.Count;

                //data[24] is the 24th image
                //string source = profile.photos.data[1].source.ToString();

                pic1.Source = new BitmapImage(new Uri(profile.photos.data[1].source.ToString(), UriKind.Absolute));
                pic2.Source = new BitmapImage(new Uri(profile.photos.data[2].source.ToString(), UriKind.Absolute));
                pic3.Source = new BitmapImage(new Uri(profile.photos.data[3].source.ToString(), UriKind.Absolute));
                pic4.Source = new BitmapImage(new Uri(profile.photos.data[4].source.ToString(), UriKind.Absolute));

                pic5.Source = new BitmapImage(new Uri(profile.photos.data[5].source.ToString(), UriKind.Absolute));
                pic6.Source = new BitmapImage(new Uri(profile.photos.data[6].source.ToString(), UriKind.Absolute));
                pic7.Source = new BitmapImage(new Uri(profile.photos.data[7].source.ToString(), UriKind.Absolute));
                pic8.Source = new BitmapImage(new Uri(profile.photos.data[8].source.ToString(), UriKind.Absolute));


                //MessageDialog dialog = new MessageDialog(source);
                // await dialog.ShowAsync();
            }
            else
            {
                MessageDialog dialog = new MessageDialog("Couldnt Find!");
                await dialog.ShowAsync();
            }
        }
    }
}
