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
    public class RecenzentiPodpodrocjaFullController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzentiPodpodrocjaFullController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RecenzentiPodpodrocjaFull
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.RecenzentiPodpodrocjaFull.Include(r => r.Podpodrocje).Include(r => r.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: RecenzentiPodpodrocjaFull/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodpodrocjaFull = await _context.RecenzentiPodpodrocjaFull
                .Include(r => r.Podpodrocje)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiPodpodrocjaFull == null)
            {
                return NotFound();
            }

            return View(recenzentiPodpodrocjaFull);
        }

        // GET: RecenzentiPodpodrocjaFull/Create
        public IActionResult Create()
        {
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: RecenzentiPodpodrocjaFull/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,RecenzentID,PodpodrocjeID")] RecenzentiPodpodrocjaFull recenzentiPodpodrocjaFull)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recenzentiPodpodrocjaFull);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodpodrocjaFull.PodpodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodpodrocjaFull.RecenzentID);
            return View(recenzentiPodpodrocjaFull);
        }

        // GET: RecenzentiPodpodrocjaFull/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodpodrocjaFull = await _context.RecenzentiPodpodrocjaFull.FindAsync(id);
            if (recenzentiPodpodrocjaFull == null)
            {
                return NotFound();
            }
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodpodrocjaFull.PodpodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodpodrocjaFull.RecenzentID);
            return View(recenzentiPodpodrocjaFull);
        }

        // POST: RecenzentiPodpodrocjaFull/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,RecenzentID,PodpodrocjeID")] RecenzentiPodpodrocjaFull recenzentiPodpodrocjaFull)
        {
            if (id != recenzentiPodpodrocjaFull.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzentiPodpodrocjaFull);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecenzentiPodpodrocjaFullExists(recenzentiPodpodrocjaFull.ID))
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
            ViewData["PodpodrocjeID"] = new SelectList(_context.Podpodrocje, "PodpodrocjeID", "PodpodrocjeID", recenzentiPodpodrocjaFull.PodpodrocjeID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiPodpodrocjaFull.RecenzentID);
            return View(recenzentiPodpodrocjaFull);
        }

        // GET: RecenzentiPodpodrocjaFull/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiPodpodrocjaFull = await _context.RecenzentiPodpodrocjaFull
                .Include(r => r.Podpodrocje)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiPodpodrocjaFull == null)
            {
                return NotFound();
            }

            return View(recenzentiPodpodrocjaFull);
        }

        // POST: RecenzentiPodpodrocjaFull/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzentiPodpodrocjaFull = await _context.RecenzentiPodpodrocjaFull.FindAsync(id);
            if (recenzentiPodpodrocjaFull != null)
            {
                _context.RecenzentiPodpodrocjaFull.Remove(recenzentiPodpodrocjaFull);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzentiPodpodrocjaFullExists(int id)
        {
            return _context.RecenzentiPodpodrocjaFull.Any(e => e.ID == id);
        }
    }
}
