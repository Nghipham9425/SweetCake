using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SweetCakeShop.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEmailToBehaviorEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "CustomerBehaviorEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBehaviorEvents_CustomerEmail",
                table: "CustomerBehaviorEvents",
                column: "CustomerEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBehaviorEvents_CustomerEmail",
                table: "CustomerBehaviorEvents");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "CustomerBehaviorEvents");
        }
    }
}
