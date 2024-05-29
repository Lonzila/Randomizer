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
    public class RecenzentiZavrnitveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzentiZavrnitveController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: RecenzentiZavrnitve
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.RecenzentiZavrnitve.Include(r => r.Prijava).Include(r => r.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: RecenzentiZavrnitve/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiZavrnitve = await _context.RecenzentiZavrnitve
                .Include(r => r.Prijava)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiZavrnitve == null)
            {
                return NotFound();
            }

            return View(recenzentiZavrnitve);
        }

        // GET: RecenzentiZavrnitve/Create
        public IActionResult Create()
        {
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: RecenzentiZavrnitve/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,RecenzentID,PrijavaID,Razlog,GrozdID")] RecenzentiZavrnitve recenzentiZavrnitve)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recenzentiZavrnitve);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", recenzentiZavrnitve.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiZavrnitve.RecenzentID);
            return View(recenzentiZavrnitve);
        }

        // GET: RecenzentiZavrnitve/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiZavrnitve = await _context.RecenzentiZavrnitve.FindAsync(id);
            if (recenzentiZavrnitve == null)
            {
                return NotFound();
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", recenzentiZavrnitve.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiZavrnitve.RecenzentID);
            return View(recenzentiZavrnitve);
        }

        // POST: RecenzentiZavrnitve/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,RecenzentID,PrijavaID,Razlog,GrozdID")] RecenzentiZavrnitve recenzentiZavrnitve)
        {
            if (id != recenzentiZavrnitve.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzentiZavrnitve);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecenzentiZavrnitveExists(recenzentiZavrnitve.ID))
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
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", recenzentiZavrnitve.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", recenzentiZavrnitve.RecenzentID);
            return View(recenzentiZavrnitve);
        }

        // GET: RecenzentiZavrnitve/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzentiZavrnitve = await _context.RecenzentiZavrnitve
                .Include(r => r.Prijava)
                .Include(r => r.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (recenzentiZavrnitve == null)
            {
                return NotFound();
            }

            return View(recenzentiZavrnitve);
        }

        // POST: RecenzentiZavrnitve/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzentiZavrnitve = await _context.RecenzentiZavrnitve.FindAsync(id);
            if (recenzentiZavrnitve != null)
            {
                _context.RecenzentiZavrnitve.Remove(recenzentiZavrnitve);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzentiZavrnitveExists(int id)
        {
            return _context.RecenzentiZavrnitve.Any(e => e.ID == id);
        }
    }
}
