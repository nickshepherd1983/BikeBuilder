using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeBuilder.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Information",
                table: "Components",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Information",
                table: "Components");
        }
    }
}
