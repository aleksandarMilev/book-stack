using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReservationLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationExpiresOnUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Orders]
                SET [ReservationExpiresOnUtc] = DATEADD(minute, 30, [CreatedOn])
                WHERE [ReservationExpiresOnUtc] IS NULL;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReservationExpiresOnUtc",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservationReleasedOnUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReservationExpiresOnUtc",
                table: "Orders",
                column: "ReservationExpiresOnUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ReservationExpiresOnUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReservationExpiresOnUtc",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReservationReleasedOnUtc",
                table: "Orders");
        }
    }
}
