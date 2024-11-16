using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace proiect_daw.Migrations
{
    /// <inheritdoc />
    public partial class Account : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountPassword",
                table: "Accounts",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "AccountEmail",
                table: "Accounts",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Accounts",
                newName: "Id");

            migrationBuilder.AddColumn<bool>(
                name: "Admin",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Admin",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Accounts",
                newName: "AccountPassword");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Accounts",
                newName: "AccountEmail");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Accounts",
                newName: "AccountId");
        }
    }
}
