using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtSyncPro.Migrations
{
    /// <inheritdoc />
    public partial class Dynamic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$CBAz2/O9k2dI3gyKBY7gAeYYvIIQkA9x75cIJx8sXyeV691MagFJy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$EZNToxvX7b9VO2dD7Ii4cei0pEmMdRBPRYsXvZ3UDWI.aS6GzZVeS");
        }
    }
}
