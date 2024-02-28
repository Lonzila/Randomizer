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
    public class IzloceniCOIController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IzloceniCOIController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: IzloceniCOI
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.IzloceniCOI.Include(i => i.Prijava).Include(i => i.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: IzloceniCOI/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniCOI = await _context.IzloceniCOI
                .Include(i => i.Prijava)
                .Include(i => i.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (izloceniCOI == null)
            {
                return NotFound();
            }

            return View(izloceniCOI);
        }

        // GET: IzloceniCOI/Create
        public IActionResult Create()
        {
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: IzloceniCOI/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PrijavaID,RecenzentID,Razlog")] IzloceniCOI izloceniCOI)
        {
            if (ModelState.IsValid)
            {
                _context.Add(izloceniCOI);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniCOI.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniCOI.RecenzentID);
            return View(izloceniCOI);
        }

        // GET: IzloceniCOI/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniCOI = await _context.IzloceniCOI.FindAsync(id);
            if (izloceniCOI == null)
            {
                return NotFound();
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniCOI.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniCOI.RecenzentID);
            return View(izloceniCOI);
        }

        // POST: IzloceniCOI/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PrijavaID,RecenzentID,Razlog")] IzloceniCOI izloceniCOI)
        {
            if (id != izloceniCOI.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(izloceniCOI);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IzloceniCOIExists(izloceniCOI.ID))
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
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniCOI.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniCOI.RecenzentID);
            return View(izloceniCOI);
        }

        // GET: IzloceniCOI/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniCOI = await _context.IzloceniCOI
                .Include(i => i.Prijava)
                .Include(i => i.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (izloceniCOI == null)
            {
                return NotFound();
            }

            return View(izloceniCOI);
        }

        // POST: IzloceniCOI/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var izloceniCOI = await _context.IzloceniCOI.FindAsync(id);
            if (izloceniCOI != null)
            {
                _context.IzloceniCOI.Remove(izloceniCOI);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IzloceniCOIExists(int id)
        {
            return _context.IzloceniCOI.Any(e => e.ID == id);
        }
    }
}
