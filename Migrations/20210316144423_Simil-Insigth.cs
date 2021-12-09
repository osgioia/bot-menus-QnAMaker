using Microsoft.EntityFrameworkCore.Migrations;

namespace QnABot.Migrations
{
    public partial class SimilInsigth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Score",
                table: "UserQnAReceived",
                nullable: false,
                defaultValue: 0f);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "UserQnAReceived");
        }
    }
}
