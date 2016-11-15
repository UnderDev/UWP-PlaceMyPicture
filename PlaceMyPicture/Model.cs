using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaceMyPicture
{
    public class PicturePlaceDb : DbContext
    {
        public DbSet<PicDatum> data { get; set; }
        public DbSet<PicPaging> paging { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=FbPicDb.db");
        }

    }


    public class PicLocation
    {
        public int PicLocationId { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class PicPlace
    {
        public int PicPlaceId { get; set; }
        public string name { get; set; }
        public PicLocation location { get; set; }
    }

    public class PicDatum
    {
        public int PicDatumId { get; set; }
        public string source { get; set; }
        public PicPlace place { get; set; }
        public string id { get; set; }
    }

    public class PicCursors
    {
        public int PicCursorsId { get; set; }
        public string before { get; set; }
        public string after { get; set; }
    }

    public class PicPaging
    {
        public int PicPagingId { get; set; }
        public PicCursors cursors { get; set; }
        public string next { get; set; }
        public string previous { get; set; }
        
    }

}
