using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tablitsya3.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseQRCodeMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_completion_dates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_completion_dates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "defects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_id = table.Column<int>(type: "integer", nullable: false),
                    qr_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    worker_id = table.Column<int>(type: "integer", nullable: true),
                    workstation_id = table.Column<int>(type: "integer", nullable: true),
                    production_stage = table.Column<int>(type: "integer", nullable: false),
                    defect_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    is_repairable = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    repaired_by_worker_id = table.Column<int>(type: "integer", nullable: true),
                    repaired_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    repair_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_defects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "imported_projects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    imported_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    material_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    operation_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    products_count = table.Column<int>(type: "integer", nullable: false),
                    parts_count = table.Column<int>(type: "integer", nullable: false),
                    total_square_meters = table.Column<double>(type: "double precision", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    workshop_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imported_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    square_meters = table.Column<double>(type: "double precision", nullable: false),
                    order_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "original_workshops",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    order_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_workshop_number = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_original_workshops", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_external_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    part_id = table.Column<int>(type: "integer", nullable: false),
                    part_counter = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    length = table.Column<double>(type: "double precision", nullable: false),
                    width = table.Column<double>(type: "double precision", nullable: false),
                    thickness = table.Column<double>(type: "double precision", nullable: false),
                    material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    order_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_cut_completed = table.Column<bool>(type: "boolean", nullable: false),
                    cut_completed_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_edge_banding_completed = table.Column<bool>(type: "boolean", nullable: false),
                    edge_banding_completed_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_drilling_completed = table.Column<bool>(type: "boolean", nullable: false),
                    drilling_completed_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_sorting_completed = table.Column<bool>(type: "boolean", nullable: false),
                    sorting_completed_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_packing_completed = table.Column<bool>(type: "boolean", nullable: false),
                    packing_completed_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    requires_cutting = table.Column<bool>(type: "boolean", nullable: false),
                    requires_edge_banding = table.Column<bool>(type: "boolean", nullable: false),
                    requires_drilling = table.Column<bool>(type: "boolean", nullable: false),
                    requires_sorting = table.Column<bool>(type: "boolean", nullable: false),
                    requires_packing = table.Column<bool>(type: "boolean", nullable: false),
                    edge_banding_sides_required = table.Column<int>(type: "integer", nullable: false),
                    edge_banding_sides_completed = table.Column<int>(type: "integer", nullable: false),
                    edge_banding_completed_dates = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_uuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    material_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    operation_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scan_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    part_id = table.Column<int>(type: "integer", nullable: false),
                    qr_code = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stage = table.Column<int>(type: "integer", nullable: false),
                    scan_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    worker_id = table.Column<int>(type: "integer", nullable: true),
                    workstation_id = table.Column<int>(type: "integer", nullable: true),
                    device_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    session_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_kpis",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    production_stage = table.Column<int>(type: "integer", nullable: false),
                    parts_processed = table.Column<int>(type: "integer", nullable: false),
                    total_square_meters = table.Column<double>(type: "double precision", nullable: false),
                    defects_count = table.Column<int>(type: "integer", nullable: false),
                    work_minutes = table.Column<int>(type: "integer", nullable: false),
                    avg_time_per_part = table.Column<double>(type: "double precision", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_kpis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "worker_sessions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_id = table.Column<int>(type: "integer", nullable: false),
                    workstation_id = table.Column<int>(type: "integer", nullable: false),
                    session_token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scans_count = table.Column<int>(type: "integer", nullable: false),
                    last_scan_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    worker_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    pin_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    pin_code_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    allowed_stages = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workshop_capacities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workshop_capacities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workshop_data",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_updated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    production_lead_time = table.Column<int>(type: "integer", nullable: false),
                    days_before_production = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workshop_data", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workshop_days_before_production",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    days_before_production = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workshop_days_before_production", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workshop_production_lead_times",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    production_lead_time = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workshop_production_lead_times", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workstations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    workshop_number = table.Column<int>(type: "integer", nullable: false),
                    production_stage = table.Column<int>(type: "integer", nullable: false),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    requires_worker_auth = table.Column<bool>(type: "boolean", nullable: false),
                    session_timeout_minutes = table.Column<int>(type: "integer", nullable: false),
                    device_identifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workstations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_completion_dates_order_key",
                table: "custom_completion_dates",
                column: "order_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_defects_created_date",
                table: "defects",
                column: "created_date");

            migrationBuilder.CreateIndex(
                name: "IX_defects_part_id",
                table: "defects",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "IX_defects_status",
                table: "defects",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_defects_worker_id",
                table: "defects",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_imported_projects_file_name",
                table: "imported_projects",
                column: "file_name");

            migrationBuilder.CreateIndex(
                name: "IX_imported_projects_project_uuid",
                table: "imported_projects",
                column: "project_uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_workshop_number_order_date",
                table: "orders",
                columns: new[] { "workshop_number", "order_date" });

            migrationBuilder.CreateIndex(
                name: "IX_original_workshops_order_key",
                table: "original_workshops",
                column: "order_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parts_is_cut_completed_is_edge_banding_completed_is_drillin~",
                table: "parts",
                columns: new[] { "is_cut_completed", "is_edge_banding_completed", "is_drilling_completed", "is_sorting_completed", "is_packing_completed" });

            migrationBuilder.CreateIndex(
                name: "IX_parts_order_name",
                table: "parts",
                column: "order_name");

            migrationBuilder.CreateIndex(
                name: "IX_parts_project_external_uuid_part_id_part_counter",
                table: "parts",
                columns: new[] { "project_external_uuid", "part_id", "part_counter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_parts_source_file_name",
                table: "parts",
                column: "source_file_name");

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                table: "products",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_products_project_uuid_product_id",
                table: "products",
                columns: new[] { "project_uuid", "product_id" });

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_part_id",
                table: "scan_logs",
                column: "part_id");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_qr_code",
                table: "scan_logs",
                column: "qr_code");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_scan_date",
                table: "scan_logs",
                column: "scan_date");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_worker_id",
                table: "scan_logs",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_scan_logs_workstation_id",
                table: "scan_logs",
                column: "workstation_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_kpis_date",
                table: "worker_kpis",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_worker_kpis_worker_id_date_production_stage",
                table: "worker_kpis",
                columns: new[] { "worker_id", "date", "production_stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_sessions_is_active_workstation_id",
                table: "worker_sessions",
                columns: new[] { "is_active", "workstation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_worker_sessions_session_token",
                table: "worker_sessions",
                column: "session_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_worker_sessions_worker_id",
                table: "worker_sessions",
                column: "worker_id");

            migrationBuilder.CreateIndex(
                name: "IX_worker_sessions_workstation_id",
                table: "worker_sessions",
                column: "workstation_id");

            migrationBuilder.CreateIndex(
                name: "IX_workers_is_active",
                table: "workers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_workers_worker_code",
                table: "workers",
                column: "worker_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workers_workshop_number",
                table: "workers",
                column: "workshop_number");

            migrationBuilder.CreateIndex(
                name: "IX_workshop_capacities_workshop_number",
                table: "workshop_capacities",
                column: "workshop_number");

            migrationBuilder.CreateIndex(
                name: "IX_workshop_days_before_production_workshop_number",
                table: "workshop_days_before_production",
                column: "workshop_number");

            migrationBuilder.CreateIndex(
                name: "IX_workshop_production_lead_times_workshop_number",
                table: "workshop_production_lead_times",
                column: "workshop_number");

            migrationBuilder.CreateIndex(
                name: "IX_workstations_is_active",
                table: "workstations",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_workstations_production_stage",
                table: "workstations",
                column: "production_stage");

            migrationBuilder.CreateIndex(
                name: "IX_workstations_station_code",
                table: "workstations",
                column: "station_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workstations_workshop_number",
                table: "workstations",
                column: "workshop_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_completion_dates");

            migrationBuilder.DropTable(
                name: "defects");

            migrationBuilder.DropTable(
                name: "imported_projects");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "original_workshops");

            migrationBuilder.DropTable(
                name: "parts");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "scan_logs");

            migrationBuilder.DropTable(
                name: "worker_kpis");

            migrationBuilder.DropTable(
                name: "worker_sessions");

            migrationBuilder.DropTable(
                name: "workers");

            migrationBuilder.DropTable(
                name: "workshop_capacities");

            migrationBuilder.DropTable(
                name: "workshop_data");

            migrationBuilder.DropTable(
                name: "workshop_days_before_production");

            migrationBuilder.DropTable(
                name: "workshop_production_lead_times");

            migrationBuilder.DropTable(
                name: "workstations");
        }
    }
}
