using ClosedXML.Excel;
using Randomizer.Models.Randomizer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Randomizer.Services
{
    public class ExcelService
    {
        public async Task<List<ExcelPodatkiModel>> PridobiPodatkeIzExcela(string filePath)
        {
            var podatki = new List<ExcelPodatkiModel>();

            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.First();

                // Začnemo branje vrstic od druge vrstice naprej (preskočimo naslove)
                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    try
                    {
                        // Izpis vrednosti celic pred pretvorbo
                        Console.WriteLine($"Vrednost celice za prijavo: {row.Cell(2).Value}");
                        Console.WriteLine($"Vrednost celice za recenzenta: {row.Cell(7).Value}");

                        // Preveri, če celica vsebuje veljavno številko za prijavo
                        if (int.TryParse(row.Cell(2).Value.ToString(), out int prijava))
                        {
                            // Preveri, če celica vsebuje veljavno številko za recenzenta
                            if (int.TryParse(row.Cell(7).Value.ToString(), out int recenzentID))
                            {
                                // Dodaj podatke v seznam, če sta obe številki veljavni
                                podatki.Add(new ExcelPodatkiModel { Prijava = prijava, ID = recenzentID });
                            }
                            else
                            {
                                // Če ni veljavnega recenzenta, izpiši napako
                                Console.WriteLine("Neveljavna šifra recenzenta v vrstici.");
                            }
                        }
                        else
                        {
                            // Če ni veljavne prijave, izpiši napako
                            Console.WriteLine("Neveljavna številka prijave v vrstici.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Obdelava napake, če pride do nepričakovane izjeme
                        Console.WriteLine($"Napaka pri obdelavi vrstice: {ex.Message}");
                    }
                }
            }

            return await Task.FromResult(podatki);
        }

    }

}
