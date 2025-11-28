using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Models.OLTP;

namespace App.Controllers
{
    public class TermsController : Controller
    {
        private readonly OltpDbContext _context;

        public TermsController(OltpDbContext context)
        {
            _context = context;
        }

        // GET: Terms
        public async Task<IActionResult> Index()
        {
            return View(await _context.Terms.ToListAsync());
        }

        // GET: Terms/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var term = await _context.Terms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (term == null)
            {
                return NotFound();
            }

            return View(term);
        }

        // GET: Terms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Terms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Year,TermNumber")] Term term)
        {
            if (ModelState.IsValid)
            {
                term.Id = Guid.NewGuid();
                _context.Add(term);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(term);
        }

        // GET: Terms/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var term = await _context.Terms.FindAsync(id);
            if (term == null)
            {
                return NotFound();
            }
            return View(term);
        }

        // POST: Terms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Year,TermNumber")] Term term)
        {
            if (id != term.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(term);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TermExists(term.Id))
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
            return View(term);
        }

        // GET: Terms/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var term = await _context.Terms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (term == null)
            {
                return NotFound();
            }

            return View(term);
        }

        // POST: Terms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var term = await _context.Terms.FindAsync(id);
            if (term != null)
            {
                _context.Terms.Remove(term);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TermExists(Guid id)
        {
            return _context.Terms.Any(e => e.Id == id);
        }
    }
}
