using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetLearning.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageReplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReplyToMessageId",
                table: "chat_messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ReplyToMessageId",
                table: "chat_messages",
                column: "ReplyToMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_messages_chat_messages_ReplyToMessageId",
                table: "chat_messages",
                column: "ReplyToMessageId",
                principalTable: "chat_messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_messages_chat_messages_ReplyToMessageId",
                table: "chat_messages");

            migrationBuilder.DropIndex(
                name: "IX_chat_messages_ReplyToMessageId",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "chat_messages");
        }
    }
}
