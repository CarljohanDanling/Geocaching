using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class SecondMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Geocache_PersonID",
                table: "Geocache",
                column: "PersonID");

            migrationBuilder.AddForeignKey(
                name: "FK_Geocache_Person_PersonID",
                table: "Geocache",
                column: "PersonID",
                principalTable: "Person",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Geocache_Person_PersonID",
                table: "Geocache");

            migrationBuilder.DropIndex(
                name: "IX_Geocache_PersonID",
                table: "Geocache");
        }
    }
}
