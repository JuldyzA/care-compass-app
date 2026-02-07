using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamYellow.Migrations
{
    /// <inheritdoc />
    public partial class CreateBusinessTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Counsellor",
                columns: table => new
                {
                    pkCounsellorId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    practitionerLicenceId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    displayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fkUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counsellor", x => x.pkCounsellorId);
                    table.ForeignKey(
                        name: "FK_Counsellor_AspNetUsers_fkUserId",
                        column: x => x.fkUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Discount",
                columns: table => new
                {
                    pkDiscountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discountCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    discountType = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "%"),
                    value = table.Column<decimal>(type: "TEXT", nullable: false),
                    startDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    endDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discount", x => x.pkDiscountId);
                });

            migrationBuilder.CreateTable(
                name: "Plan",
                columns: table => new
                {
                    pkPlanId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    planName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    planDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    billingType = table.Column<string>(type: "TEXT", nullable: false),
                    isActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plan", x => x.pkPlanId);
                });

            migrationBuilder.CreateTable(
                name: "UserLog",
                columns: table => new
                {
                    pkLogId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    logInTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    logOutTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    abandoned = table.Column<bool>(type: "INTEGER", nullable: false),
                    fkUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLog", x => x.pkLogId);
                    table.ForeignKey(
                        name: "FK_UserLog_AspNetUsers_fkUserId",
                        column: x => x.fkUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                columns: table => new
                {
                    pkUserProfileId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    firstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    lastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    profilePhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    unitNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    street = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    city = table.Column<string>(type: "TEXT", maxLength: 80, nullable: true),
                    province = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    postalCode = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    fkUserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.pkUserProfileId);
                    table.ForeignKey(
                        name: "FK_UserProfile_AspNetUsers_fkUserId",
                        column: x => x.fkUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    pkClientId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    firstName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    lastName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fkCounsellorId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.pkClientId);
                    table.ForeignKey(
                        name: "FK_Client_Counsellor_fkCounsellorId",
                        column: x => x.fkCounsellorId,
                        principalTable: "Counsellor",
                        principalColumn: "pkCounsellorId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanDiscount",
                columns: table => new
                {
                    fkPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    fkDiscountId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanDiscount", x => new { x.fkPlanId, x.fkDiscountId });
                    table.ForeignKey(
                        name: "FK_PlanDiscount_Discount_fkDiscountId",
                        column: x => x.fkDiscountId,
                        principalTable: "Discount",
                        principalColumn: "pkDiscountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanDiscount_Plan_fkPlanId",
                        column: x => x.fkPlanId,
                        principalTable: "Plan",
                        principalColumn: "pkPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanFeature",
                columns: table => new
                {
                    pkPlanFeatureId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    featureName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    featureDescription = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    sortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    fkPlanId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanFeature", x => x.pkPlanFeatureId);
                    table.ForeignKey(
                        name: "FK_PlanFeature_Plan_fkPlanId",
                        column: x => x.fkPlanId,
                        principalTable: "Plan",
                        principalColumn: "pkPlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    pkPaymentTransactionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    payerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    currency = table.Column<string>(type: "char(3)", maxLength: 3, nullable: false),
                    provider = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    providerOrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    paidAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fkSubscriptionId = table.Column<int>(type: "INTEGER", nullable: false),
                    fkDiscountId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.pkPaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Discount_fkDiscountId",
                        column: x => x.fkDiscountId,
                        principalTable: "Discount",
                        principalColumn: "pkDiscountId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    pkSubscriptionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    cycleStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    cycleEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    fkPlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    fkCounsellorId = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentTransactionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscription", x => x.pkSubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscription_Counsellor_fkCounsellorId",
                        column: x => x.fkCounsellorId,
                        principalTable: "Counsellor",
                        principalColumn: "pkCounsellorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Subscription_PaymentTransaction_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "PaymentTransaction",
                        principalColumn: "pkPaymentTransactionId");
                    table.ForeignKey(
                        name: "FK_Subscription_Plan_fkPlanId",
                        column: x => x.fkPlanId,
                        principalTable: "Plan",
                        principalColumn: "pkPlanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Client_email",
                table: "Client",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Client_fkCounsellorId",
                table: "Client",
                column: "fkCounsellorId");

            migrationBuilder.CreateIndex(
                name: "IX_Counsellor_fkUserId",
                table: "Counsellor",
                column: "fkUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Counsellor_practitionerLicenceId",
                table: "Counsellor",
                column: "practitionerLicenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Discount_discountCode",
                table: "Discount",
                column: "discountCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_fkDiscountId",
                table: "PaymentTransaction",
                column: "fkDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_fkSubscriptionId",
                table: "PaymentTransaction",
                column: "fkSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanDiscount_fkDiscountId",
                table: "PlanDiscount",
                column: "fkDiscountId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanFeature_fkPlanId",
                table: "PlanFeature",
                column: "fkPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_fkCounsellorId",
                table: "Subscription",
                column: "fkCounsellorId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_fkPlanId",
                table: "Subscription",
                column: "fkPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_PaymentTransactionId",
                table: "Subscription",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLog_fkUserId",
                table: "UserLog",
                column: "fkUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_fkUserId",
                table: "UserProfile",
                column: "fkUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransaction_Subscription_fkSubscriptionId",
                table: "PaymentTransaction",
                column: "fkSubscriptionId",
                principalTable: "Subscription",
                principalColumn: "pkSubscriptionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_Counsellor_fkCounsellorId",
                table: "Subscription");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransaction_Discount_fkDiscountId",
                table: "PaymentTransaction");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransaction_Subscription_fkSubscriptionId",
                table: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "PlanDiscount");

            migrationBuilder.DropTable(
                name: "PlanFeature");

            migrationBuilder.DropTable(
                name: "UserLog");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropTable(
                name: "Counsellor");

            migrationBuilder.DropTable(
                name: "Discount");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "Plan");
        }
    }
}
