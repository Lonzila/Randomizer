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
    public class PodpodrocjeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PodpodrocjeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Podpodrocje
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Podpodrocje.Include(p => p.Podrocje);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Podpodrocje/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podpodrocje = await _context.Podpodrocje
                .Include(p => p.Podrocje)
                .FirstOrDefaultAsync(m => m.PodpodrocjeID == id);
            if (podpodrocje == null)
            {
                return NotFound();
            }

            return View(podpodrocje);
        }

        // GET: Podpodrocje/Create
        public IActionResult Create()
        {
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            return View();
        }

        // POST: Podpodrocje/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PodpodrocjeID,PodrocjeID,Naziv,Koda")] Podpodrocje podpodrocje)
        {
            if (ModelState.IsValid)
            {
                _context.Add(podpodrocje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", podpodrocje.PodrocjeID);
            return View(podpodrocje);
        }

        // GET: Podpodrocje/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podpodrocje = await _context.Podpodrocje.FindAsync(id);
            if (podpodrocje == null)
            {
                return NotFound();
            }
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", podpodrocje.PodrocjeID);
            return View(podpodrocje);
        }

        // POST: Podpodrocje/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PodpodrocjeID,PodrocjeID,Naziv,Koda")] Podpodrocje podpodrocje)
        {
            if (id != podpodrocje.PodpodrocjeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(podpodrocje);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PodpodrocjeExists(podpodrocje.PodpodrocjeID))
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
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", podpodrocje.PodrocjeID);
            return View(podpodrocje);
        }

        // GET: Podpodrocje/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podpodrocje = await _context.Podpodrocje
                .Include(p => p.Podrocje)
                .FirstOrDefaultAsync(m => m.PodpodrocjeID == id);
            if (podpodrocje == null)
            {
                return NotFound();
            }

            return View(podpodrocje);
        }

        // POST: Podpodrocje/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var podpodrocje = await _context.Podpodrocje.FindAsync(id);
            if (podpodrocje != null)
            {
                _context.Podpodrocje.Remove(podpodrocje);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PodpodrocjeExists(int id)
        {
            return _context.Podpodrocje.Any(e => e.PodpodrocjeID == id);
        }
    }
}
