using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KargoTakip.Altyapi.Migrations
{
    /// <inheritdoc />
    public partial class Ilk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gonderiler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TakipKodu = table.Column<string>(type: "text", nullable: false),
                    MusteriId = table.Column<Guid>(type: "uuid", nullable: false),
                    KuryeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    CikisAdresi = table.Column<string>(type: "text", nullable: false),
                    VarisAdresi = table.Column<string>(type: "text", nullable: false),
                    OlusturmaZamani = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeZamani = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gonderiler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GonderiOlaylari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GonderiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Onceki = table.Column<int>(type: "integer", nullable: true),
                    Sonraki = table.Column<int>(type: "integer", nullable: false),
                    Zaman = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Not = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GonderiOlaylari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Eposta = table.Column<string>(type: "text", nullable: false),
                    ParolaOzu = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kuryeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdSoyad = table.Column<string>(type: "text", nullable: false),
                    Telefon = table.Column<string>(type: "text", nullable: true),
                    Plaka = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kuryeler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Musteriler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdSoyad = table.Column<string>(type: "text", nullable: false),
                    Telefon = table.Column<string>(type: "text", nullable: true),
                    Adres = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Musteriler", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Kullanicilar",
                columns: new[] { "Id", "Eposta", "ParolaOzu", "Rol" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "admin@example.com", "$2a$11$j2BxBgufDJFgK8Wvz.zz9uQY7Cp1PYLnvO8YhTeX5LfSxFd2oYMZi", "Admin" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "kurye@example.com", "$2a$11$3GAWMtPwt3kMVtM5Cgh1we2N2hvz4yua0Jk3PzUK/hH6Dmum9oBHG", "Kurye" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gonderiler_TakipKodu",
                table: "Gonderiler",
                column: "TakipKodu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gonderiler");

            migrationBuilder.DropTable(
                name: "GonderiOlaylari");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "Kuryeler");

            migrationBuilder.DropTable(
                name: "Musteriler");
        }
    }
}
