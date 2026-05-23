using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DRB_HMI_3D.Migrations
{
    /// <inheritdoc />
    public partial class UpdatepressTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "PressTags",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "Value",
                table: "PressTags",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
