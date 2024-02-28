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
    public class PodrocjeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PodrocjeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Podrocje
        public async Task<IActionResult> Index()
        {
            return View(await _context.Podrocje.ToListAsync());
        }

        // GET: Podrocje/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podrocje = await _context.Podrocje
                .FirstOrDefaultAsync(m => m.PodrocjeID == id);
            if (podrocje == null)
            {
                return NotFound();
            }

            return View(podrocje);
        }

        // GET: Podrocje/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Podrocje/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PodrocjeID,Naziv,Koda")] Podrocje podrocje)
        {
            if (ModelState.IsValid)
            {
                _context.Add(podrocje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(podrocje);
        }

        // GET: Podrocje/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podrocje = await _context.Podrocje.FindAsync(id);
            if (podrocje == null)
            {
                return NotFound();
            }
            return View(podrocje);
        }

        // POST: Podrocje/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PodrocjeID,Naziv,Koda")] Podrocje podrocje)
        {
            if (id != podrocje.PodrocjeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(podrocje);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PodrocjeExists(podrocje.PodrocjeID))
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
            return View(podrocje);
        }

        // GET: Podrocje/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var podrocje = await _context.Podrocje
                .FirstOrDefaultAsync(m => m.PodrocjeID == id);
            if (podrocje == null)
            {
                return NotFound();
            }

            return View(podrocje);
        }

        // POST: Podrocje/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var podrocje = await _context.Podrocje.FindAsync(id);
            if (podrocje != null)
            {
                _context.Podrocje.Remove(podrocje);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PodrocjeExists(int id)
        {
            return _context.Podrocje.Any(e => e.PodrocjeID == id);
        }
    }
}
