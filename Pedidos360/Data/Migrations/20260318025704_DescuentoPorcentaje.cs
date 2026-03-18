using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pedidos360.Data.Migrations
{
    /// <inheritdoc />
    public partial class DescuentoPorcentaje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Resetear descuentos existentes (eran montos ₡, ahora son porcentajes 0-100)
            migrationBuilder.Sql("UPDATE [PEDIDO_DETALLE] SET [Descuento] = 0");

            migrationBuilder.AlterColumn<decimal>(
                name: "Descuento",
                table: "PEDIDO_DETALLE",
                type: "decimal(5,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "CLIENTE",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Descuento",
                table: "PEDIDO_DETALLE",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "CLIENTE",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
