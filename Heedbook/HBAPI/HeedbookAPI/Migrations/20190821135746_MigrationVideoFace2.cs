using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UserOperations.Migrations
{
    public partial class MigrationVideoFace2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideoFaces",
                columns: table => new
                {
                    VideoFaceId = table.Column<Guid>(nullable: false),
                    FileVideoId = table.Column<Guid>(nullable: false),
                    FaceId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoFaces", x => x.VideoFaceId);
                    table.ForeignKey(
                        name: "FK_VideoFaces_FileVideos_FileVideoId",
                        column: x => x.FileVideoId,
                        principalTable: "FileVideos",
                        principalColumn: "FileVideoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoFaces_FileVideoId",
                table: "VideoFaces",
                column: "FileVideoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoFaces");
        }
    }
}
