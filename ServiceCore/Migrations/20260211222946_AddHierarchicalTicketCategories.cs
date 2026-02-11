using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceCore.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchicalTicketCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "TicketCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketCategories_ParentId",
                table: "TicketCategories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketCategories_TicketCategories_ParentId",
                table: "TicketCategories",
                column: "ParentId",
                principalTable: "TicketCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketCategories_TicketCategories_ParentId",
                table: "TicketCategories");

            migrationBuilder.DropIndex(
                name: "IX_TicketCategories_ParentId",
                table: "TicketCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "TicketCategories");
        }
    }
}
