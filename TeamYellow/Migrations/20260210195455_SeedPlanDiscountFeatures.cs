using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TeamYellow.Migrations
{
    /// <inheritdoc />
    public partial class SeedPlanDiscountFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Discount",
                columns: new[] { "pkDiscountId", "createdAt", "discountCode", "endDateTime", "startDateTime", "value" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "WELCOME10", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 10.00m });

            migrationBuilder.InsertData(
                table: "Discount",
                columns: new[] { "pkDiscountId", "createdAt", "discountCode", "discountType", "endDateTime", "startDateTime", "value" },
                values: new object[] { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "YEARLY50", "$", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 50.00m });

            migrationBuilder.InsertData(
                table: "PlanFeature",
                columns: new[] { "pkPlanFeatureId", "featureDescription", "featureName", "fkPlanId", "sortOrder" },
                values: new object[,]
                {
                    { 1, "Access to limited resources and tools.", "Basic Access", 1, 1 },
                    { 2, "Access to community forum support.", "Community Support", 1, 2 },
                    { 3, "Includes all Free plan features.", "All Free Features", 2, 1 },
                    { 4, "Get help faster with priority support.", "Priority Support", 2, 2 },
                    { 5, "Access to detailed reports and analytics.", "Advanced Analytics", 2, 3 },
                    { 6, "Includes all Monthly plan features.", "All Monthly Features", 3, 1 },
                    { 7, "Assigned a dedicated account manager for support.", "Dedicated Account Manager", 3, 2 },
                    { 8, "Store unlimited data and files.", "Unlimited Storage", 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "PlanDiscount",
                columns: new[] { "fkDiscountId", "fkPlanId" },
                values: new object[,]
                {
                    { 1, 2 },
                    { 1, 3 },
                    { 2, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlanDiscount",
                keyColumns: new[] { "fkDiscountId", "fkPlanId" },
                keyValues: new object[] { 1, 2 });

            migrationBuilder.DeleteData(
                table: "PlanDiscount",
                keyColumns: new[] { "fkDiscountId", "fkPlanId" },
                keyValues: new object[] { 1, 3 });

            migrationBuilder.DeleteData(
                table: "PlanDiscount",
                keyColumns: new[] { "fkDiscountId", "fkPlanId" },
                keyValues: new object[] { 2, 3 });

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "PlanFeature",
                keyColumn: "pkPlanFeatureId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Discount",
                keyColumn: "pkDiscountId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Discount",
                keyColumn: "pkDiscountId",
                keyValue: 2);
        }
    }
}
