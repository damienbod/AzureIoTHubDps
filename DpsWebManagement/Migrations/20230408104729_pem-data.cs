using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DpsWebManagement.Migrations
{
    /// <inheritdoc />
    public partial class pemdata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PemPrivateKey",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PemPublicKey",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PemPrivateKey",
                table: "DpsEnrollmentDevices");

            migrationBuilder.DropColumn(
                name: "PemPublicKey",
                table: "DpsEnrollmentDevices");
        }
    }
}
