using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static testApi_Facebook.PicturePlaceModel;

namespace PlaceMyPicture
{
    class DeserializeJson
    {
        //Gets the class PicturePlaceObject and parses through the json responce Deserializing it into the class
        public static PicturePlaceObject FromJson(string jsonText)
        {
            dynamic jsonObject = JsonConvert.DeserializeObject<PicturePlaceObject>(jsonText);
            return jsonObject;
        }
    }
}
