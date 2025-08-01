using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudInteractive.CloudPos.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTableSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaymentCompleted",
                table: "Sessions");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Sessions");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentCompleted",
                table: "Sessions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}
