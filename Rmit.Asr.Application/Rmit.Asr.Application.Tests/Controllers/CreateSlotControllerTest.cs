using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rmit.Asr.Application.Models;
using Rmit.Asr.Application.Models.ViewModels;
using Xunit;

namespace Rmit.Asr.Application.Tests.Controllers
{
    public class CreateSlotControllerTest : ControllerBaseTest
    {
        [Fact]
        public async Task CreateSlot_WithValidParameters_ReturnSuccess()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "C",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 8, 0, 0)
            };

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            Assert.Empty(SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage));
            Assert.True(SlotController.ModelState.IsValid);
            
            var viewResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", viewResult.ActionName);

            Assert.True(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime));
        }
        
        [Fact]
        public async Task CreateSlot_WithNonExistentRoom_ReturnSuccess()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "Z",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 8, 0, 0)
            };

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"Room {slot.RoomId} does not exist.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);

            // Slot does not exist in the mock database
            Assert.False(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime));
        }
        
        [Fact]
        public async Task CreateSlot_WithOverMaximumRoomBooking_ReturnFail()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "A",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 9, 0, 0)
            };

            Context.Slot.AddRange(
                new Slot
                {
                    RoomId = "A",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
                },
                new Slot
                {
                    RoomId = "A",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 14, 0, 0)
                }
            );

            Context.SaveChanges();

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"Room {slot.RoomId} has reached a maximum booking of {Room.MaxRoomBookingPerDay} per day.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);

            // Slot does not exist in the mock database
            Assert.False(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime));
        }
        
        [Fact]
        public async Task CreateSlot_WithStaffOverMaximumBooking_ReturnFail()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "D",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 13, 0,  0)
            };

            Context.Slot.AddRange(
                new Slot
                {
                    RoomId = "A",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
                },
                new Slot
                {
                    RoomId = "A",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 14, 0, 0)
                },
                new Slot
                {
                    RoomId = "B",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 10, 0, 0)
                },
                new Slot
                {
                    RoomId = "B",
                    StaffId = LoggedInStaff.Id,
                    StartTime = new DateTime(2019, 1, 1, 9, 0, 0)
                }
            );

            await Context.SaveChangesAsync();

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"Staff {LoggedInStaff.StaffId} has a maximum of {Staff.MaxBookingPerDay} bookings at {slot.StartTime:dd-MM-yyyy}.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);

            // Slot does not exist in the mock database
            Assert.False(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime));
        }
        
        [Fact]
        public async Task CreateSlot_WithStaffAlreadyTakenSlot_ReturnFail()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "A",
                StaffId = LoggedInStaff.Id,
                Staff = new Staff
                {
                    Id = LoggedInStaff.Id,
                    StaffId = LoggedInStaff.StaffId
                },
                StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
            };

            Context.Slot.AddRange(
                new Slot
                {
                    RoomId = "A",
                    StaffId = "e54321",
                    Staff = new Staff
                    {
                        StaffId = "e54321"
                    },
                    StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
                }
            );

            await Context.SaveChangesAsync();

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"Staff e54321 has already taken slot at room {slot.RoomId} {slot.StartTime:dd-MM-yyyy H:mm}.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);

            // Slot does not exist in the mock database
            Assert.False(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime && s.StaffId == slot.StaffId));
        }
        
        [Fact]
        public async Task CreateSlot_WithSlotAlreadyExists_ReturnFail()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            var slot = new CreateSlot
            {
                RoomId = "A",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
            };

            Context.Slot.AddRange(
                new Slot
                {
                    RoomId = "A",
                    StaffId = LoggedInStaff.Id,
                    Staff = new Staff
                    {
                        Id = LoggedInStaff.Id,
                        StaffId = LoggedInStaff.StaffId
                    },
                    StartTime = new DateTime(2019, 1, 1, 12, 0, 0)
                }
            );

            await Context.SaveChangesAsync();

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"Slot at room {slot.RoomId} {slot.StartTime:dd-MM-yyyy H:mm} already exists.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);
        }
        
        [Fact]
        public async Task CreateSlot_WithStaffAlreadyCreatedSlot_ReturnFail()
        {
            // Arrange
            UserLoggedIn(StaffUsername);
            
            var slot = new CreateSlot
            {
                RoomId = "D",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 13, 0, 0)
            };

            var createdSlot = new Slot
            {
                RoomId = "A",
                StaffId = LoggedInStaff.Id,
                StartTime = new DateTime(2019, 1, 1, 13, 0, 0)
            };

            Context.Slot.Add(createdSlot);

            await Context.SaveChangesAsync();

            // Act
            IActionResult result = await SlotController.Create(slot);

            // Assert
            IEnumerable<string> errorMessages = SlotController.ModelState.Values.SelectMany(e => e.Errors).Select(e => e.ErrorMessage);
            
            Assert.Contains(errorMessages, e => e == $"You have already created a slot at room {createdSlot.RoomId} {createdSlot.StartTime:dd-MM-yyyy H:mm}.");
            Assert.False(SlotController.ModelState.IsValid);
            
            Assert.IsType<ViewResult>(result);

            // Slot does not exist in the mock database
            Assert.False(Context.Slot.Any(s => s.RoomId == slot.RoomId && s.StartTime == slot.StartTime));
        }
    }
}