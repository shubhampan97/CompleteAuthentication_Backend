using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompleteAuthentication.Migrations
{
    /// <inheritdoc />
    public partial class Add_TfaSecret_UserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TfaSecret",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TfaSecret",
                table: "Users");
        }
    }
}
