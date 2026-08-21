using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamYellow.Migrations
{
    /// <inheritdoc />
    public partial class AddArchivedEmailDisplayAndUserLogEmailSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "userEmailSnapshot",
                table: "UserLog",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "archivedEmailDisplay",
                table: "Counsellor",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "archivedNormalizedEmail",
                table: "Counsellor",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counsellor_archivedNormalizedEmail",
                table: "Counsellor",
                column: "archivedNormalizedEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Counsellor_archivedNormalizedEmail",
                table: "Counsellor");

            migrationBuilder.DropColumn(
                name: "userEmailSnapshot",
                table: "UserLog");

            migrationBuilder.DropColumn(
                name: "archivedEmailDisplay",
                table: "Counsellor");

            migrationBuilder.DropColumn(
                name: "archivedNormalizedEmail",
                table: "Counsellor");
        }
    }
}
