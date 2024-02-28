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
}
