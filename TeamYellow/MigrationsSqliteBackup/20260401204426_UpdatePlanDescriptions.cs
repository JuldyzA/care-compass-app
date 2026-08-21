using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamYellow.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 1,
                column: "planDescription",
                value: "Basic access for new counsellors exploring the platform");

            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 2,
                column: "planDescription",
                value: "Full platform access with flexible month-to-month billing");

            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 3,
                column: "planDescription",
                value: "Full platform access with annual billing and better long-term value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 1,
                column: "planDescription",
                value: "Free basic access");

            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 2,
                column: "planDescription",
                value: "Monthly subscription");

            migrationBuilder.UpdateData(
                table: "Plan",
                keyColumn: "pkPlanId",
                keyValue: 3,
                column: "planDescription",
                value: "Yearly subscription");
        }
    }
}
