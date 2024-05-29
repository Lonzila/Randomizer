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
    public class GrozdiRecenzentiZavrnitveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GrozdiRecenzentiZavrnitveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: GrozdiRecenzentiZavrnitve
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.GrozdiRecenzentiZavrnitve.Include(g => g.Grozd).Include(g => g.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: GrozdiRecenzentiZavrnitve/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzentiZavrnitve = await _context.GrozdiRecenzentiZavrnitve
                .Include(g => g.Grozd)
                .Include(g => g.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (grozdiRecenzentiZavrnitve == null)
            {
                return NotFound();
            }

            return View(grozdiRecenzentiZavrnitve);
        }

        // GET: GrozdiRecenzentiZavrnitve/Create
        public IActionResult Create()
        {
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: GrozdiRecenzentiZavrnitve/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,GrozdID,RecenzentID")] GrozdiRecenzentiZavrnitve grozdiRecenzentiZavrnitve)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grozdiRecenzentiZavrnitve);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzentiZavrnitve.GrozdID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzentiZavrnitve.RecenzentID);
            return View(grozdiRecenzentiZavrnitve);
        }

        // GET: GrozdiRecenzentiZavrnitve/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzentiZavrnitve = await _context.GrozdiRecenzentiZavrnitve.FindAsync(id);
            if (grozdiRecenzentiZavrnitve == null)
            {
                return NotFound();
            }
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzentiZavrnitve.GrozdID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzentiZavrnitve.RecenzentID);
            return View(grozdiRecenzentiZavrnitve);
        }

        // POST: GrozdiRecenzentiZavrnitve/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,GrozdID,RecenzentID")] GrozdiRecenzentiZavrnitve grozdiRecenzentiZavrnitve)
        {
            if (id != grozdiRecenzentiZavrnitve.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grozdiRecenzentiZavrnitve);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GrozdiRecenzentiZavrnitveExists(grozdiRecenzentiZavrnitve.ID))
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
            ViewData["GrozdID"] = new SelectList(_context.Grozdi, "GrozdID", "GrozdID", grozdiRecenzentiZavrnitve.GrozdID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", grozdiRecenzentiZavrnitve.RecenzentID);
            return View(grozdiRecenzentiZavrnitve);
        }

        // GET: GrozdiRecenzentiZavrnitve/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grozdiRecenzentiZavrnitve = await _context.GrozdiRecenzentiZavrnitve
                .Include(g => g.Grozd)
                .Include(g => g.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (grozdiRecenzentiZavrnitve == null)
            {
                return NotFound();
            }

            return View(grozdiRecenzentiZavrnitve);
        }

        // POST: GrozdiRecenzentiZavrnitve/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grozdiRecenzentiZavrnitve = await _context.GrozdiRecenzentiZavrnitve.FindAsync(id);
            if (grozdiRecenzentiZavrnitve != null)
            {
                _context.GrozdiRecenzentiZavrnitve.Remove(grozdiRecenzentiZavrnitve);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GrozdiRecenzentiZavrnitveExists(int id)
        {
            return _context.GrozdiRecenzentiZavrnitve.Any(e => e.ID == id);
        }
    }
}
