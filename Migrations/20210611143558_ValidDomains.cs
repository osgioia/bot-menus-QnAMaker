using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace QnABot.Migrations
{
    public partial class ValidDomains : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValidDomains",
                columns: table => new
                {
                    ID = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Domain = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidDomains", x => x.ID);
                });

            migrationBuilder.InsertData(
                table: "ValidDomains",
                columns: new[] { "ID", "Domain" },
                values: new object[,]
                {
                    {1, "la.logicalis.com" },
                    {2, "ica.com.mx" }
                }
             );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidDomains");
        }
    }
}
