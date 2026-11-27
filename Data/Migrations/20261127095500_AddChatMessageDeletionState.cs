using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetLearning.Data.Migrations
{
    public partial class AddChatMessageDeletionState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 已清空的訊息回到舊版時仍應維持刪除。
            migrationBuilder.Sql("""DELETE FROM chat_messages WHERE "IsDeleted";""");
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "chat_messages");
        }
    }
}
