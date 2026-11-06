using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MundoAwoo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstudioPrincipal3D : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChaquetaGhetto",
                table: "Avatares",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ChaquetaQueen",
                table: "Avatares",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Personaje3D",
                table: "Avatares",
                type: "varchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ghetto")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChaquetaGhetto",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "ChaquetaQueen",
                table: "Avatares");

            migrationBuilder.DropColumn(
                name: "Personaje3D",
                table: "Avatares");
        }
    }
}
