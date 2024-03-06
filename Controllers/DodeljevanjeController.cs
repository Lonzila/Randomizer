using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

public class DodeljevanjeController : Controller
{
    private readonly DodeljevanjeRecenzentovService _dodeljevanjeRecenzentovService;

    public DodeljevanjeController(DodeljevanjeRecenzentovService dodeljevanjeRecenzentovService)
    {
        _dodeljevanjeRecenzentovService = dodeljevanjeRecenzentovService;
    }

    public async Task<IActionResult> DodeliRecenzente()
    {
        await _dodeljevanjeRecenzentovService.DodeliRecenzenteAsync();
        return Ok("Recenzenti so bili dodeljeni.");
    }
    public async Task<IActionResult> PrikazGrozdov()
    {
        var grozdi = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();
        return View(grozdi);
    }


    public async Task<IActionResult> ExportToExcel()
    {
        var grozdiViewModels = await _dodeljevanjeRecenzentovService.PridobiInformacijeZaIzpisAsync();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Dodelitve Recenzentov");

            // Nastavitev glave
            var currentRow = 1;
            worksheet.Cell(currentRow, 1).Value = "Številka prijave";
            worksheet.Cell(currentRow, 2).Value = "Naslov";
            worksheet.Cell(currentRow, 3).Value = "Interdisciplinarna";
            worksheet.Cell(currentRow, 4).Value = "Število recenzentov";
            worksheet.Cell(currentRow, 5).Value = "Podpodročje";
            worksheet.Cell(currentRow, 6).Value = "Dodatno podpodročje";
            worksheet.Cell(currentRow, 7).Value = "Recenzenti";

            // Dodajanje podatkov
            foreach (var grozd in grozdiViewModels)
            {
                foreach (var prijava in grozd.Prijave)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = prijava.StevilkaPrijave;
                    worksheet.Cell(currentRow, 2).Value = prijava.Naslov;
                    worksheet.Cell(currentRow, 3).Value = prijava.Interdisc ? "Da" : "Ne";
                    worksheet.Cell(currentRow, 4).Value = prijava.SteviloRecenzentov;
                    worksheet.Cell(currentRow, 5).Value = prijava.Podpodrocje;
                    worksheet.Cell(currentRow, 6).Value = prijava.DodatnoPodpodrocje;
                    worksheet.Cell(currentRow, 7).Value = string.Join(", ", prijava.Recenzenti.Select(r => r.Ime + " " + r.Priimek + " (" + r.Vloga + ")"));
                }
            }

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DodelitveRecenzentov.xlsx");
            }
        }
    }

}
