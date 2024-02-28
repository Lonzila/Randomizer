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
    public class PrijavaGrozdiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrijavaGrozdiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PrijavaGrozdi
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.PrijavaGrozdi.Include(p => p.Grozd).Include(p => p.Prijava);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: PrijavaGrozdi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijavaGrozdi = await _context.PrijavaGrozdi
                .Include(p => p.Grozd)
                .Include(p => p.Prijava)
                .FirstOrDefaultAsync(m => m.PrijavaGrozdiID == id);
            if (prijavaGrozdi == null)
            {
                return NotFound();
            }

            return View(prijavaGrozdi);
        }

        // GET: PrijavaGrozdi/Create
        public IActionResult Create()
        {
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID");
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID");
            return View();
        }

        // POST: PrijavaGrozdi/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrijavaGrozdiID,PrijavaID,GrozdID")] PrijavaGrozdi prijavaGrozdi)
        {
            if (ModelState.IsValid)
            {
                _context.Add(prijavaGrozdi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", prijavaGrozdi.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", prijavaGrozdi.PrijavaID);
            return View(prijavaGrozdi);
        }

        // GET: PrijavaGrozdi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijavaGrozdi = await _context.PrijavaGrozdi.FindAsync(id);
            if (prijavaGrozdi == null)
            {
                return NotFound();
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", prijavaGrozdi.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", prijavaGrozdi.PrijavaID);
            return View(prijavaGrozdi);
        }

        // POST: PrijavaGrozdi/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrijavaGrozdiID,PrijavaID,GrozdID")] PrijavaGrozdi prijavaGrozdi)
        {
            if (id != prijavaGrozdi.PrijavaGrozdiID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prijavaGrozdi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrijavaGrozdiExists(prijavaGrozdi.PrijavaGrozdiID))
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
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", prijavaGrozdi.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", prijavaGrozdi.PrijavaID);
            return View(prijavaGrozdi);
        }

        // GET: PrijavaGrozdi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prijavaGrozdi = await _context.PrijavaGrozdi
                .Include(p => p.Grozd)
                .Include(p => p.Prijava)
                .FirstOrDefaultAsync(m => m.PrijavaGrozdiID == id);
            if (prijavaGrozdi == null)
            {
                return NotFound();
            }

            return View(prijavaGrozdi);
        }

        // POST: PrijavaGrozdi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prijavaGrozdi = await _context.PrijavaGrozdi.FindAsync(id);
            if (prijavaGrozdi != null)
            {
                _context.PrijavaGrozdi.Remove(prijavaGrozdi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PrijavaGrozdiExists(int id)
        {
            return _context.PrijavaGrozdi.Any(e => e.PrijavaGrozdiID == id);
        }
    }
}
