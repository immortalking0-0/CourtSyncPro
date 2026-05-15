using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtSyncPro.Migrations
{
    /// <inheritdoc />
    public partial class Tournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$nKWvczi0OGM0Oxq6kf6rDOdMz3brJGZgb/RJMzx3tHlNorMEnfJPi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$1x7v19YeBpAc2GFoXrcw1uHn9AafHIdiDVBBHM8yaaqmoKd2TpC9u");
        }
    }
}
