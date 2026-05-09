using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtSyncPro.Migrations
{
    /// <inheritdoc />
    public partial class FixPromoCodeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$dIF7y5.7OK7EqQHQ7BX9n.uMclwec0AV.CYKOzqRydsGJbWtN1Lie");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Bookings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Admins",
                keyColumn: "AdminId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Tic/jFMBZSbYWMt6L3AC3ODjSGih6/RYzCTdCI2kAdy1WuRqfcFCi");
        }
    }
}
