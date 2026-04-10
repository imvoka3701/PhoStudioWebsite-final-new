using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings",
                columns: new[] { "BookingDate", "StartTime", "PhotographerId" },
                unique: true,
                filter: "[Status] <> 4 AND [PhotographerId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingDate_StartTime_PhotographerId",
                table: "Bookings",
                columns: new[] { "BookingDate", "StartTime", "PhotographerId" },
                unique: true,
                filter: "[Status] <> 3 AND [PhotographerId] IS NOT NULL");
        }
    }
}
