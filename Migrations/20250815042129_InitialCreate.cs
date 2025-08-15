using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiversityPub.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Lieux",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lieux", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Prenom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MotDePasse = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Supprimer = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AgentsTerrain",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UtilisateurId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Telephone = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstConnecte = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DerniereDeconnexion = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentsTerrain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentsTerrain_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UtilisateurId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RaisonSociale = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Adresse = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RegistreCommerce = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomDirigeant = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomContactPrincipal = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TelephoneContactPrincipal = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailContactPrincipal = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateUpload = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AgentTerrainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AgentsTerrain_AgentTerrainId",
                        column: x => x.AgentTerrainId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PositionsGPS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    Horodatage = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Precision = table.Column<double>(type: "double", nullable: false),
                    AgentTerrainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionsGPS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionsGPS_AgentsTerrain_AgentTerrainId",
                        column: x => x.AgentTerrainId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Campagnes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateDebut = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Objectifs = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campagnes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campagnes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Activations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Instructions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateActivation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HeureDebut = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    HeureFin = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MotifSuspension = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateSuspension = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PreuvesValidees = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateValidationPreuves = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValideParId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CampagneId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LieuId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResponsableId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activations_AgentsTerrain_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Activations_Campagnes_CampagneId",
                        column: x => x.CampagneId,
                        principalTable: "Campagnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activations_Lieux_LieuId",
                        column: x => x.LieuId,
                        principalTable: "Lieux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Activations_Utilisateurs_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DemandesActivation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Nom = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateActivation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HeureDebut = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    HeureFin = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    Instructions = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LieuId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CampagneId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DateDemande = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    MotifRefus = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateReponse = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReponduParId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesActivation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesActivation_Campagnes_CampagneId",
                        column: x => x.CampagneId,
                        principalTable: "Campagnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesActivation_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesActivation_Lieux_LieuId",
                        column: x => x.LieuId,
                        principalTable: "Lieux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandesActivation_Utilisateurs_ReponduParId",
                        column: x => x.ReponduParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ActivationAgentTerrain",
                columns: table => new
                {
                    ActivationsId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AgentsTerrainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationAgentTerrain", x => new { x.ActivationsId, x.AgentsTerrainId });
                    table.ForeignKey(
                        name: "FK_ActivationAgentTerrain_Activations_ActivationsId",
                        column: x => x.ActivationsId,
                        principalTable: "Activations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivationAgentTerrain_AgentsTerrain_AgentsTerrainId",
                        column: x => x.AgentsTerrainId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Note = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateFeedback = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CampagneId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ActivationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ReponseAdmin = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateReponseAdmin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdminRepondant = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstMasque = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateMasquage = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    AdminMasquant = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Activations_ActivationId",
                        column: x => x.ActivationId,
                        principalTable: "Activations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Campagnes_CampagneId",
                        column: x => x.CampagneId,
                        principalTable: "Campagnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Titre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priorite = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateResolution = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CommentaireResolution = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentTerrainId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ActivationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Activations_ActivationId",
                        column: x => x.ActivationId,
                        principalTable: "Activations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Incidents_AgentsTerrain_AgentTerrainId",
                        column: x => x.AgentTerrainId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Medias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateUpload = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Valide = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ValideParId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CommentaireValidation = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgentTerrainId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ActivationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medias_Activations_ActivationId",
                        column: x => x.ActivationId,
                        principalTable: "Activations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Medias_AgentsTerrain_AgentTerrainId",
                        column: x => x.AgentTerrainId,
                        principalTable: "AgentsTerrain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Medias_Utilisateurs_ValideParId",
                        column: x => x.ValideParId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Utilisateurs",
                columns: new[] { "Id", "Email", "MotDePasse", "Nom", "Prenom", "Role", "Supprimer" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "admin@diversitypub.ci", "$2a$11$nD6sxppEQflyGER7INZXYu2JezqZBEUcx797gTF2yTutFPP41kofu", "Super", "Admin", 1, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_ActivationAgentTerrain_AgentsTerrainId",
                table: "ActivationAgentTerrain",
                column: "AgentsTerrainId");

            migrationBuilder.CreateIndex(
                name: "IX_Activations_CampagneId",
                table: "Activations",
                column: "CampagneId");

            migrationBuilder.CreateIndex(
                name: "IX_Activations_LieuId",
                table: "Activations",
                column: "LieuId");

            migrationBuilder.CreateIndex(
                name: "IX_Activations_ResponsableId",
                table: "Activations",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Activations_ValideParId",
                table: "Activations",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentsTerrain_UtilisateurId",
                table: "AgentsTerrain",
                column: "UtilisateurId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campagnes_ClientId",
                table: "Campagnes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_UtilisateurId",
                table: "Clients",
                column: "UtilisateurId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandesActivation_CampagneId",
                table: "DemandesActivation",
                column: "CampagneId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesActivation_ClientId",
                table: "DemandesActivation",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesActivation_LieuId",
                table: "DemandesActivation",
                column: "LieuId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesActivation_ReponduParId",
                table: "DemandesActivation",
                column: "ReponduParId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AgentTerrainId",
                table: "Documents",
                column: "AgentTerrainId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ActivationId",
                table: "Feedbacks",
                column: "ActivationId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CampagneId",
                table: "Feedbacks",
                column: "CampagneId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ActivationId",
                table: "Incidents",
                column: "ActivationId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_AgentTerrainId",
                table: "Incidents",
                column: "AgentTerrainId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_ActivationId",
                table: "Medias",
                column: "ActivationId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_AgentTerrainId",
                table: "Medias",
                column: "AgentTerrainId");

            migrationBuilder.CreateIndex(
                name: "IX_Medias_ValideParId",
                table: "Medias",
                column: "ValideParId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionsGPS_AgentTerrainId",
                table: "PositionsGPS",
                column: "AgentTerrainId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationAgentTerrain");

            migrationBuilder.DropTable(
                name: "DemandesActivation");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "Medias");

            migrationBuilder.DropTable(
                name: "PositionsGPS");

            migrationBuilder.DropTable(
                name: "Activations");

            migrationBuilder.DropTable(
                name: "AgentsTerrain");

            migrationBuilder.DropTable(
                name: "Campagnes");

            migrationBuilder.DropTable(
                name: "Lieux");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
