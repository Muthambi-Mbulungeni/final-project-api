using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                schema: "public",
                table: "User",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FailedLoginWindowStart",
                schema: "public",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndsAt",
                schema: "public",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                schema: "public",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FailedLoginWindowStart",
                schema: "public",
                table: "User");

            migrationBuilder.DropColumn(
                name: "LockoutEndsAt",
                schema: "public",
                table: "User");
        }
    }
}
