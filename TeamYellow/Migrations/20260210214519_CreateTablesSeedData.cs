using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TeamYellow.Migrations
{
    /// <inheritdoc />
    public partial class CreateTablesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Discount",
                columns: table => new
                {
                    pkDiscountId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discountCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    discountType = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "%"),
                    value = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
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
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
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
                    fkCounsellorId = table.Column<int>(type: "INTEGER", nullable: false)
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
                        name: "FK_Subscription_Plan_fkPlanId",
                        column: x => x.fkPlanId,
                        principalTable: "Plan",
                        principalColumn: "pkPlanId",
                        onDelete: ReferentialAction.Restrict);
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
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    paidAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
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
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Subscription_fkSubscriptionId",
                        column: x => x.fkSubscriptionId,
                        principalTable: "Subscription",
                        principalColumn: "pkSubscriptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Discount",
                columns: new[] { "pkDiscountId", "createdAt", "discountCode", "endDateTime", "startDateTime", "value" },
                values: new object[] { 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "WELCOME10", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 10.00m });

            migrationBuilder.InsertData(
                table: "Discount",
                columns: new[] { "pkDiscountId", "createdAt", "discountCode", "discountType", "endDateTime", "startDateTime", "value" },
                values: new object[] { 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "YEARLY50", "$", new DateTime(9999, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 50.00m });

            migrationBuilder.InsertData(
                table: "Plan",
                columns: new[] { "pkPlanId", "billingType", "createdAt", "isActive", "planDescription", "planName", "price" },
                values: new object[,]
                {
                    { 1, "Free", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Free basic access", "Free", 0.00m },
                    { 2, "Monthly", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Monthly subscription", "Monthly", 49.99m },
                    { 3, "Yearly", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Yearly subscription", "Yearly", 499.99m }
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

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

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
                name: "IX_UserLog_fkUserId",
                table: "UserLog",
                column: "fkUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_fkUserId",
                table: "UserProfile",
                column: "fkUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "PlanDiscount");

            migrationBuilder.DropTable(
                name: "PlanFeature");

            migrationBuilder.DropTable(
                name: "UserLog");

            migrationBuilder.DropTable(
                name: "UserProfile");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "Discount");

            migrationBuilder.DropTable(
                name: "Counsellor");

            migrationBuilder.DropTable(
                name: "Plan");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
