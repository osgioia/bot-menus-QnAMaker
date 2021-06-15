using Microsoft.EntityFrameworkCore.Migrations;

namespace QnABot.Migrations
{
    public partial class SavingAnswerSource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "UserQnAReceived",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "UserQnAReceived");
        }
    }
}
