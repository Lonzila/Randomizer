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
    public class RecenzentiPodrocjaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzentiPodrocjaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RecenzentiPodrocja
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.RecenzentiPodrocja.Include(r => r.Podpodrocje).Include(r => r.Podrocje).Include(r => r.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: RecenzentiPodrocja/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodrocja = await _context.RecenzentiPodrocja
                .Include(r => r.Podpodrocje)
                .Include(r => r.Podrocje)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiPodrocja == null)
            {
                return NotFound();
            }

            return View(recenzentiPodrocja);
        }

        // GET: RecenzentiPodrocja/Create
        public IActionResult Create()
        {
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: RecenzentiPodrocja/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,RecenzentID,PodrocjeID,PodpodrocjeID")] RecenzentiPodrocja recenzentiPodrocja)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recenzentiPodrocja);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodrocja.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", recenzentiPodrocja.PodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodrocja.RecenzentID);
            return View(recenzentiPodrocja);
        }

        // GET: RecenzentiPodrocja/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodrocja = await _context.RecenzentiPodrocja.FindAsync(id);
            if (recenzentiPodrocja == null)
            {
                return NotFound();
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodrocja.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", recenzentiPodrocja.PodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodrocja.RecenzentID);
            return View(recenzentiPodrocja);
        }

        // POST: RecenzentiPodrocja/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,RecenzentID,PodrocjeID,PodpodrocjeID")] RecenzentiPodrocja recenzentiPodrocja)
        {
            if (id != recenzentiPodrocja.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzentiPodrocja);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecenzentiPodrocjaExists(recenzentiPodrocja.ID))
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
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodrocja.PodpodrocjeID);
            ViewData["PodrocjeID"] = new SelectList(_context.Podrocje, "PodrocjeID", "PodrocjeID", recenzentiPodrocja.PodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodrocja.RecenzentID);
            return View(recenzentiPodrocja);
        }

        // GET: RecenzentiPodrocja/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodrocja = await _context.RecenzentiPodrocja
                .Include(r => r.Podpodrocje)
                .Include(r => r.Podrocje)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiPodrocja == null)
            {
                return NotFound();
            }

            return View(recenzentiPodrocja);
        }

        // POST: RecenzentiPodrocja/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzentiPodrocja = await _context.RecenzentiPodrocja.FindAsync(id);
            if (recenzentiPodrocja != null)
            {
                _context.RecenzentiPodrocja.Remove(recenzentiPodrocja);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzentiPodrocjaExists(int id)
        {
            return _context.RecenzentiPodrocja.Any(e => e.ID == id);
        }
    }
}
