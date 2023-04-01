using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DpsWebManagement.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DpsCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DpsCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DpsEnrollmentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DpsCertificateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DpsEnrollmentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DpsEnrollmentGroups_DpsCertificates_DpsCertificateId",
                        column: x => x.DpsCertificateId,
                        principalTable: "DpsCertificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DpsEnrollmentDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPrivateKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PemPublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DpsEnrollmentGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DpsEnrollmentDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DpsEnrollmentDevices_DpsEnrollmentGroups_DpsEnrollmentGroupId",
                        column: x => x.DpsEnrollmentGroupId,
                        principalTable: "DpsEnrollmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DpsEnrollmentDevices_DpsEnrollmentGroupId",
                table: "DpsEnrollmentDevices",
                column: "DpsEnrollmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DpsEnrollmentGroups_DpsCertificateId",
                table: "DpsEnrollmentGroups",
                column: "DpsCertificateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DpsEnrollmentDevices");

            migrationBuilder.DropTable(
                name: "DpsEnrollmentGroups");

            migrationBuilder.DropTable(
                name: "DpsCertificates");
        }
    }
}
