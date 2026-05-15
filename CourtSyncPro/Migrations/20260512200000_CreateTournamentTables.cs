using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtSyncPro.Migrations
{
    /// <inheritdoc />
    public partial class CreateTournamentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    TournamentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SportType = table.Column<int>(type: "int", nullable: false),
                    TournamentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourtId = table.Column<int>(type: "int", nullable: false),
                    MaxTeams = table.Column<int>(type: "int", nullable: false),
                    RegistrationFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PrizeInformation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByOwnerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.TournamentId);
                    table.ForeignKey(
                        name: "FK_Tournaments_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "CourtId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TournamentRegistrations",
                columns: table => new
                {
                    RegistrationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumberOfPlayers = table.Column<int>(type: "int", nullable: false),
                    FeePaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRegistrations", x => x.RegistrationId);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TournamentRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_CourtId",
                table: "Tournaments",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_TournamentId",
                table: "TournamentRegistrations",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRegistrations_UserId",
                table: "TournamentRegistrations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TournamentRegistrations");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
