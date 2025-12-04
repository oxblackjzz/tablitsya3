using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tablitsya3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
   migrationBuilder.CreateTable(
     name: "workshop_data",
 columns: table => new
         {
      id = table.Column<int>(type: "integer", nullable: false)
    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
      last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
        start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
  production_lead_time = table.Column<int>(type: "integer", nullable: false),
           days_before_production = table.Column<int>(type: "integer", nullable: false)
          },
        constraints: table =>
       {
                  table.PrimaryKey("PK_workshop_data", x => x.id);
    });

      migrationBuilder.CreateTable(
             name: "custom_completion_dates",
       columns: table => new
     {
      id = table.Column<int>(type: "integer", nullable: false)
  .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
       order_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
          completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
         WorkshopDataEntityId = table.Column<int>(type: "integer", nullable: true)
    },
            constraints: table =>
        {
              table.PrimaryKey("PK_custom_completion_dates", x => x.id);
     table.ForeignKey(
     name: "FK_custom_completion_dates_workshop_data_WorkshopDataEntityId",
    column: x => x.WorkshopDataEntityId,
            principalTable: "workshop_data",
      principalColumn: "id",
  onDelete: ReferentialAction.Cascade);
             });

  migrationBuilder.CreateTable(
     name: "orders",
        columns: table => new
             {
   id = table.Column<int>(type: "integer", nullable: false)
       .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
    workshop_number = table.Column<int>(type: "integer", nullable: false),
                order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
         square_meters = table.Column<double>(type: "double precision", nullable: false),
        order_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
         WorkshopDataEntityId = table.Column<int>(type: "integer", nullable: true)
         },
   constraints: table =>
     {
        table.PrimaryKey("PK_orders", x => x.id);
   table.ForeignKey(
      name: "FK_orders_workshop_data_WorkshopDataEntityId",
         column: x => x.WorkshopDataEntityId,
 principalTable: "workshop_data",
        principalColumn: "id",
  onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
    name: "workshop_capacities",
     columns: table => new
   {
       id = table.Column<int>(type: "integer", nullable: false)
       .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
    workshop_number = table.Column<int>(type: "integer", nullable: false),
      capacity = table.Column<int>(type: "integer", nullable: false),
          WorkshopDataEntityId = table.Column<int>(type: "integer", nullable: true)
    },
                constraints: table =>
                {
   table.PrimaryKey("PK_workshop_capacities", x => x.id);
           table.ForeignKey(
   name: "FK_workshop_capacities_workshop_data_WorkshopDataEntityId",
  column: x => x.WorkshopDataEntityId,
  principalTable: "workshop_data",
          principalColumn: "id",
          onDelete: ReferentialAction.Cascade);
         });

      migrationBuilder.CreateIndex(
         name: "IX_custom_completion_dates_order_key",
        table: "custom_completion_dates",
           column: "order_key",
     unique: true);

            migrationBuilder.CreateIndex(
        name: "IX_custom_completion_dates_WorkshopDataEntityId",
        table: "custom_completion_dates",
            column: "WorkshopDataEntityId");

     migrationBuilder.CreateIndex(
        name: "IX_orders_workshop_number_order_date",
    table: "orders",
      columns: new[] { "workshop_number", "order_date" });

            migrationBuilder.CreateIndex(
  name: "IX_orders_WorkshopDataEntityId",
     table: "orders",
        column: "WorkshopDataEntityId");

        migrationBuilder.CreateIndex(
       name: "IX_workshop_capacities_workshop_number",
         table: "workshop_capacities",
         column: "workshop_number",
          unique: true);

        migrationBuilder.CreateIndex(
     name: "IX_workshop_capacities_WorkshopDataEntityId",
 table: "workshop_capacities",
     column: "WorkshopDataEntityId");
        }

    /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
{
            migrationBuilder.DropTable(
  name: "custom_completion_dates");

     migrationBuilder.DropTable(
     name: "orders");

 migrationBuilder.DropTable(
              name: "workshop_capacities");

            migrationBuilder.DropTable(
     name: "workshop_data");
        }
    }
}
