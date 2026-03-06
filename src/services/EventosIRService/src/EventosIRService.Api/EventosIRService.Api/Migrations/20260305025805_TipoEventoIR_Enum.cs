using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventosIRService.Api.Migrations
{
    /// <inheritdoc />
    public partial class TipoEventoIR_Enum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventosIR_ClienteId_DataEvento",
                table: "EventosIR");

            migrationBuilder.DropIndex(
                name: "IX_EventosIR_Ticker_DataEvento",
                table: "EventosIR");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "EventosIR",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "EventosIR",
                type: "varchar(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Quantidade",
                table: "EventosIR",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_EventosIR_ClienteId_Tipo_Ticker_DataEvento",
                table: "EventosIR",
                columns: new[] { "ClienteId", "Tipo", "Ticker", "DataEvento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventosIR_ClienteId_Tipo_Ticker_DataEvento",
                table: "EventosIR");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "EventosIR",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "EventosIR",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<long>(
                name: "Quantidade",
                table: "EventosIR",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_EventosIR_ClienteId_DataEvento",
                table: "EventosIR",
                columns: new[] { "ClienteId", "DataEvento" });

            migrationBuilder.CreateIndex(
                name: "IX_EventosIR_Ticker_DataEvento",
                table: "EventosIR",
                columns: new[] { "Ticker", "DataEvento" });
        }
    }
}
