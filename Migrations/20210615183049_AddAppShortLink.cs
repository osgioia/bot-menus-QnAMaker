using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace QnABot.Migrations
{
    public partial class AddAppShortLink : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppShortLink",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<string>(nullable: false),
                    ShortLinkMsTeams = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppShortLink", x => x.ID);
                });

            migrationBuilder.InsertData(
                table: "AppShortLink",
                columns: new[] { "ID", "AppId", "ShortLinkMsTeams" },
                values: new object[,]
                {
                    //bot-adopcion-msteams-ica-prod
                    {1, "925891fb-3ec8-442c-ad4b-55b2847257a5", "https://bit.ly/35evoTo" },
                    //bot-adopcion-msteams-prod
                    {2, "023825ee-9fc2-413c-904b-4ad3a7a37877", "https://bit.ly/2TkuORB" },
                    //harrymsteamsdemo
                    {3, "c026e097-643f-4b95-a71e-887b339553f0", "https://bit.ly/35kNGma" }
                }
             );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppShortLink");
        }
    }
}
