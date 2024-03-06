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
    public class GrozdiRecenzentiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GrozdiRecenzentiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GrozdiRecenzenti
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GrozdiRecenzenti.Include(g => g.Grozd).Include(g => g.Prijava).Include(g => g.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GrozdiRecenzenti/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzenti = await _context.GrozdiRecenzenti
                .Include(g => g.Grozd)
                .Include(g => g.Prijava)
                .Include(g => g.Recenzent)
                .FirstOrDefaultAsync(m => m.GrozdRecenzentID == id);
            if (grozdiRecenzenti == null)
            {
                return NotFound();
            }

            return View(grozdiRecenzenti);
        }

        // GET: GrozdiRecenzenti/Create
        public IActionResult Create()
        {
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID");
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: GrozdiRecenzenti/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GrozdRecenzentID,GrozdID,RecenzentID,Vloga,PrijavaID")] GrozdiRecenzenti grozdiRecenzenti)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grozdiRecenzenti);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzenti.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", grozdiRecenzenti.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzenti.RecenzentID);
            return View(grozdiRecenzenti);
        }

        // GET: GrozdiRecenzenti/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzenti = await _context.GrozdiRecenzenti.FindAsync(id);
            if (grozdiRecenzenti == null)
            {
                return NotFound();
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzenti.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", grozdiRecenzenti.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzenti.RecenzentID);
            return View(grozdiRecenzenti);
        }

        // POST: GrozdiRecenzenti/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GrozdRecenzentID,GrozdID,RecenzentID,Vloga,PrijavaID")] GrozdiRecenzenti grozdiRecenzenti)
        {
            if (id != grozdiRecenzenti.GrozdRecenzentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grozdiRecenzenti);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GrozdiRecenzentiExists(grozdiRecenzenti.GrozdRecenzentID))
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
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzenti.GrozdID);
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", grozdiRecenzenti.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzenti.RecenzentID);
            return View(grozdiRecenzenti);
        }

        // GET: GrozdiRecenzenti/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzenti = await _context.GrozdiRecenzenti
                .Include(g => g.Grozd)
                .Include(g => g.Prijava)
                .Include(g => g.Recenzent)
                .FirstOrDefaultAsync(m => m.GrozdRecenzentID == id);
            if (grozdiRecenzenti == null)
            {
                return NotFound();
            }

            return View(grozdiRecenzenti);
        }

        // POST: GrozdiRecenzenti/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grozdiRecenzenti = await _context.GrozdiRecenzenti.FindAsync(id);
            if (grozdiRecenzenti != null)
            {
                _context.GrozdiRecenzenti.Remove(grozdiRecenzenti);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GrozdiRecenzentiExists(int id)
        {
            return _context.GrozdiRecenzenti.Any(e => e.GrozdRecenzentID == id);
        }
    }
}
