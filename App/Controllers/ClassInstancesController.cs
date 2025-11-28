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
    public class ClassInstancesController : Controller
    {
        private readonly OltpDbContext _context;

        public ClassInstancesController(OltpDbContext context)
        {
            _context = context;
        }

        // GET: ClassInstances
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Classes.Include(c => c.Course).Include(c => c.Teacher).Include(c => c.Term);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ClassInstances/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classInstance = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Term)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classInstance == null)
            {
                return NotFound();
            }

            return View(classInstance);
        }

        // GET: ClassInstances/Create
        public IActionResult Create()
        {
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id");
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id");
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id");
            return View();
        }

        // POST: ClassInstances/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Location,Day,Period,StartDate,EndDate,CourseId,TeacherId,TermId")] ClassInstance classInstance)
        {
            if (ModelState.IsValid)
            {
                classInstance.Id = Guid.NewGuid();
                _context.Add(classInstance);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", classInstance.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", classInstance.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", classInstance.TermId);
            return View(classInstance);
        }

        // GET: ClassInstances/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classInstance = await _context.Classes.FindAsync(id);
            if (classInstance == null)
            {
                return NotFound();
            }
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", classInstance.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", classInstance.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", classInstance.TermId);
            return View(classInstance);
        }

        // POST: ClassInstances/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Location,Day,Period,StartDate,EndDate,CourseId,TeacherId,TermId")] ClassInstance classInstance)
        {
            if (id != classInstance.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classInstance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassInstanceExists(classInstance.Id))
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
            ViewData["CourseId"] = new SelectList(_context.Courses, "Id", "Id", classInstance.CourseId);
            ViewData["TeacherId"] = new SelectList(_context.Teachers, "Id", "Id", classInstance.TeacherId);
            ViewData["TermId"] = new SelectList(_context.Terms, "Id", "Id", classInstance.TermId);
            return View(classInstance);
        }

        // GET: ClassInstances/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classInstance = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .Include(c => c.Term)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classInstance == null)
            {
                return NotFound();
            }

            return View(classInstance);
        }

        // POST: ClassInstances/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var classInstance = await _context.Classes.FindAsync(id);
            if (classInstance != null)
            {
                _context.Classes.Remove(classInstance);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassInstanceExists(Guid id)
        {
            return _context.Classes.Any(e => e.Id == id);
        }
    }
}
