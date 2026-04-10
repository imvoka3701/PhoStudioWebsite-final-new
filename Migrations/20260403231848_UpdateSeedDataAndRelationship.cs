using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedDataAndRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Chá»¥p hÃ¬nh ká»· yáº¿u ngoáº¡i cáº£nh 1 buá»•i", "GÃ³i Chá»¥p NgoÃ i Trá»i CÆ¡ Báº£n" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "PhÃ³ng sá»± cÆ°á»›i cao cáº¥p ná»­a ngÃ y", "GÃ³i PhÃ³ng Sá»± CÆ°á»›i Premium" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Chá»¥p chÃ¢n dung nghá»‡ thuáº­t trong Studio", "GÃ³i ChÃ¢n Dung Studio" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin_seed_1",
                column: "Email",
                value: "admin@phostudio.vn");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CreatedAt", "Email", "FullName", "IsActive", "PasswordHash", "Phone", "Role", "Username" },
                values: new object[,]
                {
                    { "customer_seed_1", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "customer1@phostudio.vn", "Customer Demo", true, "f2d81a021020614f08e82069fa9f7498c0d95b508f7ce19c6736736412e2c560", "0900000003", 2, "customer01" },
                    { "photo_seed_1", null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "photo1@phostudio.vn", "Photographer Demo", true, "8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020ca112706e90", "0900000002", 1, "photographer01" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "customer_seed_1");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "photo_seed_1");

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Chụp hình kỷ yếu ngoại cảnh 1 buổi", "Gói Chụp Ngoài Trời Cơ Bản" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Phóng sự cưới cao cấp nửa ngày", "Gói Phóng Sự Cưới Premium" });

            migrationBuilder.UpdateData(
                table: "ServicePackages",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Chụp chân dung nghệ thuật trong Studio", "Gói Chân Dung Studio" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "admin_seed_1",
                column: "Email",
                value: "admin@phostudio.com");
        }
    }
}
