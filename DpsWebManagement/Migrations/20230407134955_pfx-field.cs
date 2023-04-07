using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DpsWebManagement.Migrations
{
    /// <inheritdoc />
    public partial class pfxfield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PemPrivateKey",
                table: "DpsEnrollmentDevices");

            migrationBuilder.RenameColumn(
                name: "PemPublicKey",
                table: "DpsEnrollmentDevices",
                newName: "PathToPfx");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PathToPfx",
                table: "DpsEnrollmentDevices",
                newName: "PemPublicKey");

            migrationBuilder.AddColumn<string>(
                name: "PemPrivateKey",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
