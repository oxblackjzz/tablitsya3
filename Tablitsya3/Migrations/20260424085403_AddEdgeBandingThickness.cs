using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tablitsya3.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeBandingThickness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EdgeBandingThickness",
                table: "parts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EdgeBandingThickness",
                table: "parts");
        }
    }
}
