using Microsoft.EntityFrameworkCore.Migrations;

namespace Geocaching.Migrations
{
    public partial class RemovedSpecificProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Course",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Speed",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Altitude",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Course",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_Speed",
                table: "Geocache");

            migrationBuilder.DropColumn(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Geocache");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Course",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Speed",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Person",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Altitude",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Course",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_HorizontalAccuracy",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_Speed",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GeoCoordinate_VerticalAccuracy",
                table: "Geocache",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
