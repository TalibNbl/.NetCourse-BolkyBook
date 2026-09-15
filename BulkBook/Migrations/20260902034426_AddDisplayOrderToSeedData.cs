using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BulkyBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderToSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 1,
                column: "DisplayOrder",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 2,
                column: "DisplayOrder",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 3,
                column: "DisplayOrder",
                value: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 1,
                column: "DisplayOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 2,
                column: "DisplayOrder",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Catogeries",
                keyColumn: "CategoryID",
                keyValue: 3,
                column: "DisplayOrder",
                value: 0);
        }
    }
}
