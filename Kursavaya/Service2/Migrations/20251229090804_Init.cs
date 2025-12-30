using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentService.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tournament");

            migrationBuilder.CreateTable(
                name: "Tournaments",
                schema: "tournament",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Game = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxTeams = table.Column<int>(type: "integer", nullable: false),
                    CurrentTeams = table.Column<int>(type: "integer", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PrizePool = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegistrationStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegistrationEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TournamentStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StreamUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_CreatedAt",
                schema: "tournament",
                table: "Tournaments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Game",
                schema: "tournament",
                table: "Tournaments",
                column: "Game");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_OrganizerId",
                schema: "tournament",
                table: "Tournaments",
                column: "OrganizerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Status",
                schema: "tournament",
                table: "Tournaments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tournaments",
                schema: "tournament");
        }
    }
}
