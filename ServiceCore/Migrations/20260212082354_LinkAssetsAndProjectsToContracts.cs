using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class LinkAssetsAndProjectsToContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ContractId",
                table: "Projects",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ContractId",
                table: "Assets",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Contracts_ContractId",
                table: "Assets",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Contracts_ContractId",
                table: "Projects",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Contracts_ContractId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Contracts_ContractId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ContractId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Assets_ContractId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "Assets");
        }
    }
}
