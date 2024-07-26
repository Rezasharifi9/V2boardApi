using System;
using System.ComponentModel.DataAnnotations;

public class EqualToAttribute : ValidationAttribute
{
    private readonly object _expectedValue;
    

    public EqualToAttribute(object expectedValue)
    {
        _expectedValue = expectedValue;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || value.Equals(_expectedValue))
        {
            return ValidationResult.Success;
        }
        return new ValidationResult(ErrorMessage);
    }
}