using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaceMyPicture
{
    public class PicturePlaceDb : DbContext
    {
        public DbSet<FbPicInfo> data { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=FacebookDb.db");
        }

    }


    public class FbPicInfo
    {
        public FbPicInfo() { }
        public int PicDatumId { get; set; }
        public string source { get; set; }

        //place stuff
        public string name { get; set; }

        //location stuff
        public string city { get; set; }
        public string country { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }

        public string id { get; set; }

    }

}