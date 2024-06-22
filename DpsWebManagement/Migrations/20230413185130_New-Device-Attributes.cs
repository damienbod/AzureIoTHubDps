using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DpsWebManagement.Migrations
{
    /// <inheritdoc />
    public partial class NewDeviceAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedHub",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationId",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedHub",
                table: "DpsEnrollmentDevices");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "DpsEnrollmentDevices");

            migrationBuilder.DropColumn(
                name: "RegistrationId",
                table: "DpsEnrollmentDevices");
        }
    }
}
