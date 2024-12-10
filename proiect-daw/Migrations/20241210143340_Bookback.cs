using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proiectdaw.Migrations
{
    /// <inheritdoc />
    public partial class Bookback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrivateProfile",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivateProfile",
                table: "AspNetUsers");
        }
    }
}
