using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pedidos360.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.CreateTable(
                name: "CATEGORIA",
                columns: table => new
                {
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIA", x => x.CategoriaId);
                });

            migrationBuilder.CreateTable(
                name: "CLIENTE",
                columns: table => new
                {
                    ClienteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTE", x => x.ClienteId);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCTO",
                columns: table => new
                {
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImpuestoPorc = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    ImagenUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCTO", x => x.ProductoId);
                    table.ForeignKey(
                        name: "FK_PRODUCTO_CATEGORIA_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CATEGORIA",
                        principalColumn: "CategoriaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CLIENTE_DIRECCION",
                columns: table => new
                {
                    ClienteDireccionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Distrito = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTE_DIRECCION", x => x.ClienteDireccionId);
                    table.ForeignKey(
                        name: "FK_CLIENTE_DIRECCION_CLIENTE_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "CLIENTE",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PEDIDO",
                columns: table => new
                {
                    PedidoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Impuestos = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEDIDO", x => x.PedidoId);
                    table.ForeignKey(
                        name: "FK_PEDIDO_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PEDIDO_CLIENTE_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "CLIENTE",
                        principalColumn: "ClienteId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PEDIDO_DETALLE",
                columns: table => new
                {
                    PedidoDetalleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImpuestoPorc = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalLinea = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PedidoId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PEDIDO_DETALLE", x => x.PedidoDetalleId);
                    table.ForeignKey(
                        name: "FK_PEDIDO_DETALLE_PEDIDO_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "PEDIDO",
                        principalColumn: "PedidoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PEDIDO_DETALLE_PRODUCTO_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "PRODUCTO",
                        principalColumn: "ProductoId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTE_Cedula",
                table: "CLIENTE",
                column: "Cedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTE_Correo",
                table: "CLIENTE",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTE_DIRECCION_ClienteId",
                table: "CLIENTE_DIRECCION",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_ClienteId",
                table: "PEDIDO",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_UsuarioId",
                table: "PEDIDO",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_DETALLE_PedidoId",
                table: "PEDIDO_DETALLE",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_PEDIDO_DETALLE_ProductoId",
                table: "PEDIDO_DETALLE",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCTO_CategoriaId",
                table: "PRODUCTO",
                column: "CategoriaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLIENTE_DIRECCION");

            migrationBuilder.DropTable(
                name: "PEDIDO_DETALLE");

            migrationBuilder.DropTable(
                name: "PEDIDO");

            migrationBuilder.DropTable(
                name: "PRODUCTO");

            migrationBuilder.DropTable(
                name: "CLIENTE");

            migrationBuilder.DropTable(
                name: "CATEGORIA");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
