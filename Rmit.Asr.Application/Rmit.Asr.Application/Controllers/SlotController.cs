﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Rmit.Asr.Application.Data;
using Rmit.Asr.Application.Models;
using Rmit.Asr.Application.Models.Extensions;
using Rmit.Asr.Application.Models.ViewModels;

namespace Rmit.Asr.Application.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// Houses all the staff related functions.
    /// </summary>
    public class SlotController : Controller
    {
        private readonly ApplicationDataContext _context;
        private readonly UserManager<Staff> _staffManager;
        private readonly UserManager<Student> _studentManager;

        public SlotController(ApplicationDataContext context, UserManager<Staff> staffManager, UserManager<Student> studentManager)
        {
            _context = context;
            _staffManager = staffManager;
            _studentManager = studentManager;
        }

        /// <summary>
        /// Student view for the index of slots.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = Student.RoleName)]
        public IActionResult StudentIndex()
        {
            IIncludableQueryable<Slot, Student> slots = _context.Slot
                .Include(s => s.Staff)
                .Include(s => s.Student);
            
            return View(slots);
        }
        
        /// <summary>
        /// Staff view for the index of slots.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = Staff.RoleName)]
        public IActionResult StaffIndex()
        {
            IIncludableQueryable<Slot, Student> slots = _context.Slot
                .Include(s => s.Staff)
                .Include(s => s.Student);
            
            return View(slots);
        }

        /// <summary>
        /// Create slot form.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = Staff.RoleName)]
        public IActionResult Create()
        {
            IIncludableQueryable<Slot, Student> slots = _context.Slot
                .Include(s => s.Staff)
                .Include(s => s.Student);

            var slot = new CreateSlot
            {
                Slots = slots,
                Rooms = _context.Room
            };
            
            return View(slot);
        }

        /// <summary>
        /// POST request to create a slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Staff.RoleName)]
        public async Task<IActionResult> Create([Bind("RoomId,StartTime")] CreateSlot slot)
        {
            IIncludableQueryable<Slot, Student> slots = _context.Slot
                .Include(s => s.Staff)
                .Include(s => s.Student);
            
            // Add logged in user to slot
            Staff staff = await _staffManager.GetUserAsync(User);
            slot.StaffId = staff.Id;

            // Load navigation properties
            slot.Slots = slots;
            slot.Rooms = _context.Room;
            
            if (!ModelState.IsValid) return View(slot);

            if (!_context.Room.RoomExists(slot.RoomId))
            {
                ModelState.AddModelError("RoomId", $"Room {slot.RoomId} does not exist.");
            }
            else if (!_context.Room.RoomAvailable(slot))
            {
                ModelState.AddModelError("RoomId", $"Room {slot.RoomId} has reached a maximum booking of {Room.MaxRoomBookingPerDay} per day.");
            }

            if (_context.Slot.GetStaffDailySlotCount(slot) >= Staff.MaxBookingPerDay)
            {
                ModelState.AddModelError("StartTime", $"Staff {staff.StaffId} has a maximum of {Staff.MaxBookingPerDay} bookings at {slot.StartTime:dd-MM-yyyy}.");
            }

            Slot alreadyTakenSlot = _context.Slot.GetAlreadyTakenSlot(slot).Include(s => s.Staff).FirstOrDefault();
            if (alreadyTakenSlot != null)
            {
                ModelState.AddModelError("RoomId", $"Staff {alreadyTakenSlot.Staff.StaffId} has already taken slot at room {slot.RoomId} {slot.StartTime:dd-MM-yyyy H:mm}.");
            }

            if (_context.Slot.SlotExists(slot))
            {
                ModelState.AddModelError("RoomId", $"Slot at room {slot.RoomId} {slot.StartTime:dd-MM-yyyy H:mm} already exists.");
            }

            Slot staffAlreadyCreatedSlot = _context.Slot.GetStaffSlot(slot).FirstOrDefault();
            if (staffAlreadyCreatedSlot != null)
            {
                ModelState.AddModelError("RoomId", $"You have already created a slot at room {staffAlreadyCreatedSlot.RoomId} {staffAlreadyCreatedSlot.StartTime:dd-MM-yyyy H:mm}.");
            }

            if (!ModelState.IsValid) return View(slot);
            
            _context.Slot.Add(slot);
            await _context.SaveChangesAsync();
            
            TempData["StatusMessage"] = $"Successfully created slot at room {slot.RoomId} at {slot.StartTime:dd-MM-yyyy H:mm}";
            TempData["AlertType"] = "success";

            return RedirectToAction("StaffIndex");
        }

        /// <summary>
        /// POST request to remove a slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Staff.RoleName)]
        public async Task<IActionResult> Remove([Bind("RoomId,StartTime")] RemoveSlot slot)
        {
            if (!ModelState.IsValid) return View(slot);

            if (_context.Slot.SlotBookedByStudent(slot))
                ModelState.AddModelError("StudentId", "Cannot remove slot as a student has been booked into it.");
            
            Slot deleteSlot = _context.Slot.GetSlot(slot).FirstOrDefault();
            if (deleteSlot == null)
            {
                ModelState.AddModelError(string.Empty, $"Slot at room {slot.RoomId} {slot.StartTime:dd-MM-yyyy H:mm} does not exist.");
            }
            
            if (!ModelState.IsValid || deleteSlot == null) return View(slot);

            _context.Slot.Remove(deleteSlot);

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = $"Successfully removed slot at room {slot.RoomId} at {slot.StartTime:dd-MM-yyyy H:mm}";
            TempData["AlertType"] = "success";

            return RedirectToAction("StaffIndex");
        }
        
        /// <summary>
        /// Form to make a booking.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = Student.RoleName)]
        public IActionResult Book()
        {
            IIncludableQueryable<Slot, Student> slots = _context.Slot
                .Include(s => s.Staff)
                .Include(s => s.Student);

            var slot = new BookSlot
            {
                Slots = slots,
                Rooms = _context.Room
            };
            
            return View(slot);
        }

        /// <summary>
        /// Post request to book a slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Student.RoleName)]
        public async Task<IActionResult> Book([Bind("RoomId,StartTime,StudentId")] BookSlot slot)
        {
            if (!ModelState.IsValid) return View(slot);
            
            Student student = await _studentManager.GetUserAsync(User);
            slot.StudentId = student.Id;

            if (_context.Slot.Any(s => s.StartTime.Value.Date == slot.StartTime.Value.Date && s.StudentId == slot.StudentId))
            {
                ModelState.AddModelError("StudentId", $"Student {student.StudentId} has reached their maximum bookings for this day ({slot.StartTime.GetValueOrDefault():dd-MM-yyyy})");
            }

            if (!_context.Room.RoomExists(slot.RoomId))
            {
                ModelState.AddModelError("RoomId", $"Room {slot.RoomId} does not exist.");
            }

            if (!_context.Slot.SlotExists(slot))
            {
                ModelState.AddModelError("StudentId", $"Slot does not exist in room {slot.RoomId} at {slot.StartTime:dd-MM-yyyy HH:mm}");
            }

            Slot studentBookedSlot = _context.Slot.Include(s => s.Student).GetSlot(slot).FirstOrDefault(s => s.StudentId != student.Id && !string.IsNullOrEmpty(s.StudentId));
            if (studentBookedSlot != null)
            {
                ModelState.AddModelError("StudentId", $"Student {studentBookedSlot.Student.StudentId} has already booked slot in room {slot.RoomId} at {slot.StartTime.GetValueOrDefault():dd-MM-yyyy HH:mm}");
            }

            if (!ModelState.IsValid) return View(slot);
            
            Slot updateSlot = _context.Slot.First(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime);
            updateSlot.StudentId = slot.StudentId;
            
            _context.Slot.Update(updateSlot);

            await _context.SaveChangesAsync();

            return RedirectToAction("StudentIndex", "Slot");
        }
    
        [HttpGet]
        [Authorize(Roles = Student.RoleName)]
        public IActionResult Cancel()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Student.RoleName)]
        public async Task<IActionResult> Cancel([Bind("RoomId,StartTime,StudentId")] Slot slot)
        {
            if (!ModelState.IsValid) return View(slot);

            if (!_context.Room.Any(r => r.RoomId == slot.RoomId))
            {
                ModelState.AddModelError("RoomId", $"Room {slot.RoomId} does not exist.");
            }

            if (!_context.Student.Any(r => r.Id == slot.StudentId))
            {
                ModelState.AddModelError("StudentId", $"Student {slot.StudentId} does not exist.");
            }

            var slotExist = _context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime);
            if (!slotExist)
            {
                ModelState.AddModelError("StartTime", $"No slot exist in {slot.RoomId} at {slot.StartTime:dd-MM-yyyy HH:mm}");
            }

            var slotEmpty = _context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime && s.StudentId == null);
            if (slotEmpty)
            {
                ModelState.AddModelError("StudentId", $"No students booked into this slot");
            }

            var sameStudent = _context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime && s.StudentId == slot.StudentId);
            if (!sameStudent)
            {
                ModelState.AddModelError("StudentId", $"The student ID for this slot does not match, cannot cancel this booking.");
            }

            if (!ModelState.IsValid) return View(slot);
            
            // remove the student id from the slot
            _context.Slot.FirstOrDefault(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime).StudentId = null;
           
            _context.Slot.Update(_context.Slot.FirstOrDefault(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime) );
           
            await _context.SaveChangesAsync();

            return RedirectToAction("StudentIndex", "Slot");
        }
    }
}
