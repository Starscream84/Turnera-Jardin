using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurneraJardin.Api.Migrations
{
    /// <summary>
    /// The booking form now asks for the child's and the guardian's full name in a single field.
    /// Joins the existing first + last name columns into ChildName / GuardianName (keeping the data)
    /// and then drops the old columns.
    /// </summary>
    public partial class SingleNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChildName",
                table: "Appointments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GuardianName",
                table: "Appointments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // CONCAT_WS skips NULLs; NULLIF leaves free slots (no names) as NULL instead of "".
            migrationBuilder.Sql(
                "UPDATE `Appointments` SET " +
                "`ChildName` = NULLIF(LEFT(TRIM(CONCAT_WS(' ', TRIM(`ChildFirstName`), TRIM(`ChildLastName`))), 200), ''), " +
                "`GuardianName` = NULLIF(LEFT(TRIM(CONCAT_WS(' ', TRIM(`GuardianFirstName`), TRIM(`GuardianLastName`))), 200), '');");

            migrationBuilder.DropColumn(name: "ChildFirstName", table: "Appointments");
            migrationBuilder.DropColumn(name: "ChildLastName", table: "Appointments");
            migrationBuilder.DropColumn(name: "GuardianFirstName", table: "Appointments");
            migrationBuilder.DropColumn(name: "GuardianLastName", table: "Appointments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChildFirstName",
                table: "Appointments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ChildLastName",
                table: "Appointments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GuardianFirstName",
                table: "Appointments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GuardianLastName",
                table: "Appointments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Best effort: the first word goes to the first name and the rest to the last name.
            migrationBuilder.Sql(
                "UPDATE `Appointments` SET " +
                "`ChildFirstName` = SUBSTRING_INDEX(`ChildName`, ' ', 1), " +
                "`ChildLastName` = NULLIF(TRIM(SUBSTRING(`ChildName`, CHAR_LENGTH(SUBSTRING_INDEX(`ChildName`, ' ', 1)) + 1)), ''), " +
                "`GuardianFirstName` = SUBSTRING_INDEX(`GuardianName`, ' ', 1), " +
                "`GuardianLastName` = NULLIF(TRIM(SUBSTRING(`GuardianName`, CHAR_LENGTH(SUBSTRING_INDEX(`GuardianName`, ' ', 1)) + 1)), '');");

            migrationBuilder.DropColumn(name: "ChildName", table: "Appointments");
            migrationBuilder.DropColumn(name: "GuardianName", table: "Appointments");
        }
    }
}
