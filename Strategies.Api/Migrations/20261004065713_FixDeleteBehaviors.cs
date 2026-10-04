using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteBehaviors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedById",
                table: "CaseStageHistory",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case",
                column: "ClientId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case",
                column: "VisaCategoryId",
                principalTable: "VisaCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChangedById",
                table: "CaseStageHistory",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "AuditLog",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case",
                column: "ClientId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case",
                column: "VisaCategoryId",
                principalTable: "VisaCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
