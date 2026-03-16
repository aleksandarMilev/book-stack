using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestOrderPaymentTokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuestPaymentTokenHash",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuestPaymentTokenHash",
                table: "Orders");
        }
    }
}
