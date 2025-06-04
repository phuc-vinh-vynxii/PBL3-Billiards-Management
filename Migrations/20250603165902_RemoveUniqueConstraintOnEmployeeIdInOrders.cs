using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BilliardsManagement.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueConstraintOnEmployeeIdInOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa unique index trên EmployeeId trong bảng Orders
            migrationBuilder.DropIndex(
                name: "IX_Orders_EmployeeId",
                table: "Orders");

            // Tạo lại index thông thường (non-unique) trên EmployeeId
            migrationBuilder.CreateIndex(
                name: "IX_Orders_EmployeeId",
                table: "Orders",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa index thông thường
            migrationBuilder.DropIndex(
                name: "IX_Orders_EmployeeId",
                table: "Orders");

            // Tạo lại unique index
            migrationBuilder.CreateIndex(
                name: "IX_Orders_EmployeeId",
                table: "Orders",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");
        }
    }
}
