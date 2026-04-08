using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarRentalSystem_API.Migrations
{
    /// <inheritdoc />
    public partial class tokenUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenActivities_Users_UserID",
                table: "TokenActivities");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "TokenActivities",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_TokenActivities_Users_UserID",
                table: "TokenActivities",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenActivities_Users_UserID",
                table: "TokenActivities");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "TokenActivities",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenActivities_Users_UserID",
                table: "TokenActivities",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
