using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReviewed",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadline",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookingReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Photographer = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingReviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingReviews_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    MaxUsageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentUsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUsagePerUser = table.Column<int>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLoyalties",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    TotalPoints = table.Column<long>(type: "INTEGER", nullable: false),
                    AvailablePoints = table.Column<long>(type: "INTEGER", nullable: false),
                    TotalBookings = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MembershipTier = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastPurchaseAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLoyalties", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_CustomerLoyalties_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BookingId = table.Column<string>(type: "TEXT", nullable: true),
                    ReferralCode = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: true),
                    ActionUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmailSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SmsSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    SmsSentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailSubject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EmailBody = table.Column<string>(type: "TEXT", nullable: false),
                    SmsTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhotographerRatings",
                columns: table => new
                {
                    PhotographerId = table.Column<string>(type: "TEXT", nullable: false),
                    AverageRating = table.Column<double>(type: "REAL", nullable: false),
                    TotalReviews = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotographerRatings", x => x.PhotographerId);
                });

            migrationBuilder.CreateTable(
                name: "ReferralCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    RewardPoints = table.Column<long>(type: "INTEGER", nullable: false),
                    UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxUsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralCodes", x => x.Code);
                    table.ForeignKey(
                        name: "FK_ReferralCodes_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefundPolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PolicyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DaysBeforeBookingForFullRefund = table.Column<int>(type: "INTEGER", nullable: false),
                    RefundPercentageAfterDeadline = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AdminNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RefundMethode = table.Column<string>(type: "TEXT", nullable: true),
                    BankAccount = table.Column<string>(type: "TEXT", nullable: true),
                    BankName = table.Column<string>(type: "TEXT", nullable: true),
                    TransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundRequests_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingCoupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    CouponId = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingCoupons_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingCoupons_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Gói Chụp Ngoài Trời Cơ Bản");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Gói Phóng Sự Cưới Premium");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Gói Chân Dung Studio");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCoupons_BookingId",
                table: "BookingCoupons",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingCoupons_CouponId",
                table: "BookingCoupons",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingReviews_BookingId",
                table: "BookingReviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingReviews_ClientId",
                table: "BookingReviews",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_ClientId",
                table: "LoyaltyTransactions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClientId",
                table: "Notifications",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferralCodes_ClientId",
                table: "ReferralCodes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_BookingId",
                table: "RefundRequests",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefundRequests_ClientId",
                table: "RefundRequests",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingCoupons");

            migrationBuilder.DropTable(
                name: "BookingReviews");

            migrationBuilder.DropTable(
                name: "CustomerLoyalties");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "PhotographerRatings");

            migrationBuilder.DropTable(
                name: "ReferralCodes");

            migrationBuilder.DropTable(
                name: "RefundPolicies");

            migrationBuilder.DropTable(
                name: "RefundRequests");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropColumn(
                name: "IsReviewed",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ReviewDeadline",
                table: "Bookings");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Gói Kỷ yếu Cơ Bản");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Gói Phóng Sự Cưới");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Gói Nàng Thơ");
        }
    }
}
