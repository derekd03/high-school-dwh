using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Models;

namespace App.Controllers
{
    public class ClassController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Class
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Classes.Include(@ => @.Course).Include(@ => @.Teacher).Include(@ => @.Term);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Class/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes
                .Include(@ => @.Course)
                .Include(@ => @.Teacher)
                .Include(@ => @.Term)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@class == null)
            {
                return NotFound();
            }

            return View(@class);
        }

        // GET: Class/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id");
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id");
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id");
            return View();
        }

        // POST: Class/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Location,Day,Period,StartDate,EndDate,CourseId,TeacherId,TermId")] Class @class)
        {
            if (ModelState.IsValid)
            {
                @class.Id = Guid.NewGuid();
                _context.Add(@class);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", @class.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", @class.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", @class.TermId);
            return View(@class);
        }

        // GET: Class/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes.FindAsync(id);
            if (@class == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", @class.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", @class.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", @class.TermId);
            return View(@class);
        }

        // POST: Class/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Location,Day,Period,StartDate,EndDate,CourseId,TeacherId,TermId")] Class @class)
        {
            if (id != @class.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@class);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(@class.Id))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", @class.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", @class.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", @class.TermId);
            return View(@class);
        }

        // GET: Class/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @class = await _context.Classes
                .Include(@ => @.Course)
                .Include(@ => @.Teacher)
                .Include(@ => @.Term)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@class == null)
            {
                return NotFound();
            }

            return View(@class);
        }

        // POST: Class/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var @class = await _context.Classes.FindAsync(id);
            if (@class != null)
            {
                _context.Classes.Remove(@class);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassExists(Guid id)
        {
            return _context.Classes.Any(e => e.Id == id);
        }
    }
}
