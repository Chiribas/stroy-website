using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkServicePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ServicePrices");

            migrationBuilder.DropColumn(
                name: "PriceTo",
                table: "ServicePrices");

            // Unit удаляем (старые значения вроде "м²" не должны утекать в Tag).
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ServicePrices");

            migrationBuilder.RenameColumn(
                name: "PriceFrom",
                table: "ServicePrices",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ServicePrices",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "ServicePrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArticleSlug",
                table: "ServicePrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "ServicePrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "ServicePrices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArticleSlug",
                table: "ServicePrices");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ServicePrices");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "ServicePrices");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "ServicePrices");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ServicePrices",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ServicePrices",
                newName: "PriceFrom");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ServicePrices",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ServicePrices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PriceTo",
                table: "ServicePrices",
                type: "INTEGER",
                nullable: true);
        }
    }
}
