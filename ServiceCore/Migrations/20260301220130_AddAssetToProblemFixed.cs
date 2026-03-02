using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetToProblemFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Problems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Problems_AssetId",
                table: "Problems",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Problems_Assets_AssetId",
                table: "Problems",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Problems_Assets_AssetId",
                table: "Problems");

            migrationBuilder.DropIndex(
                name: "IX_Problems_AssetId",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Problems");
        }
    }
}
