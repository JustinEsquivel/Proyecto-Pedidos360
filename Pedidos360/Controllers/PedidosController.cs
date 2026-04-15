using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using ITextBorder = iText.Layout.Borders.Border;
using ITextSolidBorder = iText.Layout.Borders.SolidBorder;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Pedidos360.Areas.Identity.Data;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Models.ViewModels;
using System.Text.Json;

namespace Pedidos360.Controllers
{
    /// Admin y Ventas: crear y ver pedidos.
    /// Operaciones: solo ver pedidos.
    [Authorize(Roles = "Admin,Ventas,Operaciones")]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PedidosController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // INDEX con filtros
        public async Task<IActionResult> Index(
            int page = 1, int pageSize = 10,
            string? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? clienteNombre = null,
            string? vendedorNombre = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 5)  pageSize = 5;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.Estado == estado);

            if (fechaDesde.HasValue)
                query = query.Where(p => p.Fecha >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(p => p.Fecha < fechaHasta.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(clienteNombre))
                query = query.Where(p => p.Cliente!.Nombre.Contains(clienteNombre));

            if (!string.IsNullOrWhiteSpace(vendedorNombre))
                query = query.Where(p => p.Usuario!.NombreCompleto.Contains(vendedorNombre));

            query = query.OrderByDescending(p => p.Fecha);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new PedidosIndexVM
            {
                Items          = items,
                Estado         = estado,
                FechaDesde     = fechaDesde,
                FechaHasta     = fechaHasta,
                ClienteNombre  = clienteNombre,
                VendedorNombre = vendedorNombre,
                Page           = page,
                PageSize       = pageSize,
                Total          = total
            };

            return View(vm);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // CREATE
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Create()
        {
            await CargarClientesViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Create(int clienteId, string lineasJson)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                ModelState.AddModelError("clienteId", "El cliente seleccionado no existe.");
                await CargarClientesViewBag();
                return View();
            }

            List<LineaFormDto>? lineas = null;
            try
            {
                lineas = JsonSerializer.Deserialize<List<LineaFormDto>>(
                    lineasJson ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                ModelState.AddModelError("", "Los datos del pedido son inválidos.");
                await CargarClientesViewBag();
                return View();
            }

            if (lineas == null || lineas.Count == 0)
            {
                ModelState.AddModelError("", "El pedido debe contener al menos un producto.");
                await CargarClientesViewBag();
                return View();
            }

            var productoIds = lineas.Select(l => l.ProductoId).Distinct().ToList();
            var productos   = await _context.Productos
                .Where(p => productoIds.Contains(p.ProductoId) && p.Activo)
                .ToListAsync();

            foreach (var linea in lineas)
            {
                var prod = productos.FirstOrDefault(p => p.ProductoId == linea.ProductoId);
                if (prod == null)
                {
                    ModelState.AddModelError("", $"El producto ID {linea.ProductoId} no existe o está inactivo.");
                    await CargarClientesViewBag();
                    return View();
                }
                if (linea.Cantidad <= 0)
                {
                    ModelState.AddModelError("", $"La cantidad del producto '{prod.Nombre}' debe ser mayor a 0.");
                    await CargarClientesViewBag();
                    return View();
                }
                if (prod.Stock < linea.Cantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}.");
                    await CargarClientesViewBag();
                    return View();
                }
            }

            decimal subtotal  = 0m;
            decimal impuestos = 0m;
            var detalles = new List<PedidoDetalle>();

            foreach (var linea in lineas)
            {
                var prod = productos.First(p => p.ProductoId == linea.ProductoId);

                decimal descPorc     = linea.Descuento < 0 ? 0 : (linea.Descuento > 100 ? 100 : linea.Descuento);
                decimal brutoLinea   = prod.Precio * linea.Cantidad;
                decimal baseLinea    = brutoLinea * (1 - descPorc / 100m);
                decimal impuestoLin  = baseLinea * (prod.ImpuestoPorc / 100m);
                decimal totalLinea   = baseLinea + impuestoLin;

                subtotal  += baseLinea;
                impuestos += impuestoLin;

                detalles.Add(new PedidoDetalle
                {
                    ProductoId   = prod.ProductoId,
                    Cantidad     = linea.Cantidad,
                    PrecioUnit   = prod.Precio,
                    Descuento    = descPorc,
                    ImpuestoPorc = prod.ImpuestoPorc,
                    TotalLinea   = Math.Round(totalLinea, 2)
                });

                prod.Stock -= linea.Cantidad;
            }

            var usuario = await _userManager.GetUserAsync(User);
            var pedido = new Pedido
            {
                ClienteId = clienteId,
                UsuarioId = usuario!.Id,
                Fecha     = DateTime.Now,
                Subtotal  = Math.Round(subtotal,  2),
                Impuestos = Math.Round(impuestos, 2),
                Total     = Math.Round(subtotal + impuestos, 2),
                Estado    = "Pendiente",
                Detalles  = detalles
            };

            _context.Pedidos.Add(pedido);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Ok"] = $"Pedido #{pedido.PedidoId} creado correctamente.";
                return RedirectToAction(nameof(Details), new { id = pedido.PedidoId });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo guardar el pedido. Intente nuevamente.");
                await CargarClientesViewBag();
                return View();
            }
        }

        // EDIT
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return NotFound();

            if (pedido.Estado != "Pendiente")
            {
                TempData["Err"] = "Solo se pueden editar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await PrepararEditView(pedido);
            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> Edit(int id, int clienteId, string lineasJson)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return NotFound();

            if (pedido.Estado != "Pendiente")
            {
                TempData["Err"] = "Solo se pueden editar pedidos en estado Pendiente.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                ModelState.AddModelError("clienteId", "El cliente seleccionado no existe.");
                await PrepararEditViewById(id);
                return View(pedido);
            }

            List<LineaFormDto>? lineas = null;
            try
            {
                lineas = JsonSerializer.Deserialize<List<LineaFormDto>>(
                    lineasJson ?? "[]",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                ModelState.AddModelError("", "Los datos del pedido son inválidos.");
                await PrepararEditViewById(id);
                return View(pedido);
            }

            if (lineas == null || lineas.Count == 0)
            {
                ModelState.AddModelError("", "El pedido debe contener al menos un producto.");
                await PrepararEditViewById(id);
                return View(pedido);
            }

            var allProductoIds = pedido.Detalles.Select(d => d.ProductoId)
                .Union(lineas.Select(l => l.ProductoId))
                .Distinct()
                .ToList();

            var productos = await _context.Productos
                .Where(p => allProductoIds.Contains(p.ProductoId))
                .ToListAsync();

            foreach (var det in pedido.Detalles)
            {
                var prod = productos.FirstOrDefault(p => p.ProductoId == det.ProductoId);
                if (prod != null) prod.Stock += det.Cantidad;
            }

            foreach (var linea in lineas)
            {
                var prod = productos.FirstOrDefault(p => p.ProductoId == linea.ProductoId && p.Activo);
                if (prod == null)
                {
                    ModelState.AddModelError("", $"El producto ID {linea.ProductoId} no existe o está inactivo.");
                    await PrepararEditViewById(id);
                    return View(pedido);
                }
                if (linea.Cantidad <= 0)
                {
                    ModelState.AddModelError("", $"La cantidad del producto '{prod.Nombre}' debe ser mayor a 0.");
                    await PrepararEditViewById(id);
                    return View(pedido);
                }
                if (prod.Stock < linea.Cantidad)
                {
                    ModelState.AddModelError("", $"Stock insuficiente para '{prod.Nombre}'. Disponible: {prod.Stock}.");
                    await PrepararEditViewById(id);
                    return View(pedido);
                }
            }

            decimal subtotal  = 0m;
            decimal impuestos = 0m;
            var newDetalles = new List<PedidoDetalle>();

            foreach (var linea in lineas)
            {
                var prod = productos.First(p => p.ProductoId == linea.ProductoId);

                decimal descPorc    = linea.Descuento < 0 ? 0 : (linea.Descuento > 100 ? 100 : linea.Descuento);
                decimal brutoLinea  = prod.Precio * linea.Cantidad;
                decimal baseLinea   = brutoLinea * (1 - descPorc / 100m);
                decimal impuestoLin = baseLinea * (prod.ImpuestoPorc / 100m);
                decimal totalLinea  = baseLinea + impuestoLin;

                subtotal  += baseLinea;
                impuestos += impuestoLin;

                newDetalles.Add(new PedidoDetalle
                {
                    ProductoId   = prod.ProductoId,
                    Cantidad     = linea.Cantidad,
                    PrecioUnit   = prod.Precio,
                    Descuento    = descPorc,
                    ImpuestoPorc = prod.ImpuestoPorc,
                    TotalLinea   = Math.Round(totalLinea, 2)
                });

                prod.Stock -= linea.Cantidad;
            }

            _context.PedidoDetalles.RemoveRange(pedido.Detalles);

            pedido.ClienteId = clienteId;
            pedido.Subtotal  = Math.Round(subtotal,  2);
            pedido.Impuestos = Math.Round(impuestos, 2);
            pedido.Total     = Math.Round(subtotal + impuestos, 2);
            pedido.Detalles  = newDetalles;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Ok"] = $"Pedido #{id} actualizado correctamente.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se pudo guardar el pedido. Intente nuevamente.");
                await PrepararEditViewById(id);
                return View(pedido);
            }
        }

        // CAMBIAR ESTADO
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Ventas")]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return NotFound();

            if (pedido.Estado == "Pendiente" && (User.IsInRole("Admin") || User.IsInRole("Ventas")))
                pedido.Estado = "Confirmado";
            else if (pedido.Estado == "Confirmado" && User.IsInRole("Admin"))
                pedido.Estado = "Facturado";
            else
            {
                TempData["Err"] = "No se puede cambiar el estado del pedido.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _context.SaveChangesAsync();
            TempData["Ok"] = $"Pedido #{id} marcado como '{pedido.Estado}'.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // EXPORTAR PDF — factura individual
        public async Task<IActionResult> ExportarPdf(int id)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles).ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return NotFound();

            try
            {
            var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            writer.SetCloseStream(false);
            var pdf    = new PdfDocument(writer);
            var doc    = new Document(pdf, PageSize.A4);
            doc.SetMargins(40, 45, 40, 45);

            var fontReg  = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            var cDark    = new DeviceRgb(15,  23,  42);
            var cMuted   = new DeviceRgb(100, 116, 139);
            var cBorder  = new DeviceRgb(226, 232, 240);
            var cAccent  = new DeviceRgb(37,  99,  235);
            var cSuccess = new DeviceRgb(22,  163,  74);
            var cWarn    = new DeviceRgb(217, 119,   6);
            var cSurface = new DeviceRgb(248, 250, 252);

            var tHeader = new Table(UnitValue.CreatePercentArray(new float[] { 58, 42 })).UseAllAvailableWidth();
            tHeader.SetBorder(ITextBorder.NO_BORDER);

            var cLeft = new Cell().SetBorder(ITextBorder.NO_BORDER).SetPaddingBottom(4);
            cLeft.Add(new Paragraph("PEDIDOS360").SetFont(fontBold).SetFontSize(20).SetFontColor(cDark));
            cLeft.Add(new Paragraph("Sistema de Gestión de Pedidos").SetFont(fontReg).SetFontSize(8).SetFontColor(cMuted));
            tHeader.AddCell(cLeft);

            var estadoColor = pedido.Estado == "Facturado" ? cSuccess
                           : pedido.Estado == "Confirmado" ? cAccent
                           : cWarn;

            var cRight = new Cell().SetBorder(ITextBorder.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPaddingBottom(4);
            cRight.Add(new Paragraph($"FACTURA #{pedido.PedidoId:D4}").SetFont(fontBold).SetFontSize(15).SetFontColor(cAccent));
            cRight.Add(new Paragraph(pedido.Fecha.ToString("dd/MM/yyyy  HH:mm")).SetFont(fontReg).SetFontSize(8).SetFontColor(cMuted));
            cRight.Add(new Paragraph($"[ {pedido.Estado.ToUpper()} ]").SetFont(fontBold).SetFontSize(8).SetFontColor(estadoColor));
            tHeader.AddCell(cRight);

            doc.Add(tHeader);
            doc.Add(PdfSep(1.5f, cDark).SetMarginTop(8).SetMarginBottom(16));

            var tInfo = new Table(UnitValue.CreatePercentArray(new float[] { 50, 50 })).UseAllAvailableWidth();
            tInfo.SetBorder(ITextBorder.NO_BORDER);

            var cCliente = new Cell().SetBorder(ITextBorder.NO_BORDER).SetPaddingBottom(12);
            cCliente.Add(new Paragraph("FACTURAR A").SetFont(fontBold).SetFontSize(7).SetFontColor(cMuted).SetCharacterSpacing(0.8f));
            cCliente.Add(new Paragraph(pedido.Cliente?.Nombre ?? "-").SetFont(fontBold).SetFontSize(11).SetFontColor(cDark).SetMarginTop(3));
            cCliente.Add(new Paragraph($"Cédula: {pedido.Cliente?.Cedula}").SetFont(fontReg).SetFontSize(9).SetFontColor(cMuted));
            cCliente.Add(new Paragraph($"Correo: {pedido.Cliente?.Correo}").SetFont(fontReg).SetFontSize(9).SetFontColor(cMuted));
            cCliente.Add(new Paragraph($"Teléfono: {pedido.Cliente?.Telefono}").SetFont(fontReg).SetFontSize(9).SetFontColor(cMuted));
            tInfo.AddCell(cCliente);

            var cVendedor = new Cell().SetBorder(ITextBorder.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPaddingBottom(12);
            cVendedor.Add(new Paragraph("VENDEDOR").SetFont(fontBold).SetFontSize(7).SetFontColor(cMuted).SetCharacterSpacing(0.8f));
            cVendedor.Add(new Paragraph(pedido.Usuario?.NombreCompleto ?? "-").SetFont(fontBold).SetFontSize(11).SetFontColor(cDark).SetMarginTop(3));
            tInfo.AddCell(cVendedor);

            doc.Add(tInfo);
            doc.Add(PdfSep(0.5f, cBorder).SetMarginBottom(14));

            doc.Add(new Paragraph("DETALLE DE PRODUCTOS")
                .SetFont(fontBold).SetFontSize(7).SetFontColor(cMuted)
                .SetCharacterSpacing(0.8f).SetMarginBottom(6));

            var tDet = new Table(UnitValue.CreatePercentArray(new float[] { 32, 13, 10, 10, 10, 12 })).UseAllAvailableWidth();
            tDet.SetBorder(ITextBorder.NO_BORDER);

            foreach (var h in new[] { "Producto", "P. Unitario", "Cant.", "Desc. %", "IVA %", "Total" })
            {
                var hCell = new Cell()
                    .SetBackgroundColor(cDark)
                    .SetBorder(ITextBorder.NO_BORDER)
                    .SetPadding(6);
                hCell.Add(new Paragraph(h).SetFont(fontBold).SetFontSize(8).SetFontColor(ColorConstants.WHITE));
                tDet.AddHeaderCell(hCell);
            }

            bool alt = false;
            foreach (var det in pedido.Detalles)
            {
                var bg = alt ? cSurface : (Color)ColorConstants.WHITE;
                alt = !alt;

                PdfAddDetCell(tDet, det.Producto?.Nombre ?? "-", bg, cDark, fontReg, false);
                PdfAddDetCell(tDet, $"CRC {det.PrecioUnit:N2}", bg, cDark, fontReg, true);
                PdfAddDetCell(tDet, det.Cantidad.ToString(), bg, cDark, fontReg, true);
                PdfAddDetCell(tDet, $"{det.Descuento:N2}%", bg, cDark, fontReg, true);
                PdfAddDetCell(tDet, $"{det.ImpuestoPorc:N2}%", bg, cDark, fontReg, true);
                PdfAddDetCell(tDet, $"CRC {det.TotalLinea:N2}", bg, cAccent, fontBold, true);
            }

            doc.Add(tDet);
            doc.Add(PdfSep(0.5f, cBorder).SetMarginTop(12));

            var tTot = new Table(UnitValue.CreatePercentArray(new float[] { 65, 35 })).UseAllAvailableWidth();
            tTot.SetBorder(ITextBorder.NO_BORDER);

            PdfAddTotRow(tTot, "Subtotal",  $"CRC {pedido.Subtotal:N2}",  cMuted, cDark,   fontReg, fontReg,  false);
            PdfAddTotRow(tTot, "Impuestos", $"CRC {pedido.Impuestos:N2}", cMuted, cDark,   fontReg, fontReg,  false);
            PdfAddTotRow(tTot, "TOTAL",     $"CRC {pedido.Total:N2}",     cDark,  cAccent, fontBold,fontBold, true,  14);

            doc.Add(tTot);

            doc.Add(PdfSep(0.5f, cBorder).SetMarginTop(22).SetMarginBottom(6));
            doc.Add(new Paragraph("Pedidos360 - Este documento es generado automaticamente por el sistema.")
                .SetFont(fontReg).SetFontSize(7).SetFontColor(cMuted)
                .SetTextAlignment(TextAlignment.CENTER));

            doc.Close();
            return File(ms.ToArray(), "application/pdf", $"Pedido_{pedido.PedidoId:D4}.pdf");
            }
            catch (Exception ex)
            {
                return Content($"PDF ERROR: {ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }

        // EXPORTAR EXCEL
        public async Task<IActionResult> ExportarExcel(
            string? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            string? clienteNombre = null,
            string? vendedorNombre = null)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(p => p.Estado == estado);
            if (fechaDesde.HasValue)
                query = query.Where(p => p.Fecha >= fechaDesde.Value);
            if (fechaHasta.HasValue)
                query = query.Where(p => p.Fecha < fechaHasta.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(clienteNombre))
                query = query.Where(p => p.Cliente!.Nombre.Contains(clienteNombre));
            if (!string.IsNullOrWhiteSpace(vendedorNombre))
                query = query.Where(p => p.Usuario!.NombreCompleto.Contains(vendedorNombre));

            var pedidos = await query.OrderByDescending(p => p.Fecha).ToListAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var pkg = new ExcelPackage();
            var ws = pkg.Workbook.Worksheets.Add("Reporte de Ventas");

            ws.Cells["A1:H1"].Merge = true;
            ws.Cells["A1"].Value = "REPORTE DE VENTAS — PEDIDOS360";
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:H2"].Merge = true;
            ws.Cells["A2"].Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cells["A2"].Style.Font.Size = 9;
            ws.Cells["A2"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(100, 116, 139));
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var filtrosAplicados = new List<string>();
            if (!string.IsNullOrWhiteSpace(estado))         filtrosAplicados.Add($"Estado: {estado}");
            if (fechaDesde.HasValue)                        filtrosAplicados.Add($"Desde: {fechaDesde:dd/MM/yyyy}");
            if (fechaHasta.HasValue)                        filtrosAplicados.Add($"Hasta: {fechaHasta:dd/MM/yyyy}");
            if (!string.IsNullOrWhiteSpace(clienteNombre))  filtrosAplicados.Add($"Cliente: {clienteNombre}");
            if (!string.IsNullOrWhiteSpace(vendedorNombre)) filtrosAplicados.Add($"Vendedor: {vendedorNombre}");

            if (filtrosAplicados.Any())
            {
                ws.Cells["A3:H3"].Merge = true;
                ws.Cells["A3"].Value = "Filtros: " + string.Join("  |  ", filtrosAplicados);
                ws.Cells["A3"].Style.Font.Size = 8;
                ws.Cells["A3"].Style.Font.Italic = true;
                ws.Cells["A3"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(100, 116, 139));
                ws.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 5;
            var headers = new[] { "#", "Fecha", "Cliente", "Vendedor", "Estado", "Subtotal ₡", "Impuestos ₡", "Total ₡" };
            var darkColor = System.Drawing.Color.FromArgb(15, 23, 42);

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[row, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 10;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(darkColor);
                cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                cell.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(37, 99, 235));
            }

            row++;

            bool altRow = false;
            var lightBg = System.Drawing.Color.FromArgb(248, 250, 252);

            foreach (var p in pedidos)
            {
                if (altRow)
                {
                    ws.Cells[row, 1, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 1, row, 8].Style.Fill.BackgroundColor.SetColor(lightBg);
                }
                altRow = !altRow;

                ws.Cells[row, 1].Value = p.PedidoId;
                ws.Cells[row, 2].Value = p.Fecha;
                ws.Cells[row, 2].Style.Numberformat.Format = "dd/mm/yyyy hh:mm";
                ws.Cells[row, 3].Value = p.Cliente?.Nombre;
                ws.Cells[row, 4].Value = p.Usuario?.NombreCompleto;
                ws.Cells[row, 5].Value = p.Estado;
                ws.Cells[row, 6].Value = (double)p.Subtotal;
                ws.Cells[row, 7].Value = (double)p.Impuestos;
                ws.Cells[row, 8].Value = (double)p.Total;

                ws.Cells[row, 6, row, 8].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 1, row, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                ws.Cells[row, 1, row, 8].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(226, 232, 240));

                row++;
            }

            if (pedidos.Any())
            {
                int dataStart = 6; int dataEnd = row - 1;
                ws.Cells[row, 5].Value = "TOTALES";
                ws.Cells[row, 5].Style.Font.Bold = true;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[row, 6].Formula = $"SUM(F{dataStart}:F{dataEnd})";
                ws.Cells[row, 7].Formula = $"SUM(G{dataStart}:G{dataEnd})";
                ws.Cells[row, 8].Formula = $"SUM(H{dataStart}:H{dataEnd})";
                ws.Cells[row, 6, row, 8].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[row, 6, row, 8].Style.Font.Bold = true;
                ws.Cells[row, 5, row, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 5, row, 8].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(239, 246, 255));
            }

            ws.Cells.AutoFitColumns(8, 50);

            return File(pkg.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ReporteVentas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // HELPERS PRIVADOS

        private async Task PrepararEditView(Pedido pedido)
        {
            var lineasIniciales = pedido.Detalles.Select(d => new
            {
                productoId = d.ProductoId,
                nombre     = d.Producto?.Nombre ?? "",
                precio     = d.PrecioUnit,
                impuesto   = d.ImpuestoPorc,
                stock      = (d.Producto?.Stock ?? 0) + d.Cantidad,
                cantidad   = d.Cantidad,
                descuento  = d.Descuento
            });
            ViewBag.LineasIniciales = JsonSerializer.Serialize(lineasIniciales);
            ViewBag.ClienteIdActual = pedido.ClienteId;
            await CargarClientesViewBag();
        }

        private async Task PrepararEditViewById(int pedidoId)
        {
            var detalles = await _context.PedidoDetalles
                .AsNoTracking()
                .Include(d => d.Producto)
                .Where(d => d.PedidoId == pedidoId)
                .ToListAsync();

            var lineasIniciales = detalles.Select(d => new
            {
                productoId = d.ProductoId,
                nombre     = d.Producto?.Nombre ?? "",
                precio     = d.PrecioUnit,
                impuesto   = d.ImpuestoPorc,
                stock      = (d.Producto?.Stock ?? 0) + d.Cantidad,
                cantidad   = d.Cantidad,
                descuento  = d.Descuento
            });
            ViewBag.LineasIniciales = JsonSerializer.Serialize(lineasIniciales);

            var pedido = await _context.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.PedidoId == pedidoId);
            ViewBag.ClienteIdActual = pedido?.ClienteId ?? 0;
            await CargarClientesViewBag();
        }

        private async Task CargarClientesViewBag()
        {
            ViewBag.Clientes = await _context.Clientes
                .AsNoTracking()
                .OrderBy(c => c.Nombre)
                .Select(c => new SelectListItem
                {
                    Value = c.ClienteId.ToString(),
                    Text  = $"{c.Nombre} — {c.Cedula}"
                })
                .ToListAsync();
        }

        private static LineSeparator PdfSep(float w, Color c)
        {
            var line = new SolidLine(w);
            line.SetColor(c);
            return new LineSeparator(line);
        }

        private static void PdfAddDetCell(Table table, string text, Color bg, Color fg,
            PdfFont font, bool right)
        {
            var cell = new Cell()
                .SetBackgroundColor(bg)
                .SetBorder(ITextBorder.NO_BORDER)
                .SetBorderBottom(new ITextSolidBorder(new DeviceRgb(226, 232, 240), 0.5f))
                .SetPaddingTop(5).SetPaddingBottom(5)
                .SetPaddingLeft(6).SetPaddingRight(6);

            var para = new Paragraph(text).SetFont(font).SetFontSize(8.5f).SetFontColor(fg);
            if (right) para.SetTextAlignment(TextAlignment.RIGHT);
            cell.Add(para);
            table.AddCell(cell);
        }

        private static void PdfAddTotRow(Table table, string label, string value,
            Color labelColor, Color valueColor,
            PdfFont labelFont, PdfFont valueFont,
            bool bold, float valueFontSize = 10)
        {
            var lCell = new Cell().SetBorder(ITextBorder.NO_BORDER)
                .SetPaddingTop(4).SetPaddingBottom(4)
                .SetTextAlignment(TextAlignment.RIGHT);
            var lPara = new Paragraph(label).SetFont(labelFont).SetFontSize(10).SetFontColor(labelColor);
            lCell.Add(lPara);
            table.AddCell(lCell);

            var vCell = new Cell().SetBorder(ITextBorder.NO_BORDER)
                .SetPaddingTop(4).SetPaddingBottom(4)
                .SetTextAlignment(TextAlignment.RIGHT);
            var vPara = new Paragraph(value).SetFont(valueFont)
                .SetFontSize(valueFontSize).SetFontColor(valueColor);
            vCell.Add(vPara);
            table.AddCell(vCell);
        }
    }

    internal class LineaFormDto
    {
        public int     ProductoId { get; set; }
        public int     Cantidad   { get; set; }
        public decimal Descuento  { get; set; } = 0;
    }
}
