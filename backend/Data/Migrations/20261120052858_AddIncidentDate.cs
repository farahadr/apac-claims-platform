using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimsApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "IncidentDate",
                table: "Claims",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddCheckConstraint(
                name: "CK_Claims_IncidentDate_NotInFuture",
                table: "Claims",
                sql: "\"IncidentDate\" <= CURRENT_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Claims_IncidentDate_NotInFuture",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "IncidentDate",
                table: "Claims");
        }
    }
}
