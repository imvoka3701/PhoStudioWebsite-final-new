using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhoStudioMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedBusinessFeaturesFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
