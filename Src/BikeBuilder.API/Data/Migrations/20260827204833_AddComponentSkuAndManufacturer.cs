using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeBuilder.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentSkuAndManufacturer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Components",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Components",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Components");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Components");
        }
    }
}
