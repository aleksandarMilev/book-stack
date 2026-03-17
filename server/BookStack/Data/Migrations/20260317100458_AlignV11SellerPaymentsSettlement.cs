using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStack.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignV11SellerPaymentsSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeePercent",
                table: "Orders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SellerNetAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SettlementStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SellerProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SupportsOnlinePayment = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SupportsCashOnDelivery = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_SellerProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentMethod",
                table: "Orders",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SettlementStatus",
                table: "Orders",
                column: "SettlementStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SellerProfiles_CreatedOn",
                table: "SellerProfiles",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_SellerProfiles_IsActive",
                table: "SellerProfiles",
                column: "IsActive");

            migrationBuilder.Sql("""
                UPDATE [Orders]
                SET
                    [PlatformFeePercent] = 10.00,
                    [PlatformFeeAmount] = ROUND([TotalAmount] * 0.10, 2),
                    [SellerNetAmount] = [TotalAmount] - ROUND([TotalAmount] * 0.10, 2)
                WHERE
                    [PlatformFeePercent] = 0
                    AND [PlatformFeeAmount] = 0
                    AND [SellerNetAmount] = 0;
                """);

            migrationBuilder.Sql("""
                INSERT INTO [SellerProfiles] (
                    [UserId],
                    [DisplayName],
                    [PhoneNumber],
                    [SupportsOnlinePayment],
                    [SupportsCashOnDelivery],
                    [IsActive],
                    [IsDeleted],
                    [CreatedOn],
                    [CreatedBy])
                SELECT
                    [u].[Id],
                    COALESCE(NULLIF([u].[UserName], ''), NULLIF([u].[Email], ''), CONCAT('Seller ', [u].[Id])),
                    [u].[PhoneNumber],
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    CAST(0 AS bit),
                    GETUTCDATE(),
                    'migration:AlignV11SellerPaymentsSettlement'
                FROM [AspNetUsers] AS [u]
                WHERE EXISTS (
                    SELECT 1
                    FROM [BookListings] AS [l]
                    WHERE [l].[CreatorId] = [u].[Id])
                    AND NOT EXISTS (
                        SELECT 1
                        FROM [SellerProfiles] AS [sp]
                        WHERE [sp].[UserId] = [u].[Id]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentMethod",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_SettlementStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlatformFeeAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlatformFeePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerNetAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SettlementStatus",
                table: "Orders");
        }
    }
}
