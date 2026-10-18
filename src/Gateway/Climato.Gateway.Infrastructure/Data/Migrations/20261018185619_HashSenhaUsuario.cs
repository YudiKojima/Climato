using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Climato.Gateway.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class HashSenhaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"user\";");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "user",
                newName: "passwordhash");

            migrationBuilder.AlterColumn<string>(
                name: "passwordhash",
                table: "user",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "passwordhash",
                table: "user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.RenameColumn(
                name: "passwordhash",
                table: "user",
                newName: "password");
        }
    }
}
