using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControleQualite.Donnees.Migrations
{
    /// <inheritdoc />
    public partial class SchemaInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "References",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_References", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Nominal = table.Column<double>(type: "float", nullable: false),
                    LimiteInferieure = table.Column<double>(type: "float", nullable: false),
                    LimiteSuperieure = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotes", x => x.Id);
                    table.CheckConstraint("CK_Cotes_Limites", "[LimiteInferieure] < [LimiteSuperieure]");
                    table.ForeignKey(
                        name: "FK_Cotes_References_ReferenceId",
                        column: x => x.ReferenceId,
                        principalTable: "References",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdresFabrication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    OuvertLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Clos = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdresFabrication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdresFabrication_References_ReferenceId",
                        column: x => x.ReferenceId,
                        principalTable: "References",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pieces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdreFabricationId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ControleeLe = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Operateur = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Etat = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    MotifQuarantaine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    QuarantaineLe = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pieces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pieces_OrdresFabrication_OrdreFabricationId",
                        column: x => x.OrdreFabricationId,
                        principalTable: "OrdresFabrication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mesures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PieceControleeId = table.Column<int>(type: "int", nullable: false),
                    CoteId = table.Column<int>(type: "int", nullable: false),
                    Valeur = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mesures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mesures_Cotes_CoteId",
                        column: x => x.CoteId,
                        principalTable: "Cotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mesures_Pieces_PieceControleeId",
                        column: x => x.PieceControleeId,
                        principalTable: "Pieces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cotes_ReferenceId_Ordre",
                table: "Cotes",
                columns: new[] { "ReferenceId", "Ordre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mesures_CoteId",
                table: "Mesures",
                column: "CoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Mesures_PieceControleeId",
                table: "Mesures",
                column: "PieceControleeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdresFabrication_Numero",
                table: "OrdresFabrication",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdresFabrication_ReferenceId",
                table: "OrdresFabrication",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_ControleeLe",
                table: "Pieces",
                column: "ControleeLe");

            migrationBuilder.CreateIndex(
                name: "IX_Pieces_OrdreFabricationId_Numero",
                table: "Pieces",
                columns: new[] { "OrdreFabricationId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_References_Code",
                table: "References",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Mesures");

            migrationBuilder.DropTable(
                name: "Cotes");

            migrationBuilder.DropTable(
                name: "Pieces");

            migrationBuilder.DropTable(
                name: "OrdresFabrication");

            migrationBuilder.DropTable(
                name: "References");
        }
    }
}
