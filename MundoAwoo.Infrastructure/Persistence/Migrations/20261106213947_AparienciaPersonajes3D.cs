using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MundoAwoo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AparienciaPersonajes3D : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarbaGhetto",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "original")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BarbaQueen",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "original")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorCabelloGhetto",
                table: "Avatares",
                type: "char(7)",
                nullable: false,
                defaultValue: "#201813")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorCabelloQueen",
                table: "Avatares",
                type: "char(7)",
                nullable: false,
                defaultValue: "#29161C")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PeloGhetto",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "original")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PeloQueen",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "original")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TatuajesGhetto",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ninguno")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TatuajesQueen",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ninguno")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarbaGhetto",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "BarbaQueen",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "ColorCabelloGhetto",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "ColorCabelloQueen",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "PeloGhetto",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "PeloQueen",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "TatuajesGhetto",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "TatuajesQueen",
                table: "Avatares");
        }
    }
}
