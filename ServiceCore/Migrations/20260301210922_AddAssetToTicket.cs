using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssetId",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssetId",
                table: "Tickets",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Assets_AssetId",
                table: "Tickets",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Assets_AssetId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssetId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssetId",
                table: "Tickets");
        }
    }
}
