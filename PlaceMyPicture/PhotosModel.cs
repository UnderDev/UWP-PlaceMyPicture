using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testApi_Facebook
{
    public class Image
    {
        public int height { get; set; }
        public string source { get; set; }
        public int width { get; set; }
    }

    public class Datum
    {
        public List<Image> images { get; set; }
        public string id { get; set; }
    }

    public class Cursors
    {
        public string before { get; set; }
        public string after { get; set; }
    }

    public class Paging
    {
        public Cursors cursors { get; set; }
        public string next { get; set; }
    }

    public class Photos
    {
        public List<Datum> data { get; set; }
        public Paging paging { get; set; }
    }

    public class RootObject
    {
        public Photos photos { get; set; }
        public string id { get; set; }
    }
}
