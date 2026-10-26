using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadConsentGivenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentGivenAt",
                schema: "public",
                table: "Lead",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentGivenAt",
                schema: "public",
                table: "Lead");
        }
    }
}
