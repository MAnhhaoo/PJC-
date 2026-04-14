using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication2.Migrations
{
    /// <inheritdoc />
    public partial class AddTourPriceAndPaymentTourId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Tours",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TourId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TourId",
                table: "Payments",
                column: "TourId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Tours_TourId",
                table: "Payments",
                column: "TourId",
                principalTable: "Tours",
                principalColumn: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Tours_TourId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TourId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "TourId",
                table: "Payments");
        }
    }
}
