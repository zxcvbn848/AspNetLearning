using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "todo_items",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "todo_items",
                keyColumn: "Id",
                keyValue: 1,
                column: "UserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "todo_items",
                keyColumn: "Id",
                keyValue: 2,
                column: "UserId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_todo_items_UserId",
                table: "todo_items",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_todo_items_users_UserId",
                table: "todo_items",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_todo_items_users_UserId",
                table: "todo_items");

            migrationBuilder.DropIndex(
                name: "IX_todo_items_UserId",
                table: "todo_items");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "todo_items");
        }
    }
}
