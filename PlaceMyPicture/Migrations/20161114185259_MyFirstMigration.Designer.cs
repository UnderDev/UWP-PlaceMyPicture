using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PlaceMyPicture;

namespace PlaceMyPicture.Migrations
{
    [DbContext(typeof(PicturePlaceDb))]
    [Migration("20161114185259_MyFirstMigration")]
    partial class MyFirstMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1");

            modelBuilder.Entity("PlaceMyPicture.PicCursors", b =>
                {
                    b.Property<int>("PicCursorsId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("after");

                    b.Property<string>("before");

                    b.HasKey("PicCursorsId");

                    b.ToTable("PicCursors");
                });

            modelBuilder.Entity("PlaceMyPicture.PicDatum", b =>
                {
                    b.Property<int>("PicDatumId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("id");

                    b.Property<int?>("placePicPlaceId");

                    b.Property<string>("source");

                    b.HasKey("PicDatumId");

                    b.HasIndex("placePicPlaceId");

                    b.ToTable("data");
                });

            modelBuilder.Entity("PlaceMyPicture.PicLocation", b =>
                {
                    b.Property<int>("PicLocationId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("city");

                    b.Property<string>("country");

                    b.Property<double>("latitude");

                    b.Property<double>("longitude");

                    b.HasKey("PicLocationId");

                    b.ToTable("PicLocation");
                });

            modelBuilder.Entity("PlaceMyPicture.PicPaging", b =>
                {
                    b.Property<int>("PicPagingId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("cursorsPicCursorsId");

                    b.Property<string>("next");

                    b.Property<string>("previous");

                    b.HasKey("PicPagingId");

                    b.HasIndex("cursorsPicCursorsId");

                    b.ToTable("paging");
                });

            modelBuilder.Entity("PlaceMyPicture.PicPlace", b =>
                {
                    b.Property<int>("PicPlaceId")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("locationPicLocationId");

                    b.Property<string>("name");

                    b.HasKey("PicPlaceId");

                    b.HasIndex("locationPicLocationId");

                    b.ToTable("PicPlace");
                });

            modelBuilder.Entity("PlaceMyPicture.PicDatum", b =>
                {
                    b.HasOne("PlaceMyPicture.PicPlace", "place")
                        .WithMany()
                        .HasForeignKey("placePicPlaceId");
                });

            modelBuilder.Entity("PlaceMyPicture.PicPaging", b =>
                {
                    b.HasOne("PlaceMyPicture.PicCursors", "cursors")
                        .WithMany()
                        .HasForeignKey("cursorsPicCursorsId");
                });

            modelBuilder.Entity("PlaceMyPicture.PicPlace", b =>
                {
                    b.HasOne("PlaceMyPicture.PicLocation", "location")
                        .WithMany()
                        .HasForeignKey("locationPicLocationId");
                });
        }
    }
}
