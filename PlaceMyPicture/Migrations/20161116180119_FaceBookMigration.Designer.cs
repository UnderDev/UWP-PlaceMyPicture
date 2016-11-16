using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PlaceMyPicture;

namespace PlaceMyPicture.Migrations
{
    [DbContext(typeof(PicturePlaceDb))]
    [Migration("20161116180119_FaceBookMigration")]
    partial class FaceBookMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1");

            modelBuilder.Entity("PlaceMyPicture.FbPicInfo", b =>
                {
                    b.Property<string>("id");

                    b.Property<int>("PicDatumId");

                    b.Property<string>("city");

                    b.Property<string>("country");

                    b.Property<double>("latitude");

                    b.Property<double>("longitude");

                    b.Property<string>("name");

                    b.Property<string>("source");

                    b.HasKey("id");

                    b.ToTable("data");
                });
        }
    }
}
