using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Vafadar.Dastan.People.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "personal");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "VisitorAccounts",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notices = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Stamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedByOperator = table.Column<bool>(type: "boolean", nullable: false),
                    TermsVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitorClaims",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorClaims_VisitorAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "VisitorAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorLogins",
                schema: "identity",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_VisitorLogins_VisitorAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "VisitorAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorTokens",
                schema: "identity",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_VisitorTokens_VisitorAccounts_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "VisitorAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Albums",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Owner = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Note = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Albums_Visitors_Owner",
                        column: x => x.Owner,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Circles",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Owner = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Circles_Visitors_Owner",
                        column: x => x.Owner,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    By = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastDay = table.Column<DateOnly>(type: "date", nullable: false),
                    UsedBy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UsedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitations_Visitors_By",
                        column: x => x.By,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Invitations_Visitors_UsedBy",
                        column: x => x.UsedBy,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Owner = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Caption = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Taken = table.Column<DateOnly>(type: "date", nullable: true),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Circle = table.Column<long>(type: "bigint", nullable: true),
                    File = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Visitors_Owner",
                        column: x => x.Owner,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingPositions",
                schema: "personal",
                columns: table => new
                {
                    VisitorId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Collection = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingPositions", x => new { x.VisitorId, x.Collection });
                    table.ForeignKey(
                        name: "FK_ReadingPositions_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryCodes",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitorId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryCodes", x => x.Number);
                    table.ForeignKey(
                        name: "FK_RecoveryCodes_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    From = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    To = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    FromScope = table.Column<int>(type: "integer", nullable: false),
                    FromCircle = table.Column<long>(type: "bigint", nullable: true),
                    ToScope = table.Column<int>(type: "integer", nullable: false),
                    ToCircle = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_Visitors_From",
                        column: x => x.From,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Relationships_Visitors_To",
                        column: x => x.To,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Restrictions",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Ground = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Noticed = table.Column<bool>(type: "boolean", nullable: false),
                    ImposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LiftedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restrictions", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Restrictions_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedPages",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitorId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Collection = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PageId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedPages", x => x.Number);
                    table.ForeignKey(
                        name: "FK_SavedPages_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trees",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Owner = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PublicationAskedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trees_Visitors_Owner",
                        column: x => x.Owner,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CircleMembers",
                schema: "personal",
                columns: table => new
                {
                    Circle = table.Column<long>(type: "bigint", nullable: false),
                    Member = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMembers", x => new { x.Circle, x.Member });
                    table.ForeignKey(
                        name: "FK_CircleMembers_Circles_Circle",
                        column: x => x.Circle,
                        principalSchema: "personal",
                        principalTable: "Circles",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CircleMembers_Visitors_Member",
                        column: x => x.Member,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumPhotos",
                schema: "personal",
                columns: table => new
                {
                    Album = table.Column<long>(type: "bigint", nullable: false),
                    Photo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumPhotos", x => new { x.Album, x.Photo });
                    table.ForeignKey(
                        name: "FK_AlbumPhotos_Albums_Album",
                        column: x => x.Album,
                        principalSchema: "personal",
                        principalTable: "Albums",
                        principalColumn: "Number",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumPhotos_Photos_Photo",
                        column: x => x.Photo,
                        principalSchema: "personal",
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTags",
                schema: "personal",
                columns: table => new
                {
                    Photo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Everybody = table.Column<bool>(type: "boolean", nullable: false),
                    Objection = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTags", x => new { x.Photo, x.Account });
                    table.ForeignKey(
                        name: "FK_PhotoTags_Photos_Photo",
                        column: x => x.Photo,
                        principalSchema: "personal",
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Persons",
                schema: "personal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Born = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    Died = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    BirthPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeathPlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    About = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Picture = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Deceased = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Persons_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Persons_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Persons_Visitors_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PublicationConsents",
                schema: "personal",
                columns: table => new
                {
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationConsents", x => new { x.Tree, x.Account });
                    table.ForeignKey(
                        name: "FK_PublicationConsents_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublicationConsents_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreeChanges",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    By = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    What = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Subject = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeChanges", x => x.Number);
                    table.ForeignKey(
                        name: "FK_TreeChanges_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreeChanges_Visitors_By",
                        column: x => x.By,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TreeMembers",
                schema: "personal",
                columns: table => new
                {
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeMembers", x => new { x.Tree, x.Account });
                    table.ForeignKey(
                        name: "FK_TreeMembers_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreeMembers_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Person = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Account = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Claims_Persons_Person",
                        column: x => x.Person,
                        principalSchema: "personal",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Claims_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Claims_Visitors_Account",
                        column: x => x.Account,
                        principalSchema: "personal",
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kinships",
                schema: "personal",
                columns: table => new
                {
                    Number = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tree = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    From = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    To = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kinships", x => x.Number);
                    table.ForeignKey(
                        name: "FK_Kinships_Persons_From",
                        column: x => x.From,
                        principalSchema: "personal",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kinships_Persons_To",
                        column: x => x.To,
                        principalSchema: "personal",
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kinships_Trees_Tree",
                        column: x => x.Tree,
                        principalSchema: "personal",
                        principalTable: "Trees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumPhotos_Photo",
                schema: "personal",
                table: "AlbumPhotos",
                column: "Photo");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_Owner",
                schema: "personal",
                table: "Albums",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_CircleMembers_Member",
                schema: "personal",
                table: "CircleMembers",
                column: "Member");

            migrationBuilder.CreateIndex(
                name: "IX_Circles_Owner",
                schema: "personal",
                table: "Circles",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Account",
                schema: "personal",
                table: "Claims",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Person",
                schema: "personal",
                table: "Claims",
                column: "Person");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Tree",
                schema: "personal",
                table: "Claims",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_By",
                schema: "personal",
                table: "Invitations",
                column: "By");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UsedBy",
                schema: "personal",
                table: "Invitations",
                column: "UsedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_From",
                schema: "personal",
                table: "Kinships",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_To",
                schema: "personal",
                table: "Kinships",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_Kinships_Tree",
                schema: "personal",
                table: "Kinships",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Account",
                schema: "personal",
                table: "Persons",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_CreatedBy",
                schema: "personal",
                table: "Persons",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Persons_Tree",
                schema: "personal",
                table: "Persons",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Owner",
                schema: "personal",
                table: "Photos",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_Account",
                schema: "personal",
                table: "PhotoTags",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationConsents_Account",
                schema: "personal",
                table: "PublicationConsents",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryCodes_VisitorId",
                schema: "personal",
                table: "RecoveryCodes",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_From",
                schema: "personal",
                table: "Relationships",
                column: "From");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_To",
                schema: "personal",
                table: "Relationships",
                column: "To");

            migrationBuilder.CreateIndex(
                name: "IX_Restrictions_Account",
                schema: "personal",
                table: "Restrictions",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPages_VisitorId_Collection_Kind_PageId",
                schema: "personal",
                table: "SavedPages",
                columns: new[] { "VisitorId", "Collection", "Kind", "PageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreeChanges_By",
                schema: "personal",
                table: "TreeChanges",
                column: "By");

            migrationBuilder.CreateIndex(
                name: "IX_TreeChanges_Tree",
                schema: "personal",
                table: "TreeChanges",
                column: "Tree");

            migrationBuilder.CreateIndex(
                name: "IX_TreeMembers_Account",
                schema: "personal",
                table: "TreeMembers",
                column: "Account");

            migrationBuilder.CreateIndex(
                name: "IX_Trees_Owner",
                schema: "personal",
                table: "Trees",
                column: "Owner");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccounts_NormalizedEmail",
                schema: "identity",
                table: "VisitorAccounts",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccounts_NormalizedUserName",
                schema: "identity",
                table: "VisitorAccounts",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorClaims_UserId",
                schema: "identity",
                table: "VisitorClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorLogins_UserId_LoginProvider",
                schema: "identity",
                table: "VisitorLogins",
                columns: new[] { "UserId", "LoginProvider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_NameKey",
                schema: "personal",
                table: "Visitors",
                column: "NameKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumPhotos",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "CircleMembers",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Claims",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Invitations",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Kinships",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "PhotoTags",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "PublicationConsents",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "ReadingPositions",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "RecoveryCodes",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Relationships",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Restrictions",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "SavedPages",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "TreeChanges",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "TreeMembers",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "VisitorClaims",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "VisitorLogins",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "VisitorTokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Albums",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Circles",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Persons",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Photos",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "VisitorAccounts",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Trees",
                schema: "personal");

            migrationBuilder.DropTable(
                name: "Visitors",
                schema: "personal");
        }
    }
}
