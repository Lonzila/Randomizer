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
    public class PrijavaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrijavaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Prijava
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Prijave.Include(p => p.DodatnoPodpodrocje).Include(p => p.DodatnoPodrocje).Include(p => p.Podpodrocje).Include(p => p.Podrocje);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Prijava/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijava = await _context.Prijave
                .Include(p => p.DodatnoPodpodrocje)
                .Include(p => p.DodatnoPodrocje)
                .Include(p => p.Podpodrocje)
                .Include(p => p.Podrocje)
                .FirstOrDefaultAsync(m => m.PrijavaID == id);
            if (prijava == null)
            {
                return NotFound();
            }

            return View(prijava);
        }

        // GET: Prijava/Create
        public IActionResult Create()
        {
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["DodatnoPodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            return View();
        }

        // POST: Prijava/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrijavaID,StevilkaPrijave,VrstaProjekta,Drzava,PodrocjeID,PodpodrocjeID,DodatnoPodrocjeID,DodatnoPodpodrocjeID,Naslov,SteviloRecenzentov,PartnerskaAgencija1,PartnerskaAgencija2")] Prijave prijava)
        {
            if (ModelState.IsValid)
            {
                _context.Add(prijava);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.DodatnoPodpodrocjeID);
            ViewData["DodatnoPodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.DodatnoPodrocjeID);
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.PodrocjeID);
            return View(prijava);
        }

        // GET: Prijava/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijava = await _context.Prijave.FindAsync(id);
            if (prijava == null)
            {
                return NotFound();
            }
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.DodatnoPodpodrocjeID);
            ViewData["DodatnoPodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.DodatnoPodrocjeID);
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.PodrocjeID);
            return View(prijava);
        }

        // POST: Prijava/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrijavaID,StevilkaPrijave,VrstaProjekta,Drzava,PodrocjeID,PodpodrocjeID,DodatnoPodrocjeID,DodatnoPodpodrocjeID,Naslov,SteviloRecenzentov,PartnerskaAgencija1,PartnerskaAgencija2")] Prijave prijava)
        {
            if (id != prijava.PrijavaID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prijava);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrijavaExists(prijava.PrijavaID))
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
            ViewData["DodatnoPodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.DodatnoPodpodrocjeID);
            ViewData["DodatnoPodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.DodatnoPodrocjeID);
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", prijava.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", prijava.PodrocjeID);
            return View(prijava);
        }

        // GET: Prijava/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijava = await _context.Prijave
                .Include(p => p.DodatnoPodpodrocje)
                .Include(p => p.DodatnoPodrocje)
                .Include(p => p.Podpodrocje)
                .Include(p => p.Podrocje)
                .FirstOrDefaultAsync(m => m.PrijavaID == id);
            if (prijava == null)
            {
                return NotFound();
            }

            return View(prijava);
        }

        // POST: Prijava/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prijava = await _context.Prijave.FindAsync(id);
            if (prijava != null)
            {
                _context.Prijave.Remove(prijava);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PrijavaExists(int id)
        {
            return _context.Prijave.Any(e => e.PrijavaID == id);
        }
    }
}
