using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiversityPub.Migrations
{
    /// <inheritdoc />
    public partial class AddSuperAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "MotDePasse",
                value: "$2a$11$aJeLp7uoJndd7QCLHQu9kuUCXSHvtW95AiQs6a2u2GIhFWkDhxRwW");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Utilisateurs",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "MotDePasse",
                value: "$2a$11$nD6sxppEQflyGER7INZXYu2JezqZBEUcx797gTF2yTutFPP41kofu");
        }
    }
}
