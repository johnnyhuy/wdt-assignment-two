using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Rmit.Asr.Application.Areas.Identity.Models;
using Xunit;

namespace Rmit.Asr.Application.Tests
{
    public class StaffTest
    {
        [Theory]
        [InlineData("e12345")]
        [InlineData("e32145")]
        [InlineData("e23222")]
        public void SetStaffId_WithValidInput_ValidationSuccess(string input)
        {
            // Arrange
            var staff = new Staff();
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(staff) { MemberName = nameof(staff.Id) };

            // Act
            staff.Id = input;
            
            bool results = Validator.TryValidateProperty(staff.Id, validationContext, validationResults);

            // Assert
            Assert.Empty(validationResults);
            Assert.True(results);
        }
        
        [Theory]
        [InlineData("e12345678")]
        [InlineData("e36046723123")]
        [InlineData("e")]
        [InlineData("e12345@rmit.edu.au")]
        [InlineData("@#&(!@*#&")]
        [InlineData("this is a wacky string!")]
        public void SetStaffId_WithInvalidInput_ValidationFails(string input)
        {
            // Arrange
            var staff = new Staff();
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(staff) { MemberName = nameof(staff.Id) };

            // Act
            staff.Id = input;
            
            bool results = Validator.TryValidateProperty(staff.Id, validationContext, validationResults);

            // Assert
            string expectedMessage =
                $"The staff ID {staff.Id} is invalid, it always starts with a letter ‘e’ followed by 5 numbers.";
            
            Assert.Contains(validationResults, r => r.ErrorMessage == expectedMessage);
            Assert.False(results);
        }
        
        [Fact]
        public void SetStaff_WithEmptyFirstName_ValidationFails()
        {
            // Arrange
            var student = new Staff();
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(student) { MemberName = nameof(student.FirstName) };

            // Act
            student.FirstName = null;
            
            bool results = Validator.TryValidateProperty(student.FirstName, validationContext, validationResults);

            // Assert
            const string expectedMessage = "The First Name field is required.";
            
            Assert.Contains(validationResults, r => r.ErrorMessage == expectedMessage);
            Assert.False(results);
        }
        
        [Fact]
        public void SetStaff_WithEmptyLastName_ValidationFails()
        {
            // Arrange
            var student = new Staff();
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(student) { MemberName = nameof(student.LastName) };

            // Act
            student.LastName = null;
            
            bool results = Validator.TryValidateProperty(student.LastName, validationContext, validationResults);

            // Assert
            const string expectedMessage = "The Last Name field is required.";
            
            Assert.Contains(validationResults, r => r.ErrorMessage == expectedMessage);
            Assert.False(results);
        }
    }
}