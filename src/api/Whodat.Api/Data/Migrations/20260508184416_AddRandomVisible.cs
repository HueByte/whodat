using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Whodat.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRandomVisible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RandomVisible",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RandomVisible",
                table: "AspNetUsers");
        }
    }
}
