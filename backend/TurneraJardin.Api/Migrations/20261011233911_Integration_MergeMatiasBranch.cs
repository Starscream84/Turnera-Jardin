using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurneraJardin.Api.Migrations
{
    /// <inheritdoc />
    public partial class Integration_MergeMatiasBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Detalle",
                table: "AuditoriaLogs");

            migrationBuilder.DropColumn(
                name: "Entidad",
                table: "AuditoriaLogs");

            migrationBuilder.DropColumn(
                name: "EntidadId",
                table: "AuditoriaLogs");

            migrationBuilder.DropColumn(
                name: "UsuarioEmail",
                table: "AuditoriaLogs");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "Usuarios",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PasswordTemporalHash",
                table: "Usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmacionSolicitada",
                table: "Turnos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "AuditoriaLogs",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Detalles",
                table: "AuditoriaLogs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaLogs_UsuarioId",
                table: "AuditoriaLogs",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditoriaLogs_Usuarios_UsuarioId",
                table: "AuditoriaLogs",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditoriaLogs_Usuarios_UsuarioId",
                table: "AuditoriaLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditoriaLogs_UsuarioId",
                table: "AuditoriaLogs");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "PasswordTemporalHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ConfirmacionSolicitada",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "Detalles",
                table: "AuditoriaLogs");

            migrationBuilder.AlterColumn<string>(
                name: "Accion",
                table: "AuditoriaLogs",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Detalle",
                table: "AuditoriaLogs",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Entidad",
                table: "AuditoriaLogs",
                type: "varchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EntidadId",
                table: "AuditoriaLogs",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioEmail",
                table: "AuditoriaLogs",
                type: "varchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
