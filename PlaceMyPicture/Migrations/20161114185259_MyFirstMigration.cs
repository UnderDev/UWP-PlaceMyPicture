using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlaceMyPicture.Migrations
{
    public partial class MyFirstMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PicCursors",
                columns: table => new
                {
                    PicCursorsId = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    after = table.Column<string>(nullable: true),
                    before = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PicCursors", x => x.PicCursorsId);
                });

            migrationBuilder.CreateTable(
                name: "PicLocation",
                columns: table => new
                {
                    PicLocationId = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    city = table.Column<string>(nullable: true),
                    country = table.Column<string>(nullable: true),
                    latitude = table.Column<double>(nullable: false),
                    longitude = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PicLocation", x => x.PicLocationId);
                });

            migrationBuilder.CreateTable(
                name: "paging",
                columns: table => new
                {
                    PicPagingId = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    cursorsPicCursorsId = table.Column<int>(nullable: true),
                    next = table.Column<string>(nullable: true),
                    previous = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paging", x => x.PicPagingId);
                    table.ForeignKey(
                        name: "FK_paging_PicCursors_cursorsPicCursorsId",
                        column: x => x.cursorsPicCursorsId,
                        principalTable: "PicCursors",
                        principalColumn: "PicCursorsId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PicPlace",
                columns: table => new
                {
                    PicPlaceId = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    locationPicLocationId = table.Column<int>(nullable: true),
                    name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PicPlace", x => x.PicPlaceId);
                    table.ForeignKey(
                        name: "FK_PicPlace_PicLocation_locationPicLocationId",
                        column: x => x.locationPicLocationId,
                        principalTable: "PicLocation",
                        principalColumn: "PicLocationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "data",
                columns: table => new
                {
                    PicDatumId = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    id = table.Column<string>(nullable: true),
                    placePicPlaceId = table.Column<int>(nullable: true),
                    source = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data", x => x.PicDatumId);
                    table.ForeignKey(
                        name: "FK_data_PicPlace_placePicPlaceId",
                        column: x => x.placePicPlaceId,
                        principalTable: "PicPlace",
                        principalColumn: "PicPlaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_data_placePicPlaceId",
                table: "data",
                column: "placePicPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_paging_cursorsPicCursorsId",
                table: "paging",
                column: "cursorsPicCursorsId");

            migrationBuilder.CreateIndex(
                name: "IX_PicPlace_locationPicLocationId",
                table: "PicPlace",
                column: "locationPicLocationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data");

            migrationBuilder.DropTable(
                name: "paging");

            migrationBuilder.DropTable(
                name: "PicPlace");

            migrationBuilder.DropTable(
                name: "PicCursors");

            migrationBuilder.DropTable(
                name: "PicLocation");
        }
    }
}
