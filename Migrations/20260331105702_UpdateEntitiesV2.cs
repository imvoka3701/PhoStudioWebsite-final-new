using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntitiesV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_PhotographerId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_CloudAlbums_BookingId",
                table: "CloudAlbums");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ServicePackages",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ServicePackages",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "ServicePackages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "ServicePackages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstVerifiedAt",
                table: "CloudAlbums",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedByClient",
                table: "CloudAlbums",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Bookings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "Bookings",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConceptNote",
                table: "Bookings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RetouchRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    PhotoReference = table.Column<string>(type: "TEXT", nullable: true),
                    RequestNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetouchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetouchRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookingId = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    GatewayTransactionId = table.Column<string>(type: "TEXT", nullable: true),
                    GatewayResponse = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DurationMinutes", "ThumbnailUrl" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 120, null });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DurationMinutes", "ThumbnailUrl" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 240, null });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DurationMinutes", "ThumbnailUrl" },
                values: new object[] { new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 120, null });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin_seed_1",
                columns: new[] { "AvatarUrl", "IsActive" },
                values: new object[] { null, true });

            migrationBuilder.CreateIndex(
                name: "IX_CloudAlbums_BookingId",
                table: "CloudAlbums",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings",
                columns: new[] { "BookingDate", "StartTime", "PhotographerId" },
                unique: true,
                filter: "[Status] <> 3 AND [PhotographerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RetouchRequests_BookingId",
                table: "RetouchRequests",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_BookingId",
                table: "Transactions",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_PhotographerId",
                table: "Bookings",
                column: "PhotographerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_PhotographerId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "RetouchRequests");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_CloudAlbums_BookingId",
                table: "CloudAlbums");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "FirstVerifiedAt",
                table: "CloudAlbums");

            migrationBuilder.DropColumn(
                name: "IsVerifiedByClient",
                table: "CloudAlbums");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ConceptNote",
                table: "Bookings");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "ServicePackages",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_CloudAlbums_BookingId",
                table: "CloudAlbums",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_PhotographerId",
                table: "Bookings",
                column: "PhotographerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
