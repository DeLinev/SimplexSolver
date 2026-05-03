using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SimplexMethodApp.Validators
{
    public class PositiveIntegerAttribute : ValidationAttribute
    {
        private readonly int _min;
        private readonly int _max;

        public PositiveIntegerAttribute(int min, int max, string ErrorMsg)
        {
            _min = min;
            _max = max;
            ErrorMessage = ErrorMsg;
        }

        protected override ValidationResult IsValid(object value, ValidationContext ctx)
        {
            if (value is string str && int.TryParse(str, out int n) && n >= _min && n <= _max)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }
    }

}
