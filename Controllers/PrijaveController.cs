using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Models;

namespace Randomizer.Controllers
{
    public class PrijaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrijaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Prijave
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Prijave.Include(p => p.DodatnoPodpodrocje).Include(p => p.Podpodrocje);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Prijave/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijave = await _context.Prijave
                .Include(p => p.DodatnoPodpodrocje)
          
                .Include(p => p.Podpodrocje)

                .FirstOrDefaultAsync(m => m.PrijavaID == id);
            if (prijave == null)
            {
                return NotFound();
            }

            return View(prijave);
        }

        // GET: Prijave/Create
        public IActionResult Create()
        {
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["DodatnoPodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            return View();
        }

        // POST: Prijave/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrijavaID,StevilkaPrijave,VrstaProjekta,PodrocjeID,PodpodrocjeID,DodatnoPodrocjeID,DodatnoPodpodrocjeID,Naslov,SteviloRecenzentov,Interdisc,PartnerskaAgencija1,PartnerskaAgencija2,AngNaslov,Vodja,SifraVodje,NazivRO,AngNazivRO,SifraRO")] Prijave prijave)
        {
            if (ModelState.IsValid)
            {
                _context.Add(prijave);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.DodatnoPodpodrocjeID);
           
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.PodpodrocjeID);
           
            return View(prijave);
        }

        // GET: Prijave/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijave = await _context.Prijave.FindAsync(id);
            if (prijave == null)
            {
                return NotFound();
            }
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.DodatnoPodpodrocjeID);

            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.PodpodrocjeID);

            return View(prijave);
        }

        // POST: Prijave/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrijavaID,StevilkaPrijave,VrstaProjekta,PodrocjeID,PodpodrocjeID,DodatnoPodrocjeID,DodatnoPodpodrocjeID,Naslov,SteviloRecenzentov,Interdisc,PartnerskaAgencija1,PartnerskaAgencija2,AngNaslov,Vodja,SifraVodje,NazivRO,AngNazivRO,SifraRO")] Prijave prijave)
        {
            if (id != prijave.PrijavaID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prijave);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrijaveExists(prijave.PrijavaID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.DodatnoPodpodrocjeID);
         
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijave.PodpodrocjeID);

            return View(prijave);
        }

        // GET: Prijave/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijave = await _context.Prijave
                .Include(p => p.DodatnoPodpodrocje)
                
                .Include(p => p.Podpodrocje)
       
                .FirstOrDefaultAsync(m => m.PrijavaID == id);
            if (prijave == null)
            {
                return NotFound();
            }

            return View(prijave);
        }

        // POST: Prijave/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prijave = await _context.Prijave.FindAsync(id);
            if (prijave != null)
            {
                _context.Prijave.Remove(prijave);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PrijaveExists(int id)
        {
            return _context.Prijave.Any(e => e.PrijavaID == id);
        }
    }
}
