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
    public class GrozdiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GrozdiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Grozdi
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Grozdi.Include(g => g.Podpodrocje);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Grozdi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdi = await _context.Grozdi
                .Include(g => g.Podpodrocje)
                .FirstOrDefaultAsync(m => m.GrozdID == id);
            if (grozdi == null)
            {
                return NotFound();
            }

            return View(grozdi);
        }

        // GET: Grozdi/Create
        public IActionResult Create()
        {
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            return View();
        }

        // POST: Grozdi/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GrozdID,PodpodrocjeID,Koda")] Grozdi grozdi)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grozdi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", grozdi.PodpodrocjeID);
            return View(grozdi);
        }

        // GET: Grozdi/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdi = await _context.Grozdi.FindAsync(id);
            if (grozdi == null)
            {
                return NotFound();
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", grozdi.PodpodrocjeID);
            return View(grozdi);
        }

        // POST: Grozdi/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GrozdID,PodpodrocjeID,Koda")] Grozdi grozdi)
        {
            if (id != grozdi.GrozdID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grozdi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GrozdiExists(grozdi.GrozdID))
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
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", grozdi.PodpodrocjeID);
            return View(grozdi);
        }

        // GET: Grozdi/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdi = await _context.Grozdi
                .Include(g => g.Podpodrocje)
                .FirstOrDefaultAsync(m => m.GrozdID == id);
            if (grozdi == null)
            {
                return NotFound();
            }

            return View(grozdi);
        }

        // POST: Grozdi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grozdi = await _context.Grozdi.FindAsync(id);
            if (grozdi != null)
            {
                _context.Grozdi.Remove(grozdi);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GrozdiExists(int id)
        {
            return _context.Grozdi.Any(e => e.GrozdID == id);
        }
    }
}
