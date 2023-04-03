using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DpsWebManagement.Migrations
{
    /// <inheritdoc />
    public partial class newfields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "DpsEnrollmentDevices",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "DpsEnrollmentDevices");
        }
    }
}
