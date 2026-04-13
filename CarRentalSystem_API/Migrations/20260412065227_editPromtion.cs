using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalSystem_API.Migrations
{
    /// <inheritdoc />
    public partial class editPromtion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableModel",
                table: "Promotions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicableModel",
                table: "Promotions");
        }
    }
}
