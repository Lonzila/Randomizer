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
    public class RecenzentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecenzentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Recenzent
        public async Task<IActionResult> Index()
        {
            return View(await _context.Recenzenti.ToListAsync());
        }

        // GET: Recenzent/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzent = await _context.Recenzenti
                .FirstOrDefaultAsync(m => m.RecenzentID == id);
            if (recenzent == null)
            {
                return NotFound();
            }

            return View(recenzent);
        }

        // GET: Recenzent/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Recenzent/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RecenzentID,Sifra,Ime,Priimek,EPosta,SteviloProjektov,Drzava,Porocevalec")] Recenzent recenzent)
        {
            if (ModelState.IsValid)
            {
                _context.Add(recenzent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(recenzent);
        }

        // GET: Recenzent/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzent = await _context.Recenzenti.FindAsync(id);
            if (recenzent == null)
            {
                return NotFound();
            }
            return View(recenzent);
        }

        // POST: Recenzent/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecenzentID,Sifra,Ime,Priimek,EPosta,SteviloProjektov,Drzava,Porocevalec")] Recenzent recenzent)
        {
            if (id != recenzent.RecenzentID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(recenzent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RecenzentExists(recenzent.RecenzentID))
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
            return View(recenzent);
        }

        // GET: Recenzent/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var recenzent = await _context.Recenzenti
                .FirstOrDefaultAsync(m => m.RecenzentID == id);
            if (recenzent == null)
            {
                return NotFound();
            }

            return View(recenzent);
        }

        // POST: Recenzent/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var recenzent = await _context.Recenzenti.FindAsync(id);
            if (recenzent != null)
            {
                _context.Recenzenti.Remove(recenzent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RecenzentExists(int id)
        {
            return _context.Recenzenti.Any(e => e.RecenzentID == id);
        }
    }
}
