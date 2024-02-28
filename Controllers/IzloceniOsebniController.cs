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
    public class IzloceniOsebniController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IzloceniOsebniController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: IzloceniOsebni
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.IzloceniOsebni.Include(i => i.Prijava).Include(i => i.Recenzent);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: IzloceniOsebni/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniOsebni = await _context.IzloceniOsebni
                .Include(i => i.Prijava)
                .Include(i => i.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (izloceniOsebni == null)
            {
                return NotFound();
            }

            return View(izloceniOsebni);
        }

        // GET: IzloceniOsebni/Create
        public IActionResult Create()
        {
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID");
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID");
            return View();
        }

        // POST: IzloceniOsebni/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PrijavaID,RecenzentID")] IzloceniOsebni izloceniOsebni)
        {
            if (ModelState.IsValid)
            {
                _context.Add(izloceniOsebni);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniOsebni.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniOsebni.RecenzentID);
            return View(izloceniOsebni);
        }

        // GET: IzloceniOsebni/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniOsebni = await _context.IzloceniOsebni.FindAsync(id);
            if (izloceniOsebni == null)
            {
                return NotFound();
            }
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniOsebni.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniOsebni.RecenzentID);
            return View(izloceniOsebni);
        }

        // POST: IzloceniOsebni/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PrijavaID,RecenzentID")] IzloceniOsebni izloceniOsebni)
        {
            if (id != izloceniOsebni.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(izloceniOsebni);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IzloceniOsebniExists(izloceniOsebni.ID))
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
            ViewData["PrijavaID"] = new SelectList(_context.Prijave, "PrijavaID", "PrijavaID", izloceniOsebni.PrijavaID);
            ViewData["RecenzentID"] = new SelectList(_context.Recenzenti, "RecenzentID", "RecenzentID", izloceniOsebni.RecenzentID);
            return View(izloceniOsebni);
        }

        // GET: IzloceniOsebni/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var izloceniOsebni = await _context.IzloceniOsebni
                .Include(i => i.Prijava)
                .Include(i => i.Recenzent)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (izloceniOsebni == null)
            {
                return NotFound();
            }

            return View(izloceniOsebni);
        }

        // POST: IzloceniOsebni/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var izloceniOsebni = await _context.IzloceniOsebni.FindAsync(id);
            if (izloceniOsebni != null)
            {
                _context.IzloceniOsebni.Remove(izloceniOsebni);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool IzloceniOsebniExists(int id)
        {
            return _context.IzloceniOsebni.Any(e => e.ID == id);
        }
    }
}
