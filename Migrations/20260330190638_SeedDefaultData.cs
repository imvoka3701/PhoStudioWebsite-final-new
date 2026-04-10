using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "Description", "ImageUrl", "IsActive", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Chụp hình kỷ yếu ngoại cảnh 1 buổi", null, true, "Gói Kỷ yếu Cơ Bản", 1500000m },
                    { 2, "Phóng sự cưới cao cấp nửa ngày", null, true, "Gói Phóng Sự Cưới", 5000000m },
                    { 3, "Chụp chân dung nghệ thuật trong Studio", null, true, "Gói Nàng Thơ", 3000000m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "PasswordHash", "Phone", "Role", "Username" },
                values: new object[] { "admin_seed_1", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@phostudio.com", "System Administrator", "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9", "0901123456", 0, "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin_seed_1");
        }
    }
}
