using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtSyncPro.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$/r0AgeKfrMInOX6Dz7pgweZHtZo.3Mo7qUtob4jtjYNltR/753//K");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$nKWvczi0OGM0Oxq6kf6rDOdMz3brJGZgb/RJMzx3tHlNorMEnfJPi");
        }
    }
}
