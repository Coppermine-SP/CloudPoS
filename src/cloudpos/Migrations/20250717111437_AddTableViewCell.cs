using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudInteractive.CloudPos.Migrations
{
    /// <inheritdoc />
    public partial class AddTableViewCell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AuthCode",
                table: "Sessions",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldMaxLength: 4);

            migrationBuilder.CreateTable(
                name: "TableViewCells",
                columns: table => new
                {
                    X = table.Column<int>(type: "int", nullable: false),
                    Y = table.Column<int>(type: "int", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableViewCells", x => new { x.X, x.Y });
                    table.ForeignKey(
                        name: "FK_TableViewCells_Tables_TableId",
                        column: x => x.TableId,
                        principalTable: "Tables",
                        principalColumn: "TableId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TableViewCells_TableId",
                table: "TableViewCells",
                column: "TableId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TableViewCells");

            migrationBuilder.AlterColumn<string>(
                name: "AuthCode",
                table: "Sessions",
                type: "varchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldMaxLength: 4,
                oldNullable: true);
        }
    }
}
