using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBehaviorEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerBehaviorEvents",
                columns: table => new
                {
                    CustomerBehaviorEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChatToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerBehaviorEvents", x => x.CustomerBehaviorEventId);
                    table.ForeignKey(
                        name: "FK_CustomerBehaviorEvents_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_ChatToken",
                table: "CustomerBehaviorEvents",
                column: "ChatToken");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_CreatedAt",
                table: "CustomerBehaviorEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_EventType",
                table: "CustomerBehaviorEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_ProductId",
                table: "CustomerBehaviorEvents",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_UserId",
                table: "CustomerBehaviorEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerBehaviorEvents");
        }
    }
}
