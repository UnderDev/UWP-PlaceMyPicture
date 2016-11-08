using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using winsdkfb;
using winsdkfb.Graph;

namespace PlaceMyPicture.Data
{
    class Pictures
    {
        private Dictionary<String, Datum> picDataDict;


        private async void onSuccessLogin()
        {
            string endpoint = "/me/photos";//where the url starts from 

            PropertySet parameters = new PropertySet();
            parameters.Add("fields", "source,place");//Required fields needed

            FBSingleValue value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
            FBResult graphResult = await value.GetAsync();

            if (graphResult.Succeeded)//check to see if the Requets Succeeded
            {

                PicturePlaceObject results = graphResult.Object as PicturePlaceObject;

                while (results.paging != null || results.data.Count != 0)
                {
                    addPicToList(results);//Add Results to a list

                    parameters.Remove("after");//Remove previous parameters
                    parameters.Add("after", results.paging.cursors.after);//the next page to send the request too

                    value = new FBSingleValue(endpoint, parameters, DeserializeJson.FromJson);//send the request and get back a JSON responce
                    graphResult = await value.GetAsync();//check to see if the Requets Succeeded 
                    results = graphResult.Object as PicturePlaceObject;
                }

                //makeCustomPin();//makes pins from FB images that have locations added
            }
            else
            {
                //no pics couldnt find
            }
        }

        /*Creates a dictonary with the object id as the key and the object iteself as the value
    */
        private void addPicToList(PicturePlaceObject results)
        {
            foreach (var pic in results.data)
            {
                picDataDict.Add(pic.id, pic);
            }
        }
    }
}
