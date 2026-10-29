using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MundoAwoo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdministracionCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAdministrador",
                table: "Usuarios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ItemsDeAvatar_CategoriaId_Nombre",
                table: "ItemsDeAvatar",
                columns: new[] { "CategoriaId", "Nombre" },
                unique: true);

            // La FK necesita un índice por CategoriaId mientras reemplazamos el anterior.
            migrationBuilder.DropIndex(
                name: "IX_ItemsDeAvatar_CategoriaId",
                table: "ItemsDeAvatar");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ItemsDeAvatar_CategoriaId",
                table: "ItemsDeAvatar",
                column: "CategoriaId");

            migrationBuilder.DropIndex(
                name: "IX_ItemsDeAvatar_CategoriaId_Nombre",
                table: "ItemsDeAvatar");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "EsAdministrador",
                table: "Usuarios");

        }
    }
}
