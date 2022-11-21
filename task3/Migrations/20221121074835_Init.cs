using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace emotionswpf.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "images",
                columns: table => new
                {
                    ImageInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    filename = table.Column<string>(type: "TEXT", nullable: false),
                    path = table.Column<string>(type: "TEXT", nullable: false),
                    hash = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_images", x => x.ImageInfoId);
                });

            migrationBuilder.CreateTable(
                name: "emotions",
                columns: table => new
                {
                    EmotionId = table.Column<int>(name: "Emotion_Id", type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    value = table.Column<float>(type: "REAL", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    ImageInfoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emotions", x => x.EmotionId);
                    table.ForeignKey(
                        name: "FK_emotions_images_ImageInfoId",
                        column: x => x.ImageInfoId,
                        principalTable: "images",
                        principalColumn: "ImageInfoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "values",
                columns: table => new
                {
                    ImageInfoId = table.Column<int>(type: "INTEGER", nullable: false),
                    data = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_values", x => x.ImageInfoId);
                    table.ForeignKey(
                        name: "FK_values_images_ImageInfoId",
                        column: x => x.ImageInfoId,
                        principalTable: "images",
                        principalColumn: "ImageInfoId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_emotions_ImageInfoId",
                table: "emotions",
                column: "ImageInfoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "emotions");

            migrationBuilder.DropTable(
                name: "values");

            migrationBuilder.DropTable(
                name: "images");
        }
    }
}
