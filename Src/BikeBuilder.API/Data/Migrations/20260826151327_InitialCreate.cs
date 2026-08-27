using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BikeBuilder.API.Data.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.CreateTable(
        name: "BikeBuilds",
        columns: table => new
        {
          Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
          Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
          Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
          Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
        },
        constraints: table =>
        {
          table.PrimaryKey("PK_BikeBuilds", x => x.Id);
        });

    migrationBuilder.CreateTable(
        name: "Components",
        columns: table => new
        {
          Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
          Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
          Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
          Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
        },
        constraints: table =>
        {
          table.PrimaryKey("PK_Components", x => x.Id);
        });

    migrationBuilder.CreateTable(
        name: "BikeBuildComponents",
        columns: table => new
        {
          Id = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
          BikeBuildId = table.Column<int>(type: "int", nullable: false),
          ComponentId = table.Column<int>(type: "int", nullable: false),
          Quantity = table.Column<int>(type: "int", nullable: false),
          Date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
        },
        constraints: table =>
        {
          table.PrimaryKey("PK_BikeBuildComponents", x => x.Id);
          table.ForeignKey(
                    name: "FK_BikeBuildComponents_BikeBuilds_BikeBuildId",
                    column: x => x.BikeBuildId,
                    principalTable: "BikeBuilds",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
          table.ForeignKey(
                    name: "FK_BikeBuildComponents_Components_ComponentId",
                    column: x => x.ComponentId,
                    principalTable: "Components",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
        });

    migrationBuilder.CreateIndex(
        name: "IX_BikeBuildComponents_BikeBuildId",
        table: "BikeBuildComponents",
        column: "BikeBuildId");

    migrationBuilder.CreateIndex(
        name: "IX_BikeBuildComponents_ComponentId",
        table: "BikeBuildComponents",
        column: "ComponentId");
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.DropTable(
        name: "BikeBuildComponents");

    migrationBuilder.DropTable(
        name: "BikeBuilds");

    migrationBuilder.DropTable(
        name: "Components");
  }
}
