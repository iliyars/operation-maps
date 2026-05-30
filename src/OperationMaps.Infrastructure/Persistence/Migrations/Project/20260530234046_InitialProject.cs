using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OperationMaps.Infrastructure.src.OperationMaps.Infrastructure.Persistence.Migrations.Project
{
    /// <inheritdoc />
    public partial class InitialProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Login = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Note",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceFileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Project_AppUser_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Family",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Family", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Family_ComponentType_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyParsingRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Pattern = table.Column<string>(type: "TEXT", nullable: false),
                    Example = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyParsingRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyParsingRule_ComponentType_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Component",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: true),
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Component", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Component_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designation = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RawName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetectedCategory = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectComponents_Component_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Component",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectComponents_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentNote",
                columns: table => new
                {
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentNote", x => new { x.ComponentId, x.FormParameterId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_ComponentNote_Component_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComponentNote_Note_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Note",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentNtdValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentNtdValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentNtdValue_Component_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComponentPinValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComponentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Pins = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentPinValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComponentPinValue_Component_ComponentId",
                        column: x => x.ComponentId,
                        principalTable: "Component",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyForm",
                columns: table => new
                {
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyForm", x => new { x.FamilyId, x.FormId });
                    table.ForeignKey(
                        name: "FK_FamilyForm_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyNote",
                columns: table => new
                {
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyNote", x => new { x.FamilyId, x.FormParameterId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_FamilyNote_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyNote_Note_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Note",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyNtdValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FamilyId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyNtdValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyNtdValue_Family_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Family",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Form",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsUniversal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ColumnGrouping = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultLoadFactorParameterId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Form", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSection",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormSection_Form_FormId",
                        column: x => x.FormId,
                        principalTable: "Form",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormValueColumn",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormValueColumn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormValueColumn_Form_FormId",
                        column: x => x.FormId,
                        principalTable: "Form",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormParameter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    SectionId = table.Column<int>(type: "INTEGER", nullable: true),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CanBeLoadFactorBase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormParameter_FormSection_SectionId",
                        column: x => x.SectionId,
                        principalTable: "FormSection",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FormParameter_Form_FormId",
                        column: x => x.FormId,
                        principalTable: "Form",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegimeGroup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormId = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    LoadFactorParameterId = table.Column<int>(type: "INTEGER", nullable: true),
                    LoadFactorMin = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimeGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegimeGroup_FormParameter_LoadFactorParameterId",
                        column: x => x.LoadFactorParameterId,
                        principalTable: "FormParameter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegimeGroup_Form_FormId",
                        column: x => x.FormId,
                        principalTable: "Form",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegimeGroup_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterCellValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegimeGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormParameterId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormValueColumnId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterCellValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterCellValues_FormParameter_FormParameterId",
                        column: x => x.FormParameterId,
                        principalTable: "FormParameter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParameterCellValues_FormValueColumn_FormValueColumnId",
                        column: x => x.FormValueColumnId,
                        principalTable: "FormValueColumn",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ParameterCellValues_RegimeGroup_RegimeGroupId",
                        column: x => x.RegimeGroupId,
                        principalTable: "RegimeGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegimeGroupMember",
                columns: table => new
                {
                    RegimeGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectComponentId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimeGroupMember", x => new { x.RegimeGroupId, x.ProjectComponentId });
                    table.ForeignKey(
                        name: "FK_RegimeGroupMember_ProjectComponents_ProjectComponentId",
                        column: x => x.ProjectComponentId,
                        principalTable: "ProjectComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegimeGroupMember_RegimeGroup_RegimeGroupId",
                        column: x => x.RegimeGroupId,
                        principalTable: "RegimeGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterCellNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParameterCellValueId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterCellNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterCellNotes_Note_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Note",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParameterCellNotes_ParameterCellValues_ParameterCellValueId",
                        column: x => x.ParameterCellValueId,
                        principalTable: "ParameterCellValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_Login",
                table: "AppUser",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Component_FamilyId",
                table: "Component",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Component_FullName",
                table: "Component",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNote_FormParameterId",
                table: "ComponentNote",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNote_NoteId",
                table: "ComponentNote",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValue_ComponentId",
                table: "ComponentNtdValue",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentNtdValue_FormParameterId",
                table: "ComponentNtdValue",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValue_ComponentId",
                table: "ComponentPinValue",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentPinValue_FormParameterId",
                table: "ComponentPinValue",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentType_Name",
                table: "ComponentType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Family_ComponentTypeId_Name",
                table: "Family",
                columns: new[] { "ComponentTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyForm_FormId",
                table: "FamilyForm",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNote_FormParameterId",
                table: "FamilyNote",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNote_NoteId",
                table: "FamilyNote",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValue_FamilyId",
                table: "FamilyNtdValue",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyNtdValue_FormParameterId",
                table: "FamilyNtdValue",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyParsingRule_ComponentTypeId",
                table: "FamilyParsingRule",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Form_DefaultLoadFactorParameterId",
                table: "Form",
                column: "DefaultLoadFactorParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_Form_Number",
                table: "Form",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormParameter_FormId_RowNumber",
                table: "FormParameter",
                columns: new[] { "FormId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormParameter_SectionId",
                table: "FormParameter",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSection_FormId",
                table: "FormSection",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormValueColumn_FormId_Key",
                table: "FormValueColumn",
                columns: new[] { "FormId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNotes_NoteId",
                table: "ParameterCellNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellNotes_ParameterCellValueId_Order",
                table: "ParameterCellNotes",
                columns: new[] { "ParameterCellValueId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValues_FormParameterId",
                table: "ParameterCellValues",
                column: "FormParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValues_FormValueColumnId",
                table: "ParameterCellValues",
                column: "FormValueColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterCellValues_RegimeGroupId_FormParameterId_FormValueColumnId",
                table: "ParameterCellValues",
                columns: new[] { "RegimeGroupId", "FormParameterId", "FormValueColumnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Project_CreatedById",
                table: "Project",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectComponents_ComponentId",
                table: "ProjectComponents",
                column: "ComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectComponents_ProjectId",
                table: "ProjectComponents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_FormId",
                table: "RegimeGroup",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_LoadFactorParameterId",
                table: "RegimeGroup",
                column: "LoadFactorParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroup_ProjectId",
                table: "RegimeGroup",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RegimeGroupMember_ProjectComponentId",
                table: "RegimeGroupMember",
                column: "ProjectComponentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNote_FormParameter_FormParameterId",
                table: "ComponentNote",
                column: "FormParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentNtdValue_FormParameter_FormParameterId",
                table: "ComponentNtdValue",
                column: "FormParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComponentPinValue_FormParameter_FormParameterId",
                table: "ComponentPinValue",
                column: "FormParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyForm_Form_FormId",
                table: "FamilyForm",
                column: "FormId",
                principalTable: "Form",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyNote_FormParameter_FormParameterId",
                table: "FamilyNote",
                column: "FormParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FamilyNtdValue_FormParameter_FormParameterId",
                table: "FamilyNtdValue",
                column: "FormParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Form_FormParameter_DefaultLoadFactorParameterId",
                table: "Form",
                column: "DefaultLoadFactorParameterId",
                principalTable: "FormParameter",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Form_FormParameter_DefaultLoadFactorParameterId",
                table: "Form");

            migrationBuilder.DropTable(
                name: "ComponentNote");

            migrationBuilder.DropTable(
                name: "ComponentNtdValue");

            migrationBuilder.DropTable(
                name: "ComponentPinValue");

            migrationBuilder.DropTable(
                name: "FamilyForm");

            migrationBuilder.DropTable(
                name: "FamilyNote");

            migrationBuilder.DropTable(
                name: "FamilyNtdValue");

            migrationBuilder.DropTable(
                name: "FamilyParsingRule");

            migrationBuilder.DropTable(
                name: "ParameterCellNotes");

            migrationBuilder.DropTable(
                name: "RegimeGroupMember");

            migrationBuilder.DropTable(
                name: "Note");

            migrationBuilder.DropTable(
                name: "ParameterCellValues");

            migrationBuilder.DropTable(
                name: "ProjectComponents");

            migrationBuilder.DropTable(
                name: "FormValueColumn");

            migrationBuilder.DropTable(
                name: "RegimeGroup");

            migrationBuilder.DropTable(
                name: "Component");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Family");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "ComponentType");

            migrationBuilder.DropTable(
                name: "FormParameter");

            migrationBuilder.DropTable(
                name: "FormSection");

            migrationBuilder.DropTable(
                name: "Form");
        }
    }
}
